using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>기존 UI 오브젝트를 재사용하여 GameWindow의 공통 본문과 선택지 참조를 연결한다.</summary>
public static class SharedNodeUISetup
{
    private const string ScenePath = "Assets/Scenes/GameWindow.unity";

    [MenuItem("Tools/UI/Install Shared Node View")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        else if (scene.isDirty) throw new InvalidOperationException("GameWindow의 변경을 먼저 저장하거나 취소하세요.");
        try
        {
            var main = Find(scene, "MainUIGroup");
            var installed = main.GetComponentInChildren<SharedNodeView>(true);
            if (installed != null)
            {
                EnsureDialogueScroll(installed);
                EditorUtility.SetDirty(installed);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(Validate(scene));
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
                return;
            }
            var story = main.GetComponentInChildren<StoryUIController>(true);
            var eventUI = main.GetComponentInChildren<EventUIController>(true);
            var combat = main.GetComponentInChildren<CombatUIController>(true);
            var ending = main.GetComponentInChildren<EndingUIController>(true);
            if (story == null || eventUI == null || combat == null || ending == null)
                throw new InvalidOperationException("4종 노드 Controller 연결이 필요합니다.");
            var modes = new UIController[] { story, eventUI, combat, ending };
            if (modes.Any(x => x.gameObject.activeSelf))
                throw new InvalidOperationException("설치 중 노드 Controller는 비활성 상태여야 합니다.");
            var body = story.dialogueText;
            var template = eventUI.choiceButtons.FirstOrDefault(x => x != null);
            if (body == null || template == null || combat.attackButtons == null)
                throw new InvalidOperationException("기존 본문·버튼 템플릿·공격 영역이 없습니다.");
            var boards = modes.Select(x => x.transform.Find("Text_Board")).ToArray();
            if (boards.Any(x => x == null)) throw new InvalidOperationException("기존 Text_Board 구조가 다릅니다.");
            var oldTexts = new TMP_Text[] { story.dialogueText, eventUI.dialogueText, combat.combatText, ending.dialogueText };
            var oldChoiceRoots = new[] { story.transform.Find("Choice_Buttons"), eventUI.transform.Find("Choice_Buttons") };
            var oldButtons = story.choiceButtons.Concat(eventUI.choiceButtons).Where(x => x != null).ToArray();

            RectTransform shared = Rect("SharedContent", main);
            var view = shared.gameObject.AddComponent<SharedNodeView>();
            // Story 본문 오브젝트와 메타 참조를 보존한다.
            var area = (RectTransform)boards[0];
            area.SetParent(shared, false);
            area.name = "DialogueArea";
            Stretch(area, new Vector2(80, 80), new Vector2(-80, -80));
            RectTransform feedback = Rect("HitFeedbackRoot", area);
            body.transform.SetParent(feedback, false);
            body.name = "DialogueText";
            Stretch(body.rectTransform, Vector2.zero, Vector2.zero);
            body.raycastTarget = false;
            body.enableAutoSizing = false;
            foreach (var graphic in area.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            RectTransform choices = Rect("ChoiceArea", shared);
            choices.anchorMin = choices.anchorMax = Vector2.zero;
            choices.anchorMax = new Vector2(1, 0);
            choices.offsetMin = new Vector2(12, 12);
            choices.offsetMax = new Vector2(-12, 640);
            var viewport = Rect("ScrollView", choices);
            var image = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 30;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            template.transform.SetParent(shared, false);
            template.name = "ChoiceButtonTemplate";
            template.onClick = new Button.ButtonClickedEvent();
            template.gameObject.SetActive(false);
            view.dialogueText = body; view.choiceArea = choices; view.choiceTemplate = template;
            view.choiceScroll = scroll; view.choiceContent = content;
            EnsureDialogueScroll(view);
            choices.gameObject.SetActive(false);

            var references = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            foreach (var old in oldTexts)
            {
                references[old] = body;
                references[old.gameObject] = body.gameObject;
                references[old.transform] = body.transform;
            }
            foreach (var button in oldButtons)
            {
                references[button] = template;
                references[button.gameObject] = template.gameObject;
                references[button.transform] = template.transform;
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        UnityEngine.Object replacement;
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue != null && references.TryGetValue(property.objectReferenceValue, out replacement))
                            property.objectReferenceValue = replacement;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            foreach (var mode in modes)
            {
                mode.sharedView = view;
                mode.NodeText = body;
                EditorUtility.SetDirty(mode);
            }
            story.dialogueText = eventUI.dialogueText = ending.dialogueText = body;
            combat.combatText = body;
            story.choiceButtons = eventUI.choiceButtons = new[] { template };
            var combatSerialized = new SerializedObject(combat);
            combatSerialized.FindProperty("choiceTemplate").objectReferenceValue = template;
            combatSerialized.ApplyModifiedPropertiesWithoutUndo();

            // 공통화한 표시 자식만 정리하고 모드별 처리 오브젝트는 보존한다.
            foreach (var board in boards.Skip(1)) UnityEngine.Object.DestroyImmediate(board.gameObject);
            foreach (Transform child in area.Cast<Transform>().ToArray())
                if (child != feedback) UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (var root in oldChoiceRoots)
                if (root != null) UnityEngine.Object.DestroyImmediate(root.gameObject);
            var obsoleteResult = eventUI.transform.Find("EventResultText");
            if (obsoleteResult != null)
            {
                var router = eventUI.GetComponent<EventUIRouter>();
                if (router != null)
                {
                    var routerSerialized = new SerializedObject(router);
                    var result = routerSerialized.FindProperty("resultText");
                    if (result != null) result.objectReferenceValue = null;
                    routerSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
                UnityEngine.Object.DestroyImmediate(obsoleteResult.gameObject);
            }
            var controllers = Rect("NodeControllers", main);
            foreach (var mode in modes) mode.transform.SetParent(controllers, false);
            var combatArea = Rect("CombatArea", combat.transform);
            combat.attackButtons.transform.SetParent(combatArea, false);
            // 기존 부위 버튼의 이름·영구 이벤트·참조를 유지한다.
            shared.SetAsFirstSibling();
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            string resultMessage = Validate(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("씬 저장 실패");
            Debug.Log(resultMessage);
        }
        finally
        {
            if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    public static string Validate(Scene scene)
    {
        var main = Find(scene, "MainUIGroup");
        var view = main.GetComponentInChildren<SharedNodeView>(true);
        var modes = main.GetComponentsInChildren<UIController>(true);
        if (view == null || view.dialogueText == null || view.choiceArea == null || view.choiceTemplate == null ||
            view.choiceContent == null || view.choiceScroll == null || view.dialogueScroll == null || modes.Length != 4 ||
            modes.Any(x => x.sharedView != view || x.NodeText != view.dialogueText))
            throw new InvalidOperationException("공통 뷰 또는 4종 모드 참조가 올바르지 않습니다.");
        if (view.GetComponentsInParent<UIController>(true).Length != 0 ||
            view.dialogueText.transform.parent.name != "HitFeedbackRoot" || view.choiceTemplate.gameObject.activeSelf)
            throw new InvalidOperationException("공통 뷰 수명·피격 영역·템플릿 설정 오류");
        var combat = modes.OfType<CombatUIController>().Single();
        if (combat.attackButtons == null || combat.attackButtons.GetComponentsInChildren<Button>(true).Length != 6)
            throw new InvalidOperationException("전투 부위 버튼 6개를 확인하세요.");
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    throw new InvalidOperationException("Missing Script: " + t.name);
        return "SharedNodeUI: 4종 본문 참조·공통 선택지·6개 부위 버튼·Missing Script 검사 통과";
    }

    private static void EnsureDialogueScroll(SharedNodeView view)
    {
        var viewport = view.dialogueText.transform.parent as RectTransform;
        if (viewport == null) throw new InvalidOperationException("본문의 HitFeedbackRoot가 없습니다.");
        var image = viewport.GetComponent<UnityEngine.UI.Image>() ?? viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
        if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = view.dialogueText.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.enabled = false;
        view.dialogueScroll = scroll;
    }

    private static Transform Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        throw new InvalidOperationException("UI를 찾을 수 없습니다: " + name);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Stretch(rect, Vector2.zero, Vector2.zero);
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = min; rect.offsetMax = max;
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
    }
}
