using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>시간 대기를 건너뛰며 실제 본문/선택지 코루틴의 표시 순서를 검사한다.</summary>
public static class NodeChoiceUIValidation
{
    public static string PlayResult { get; private set; } = "Not run";

    public static void StartPlayChecks()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode에서 실행하세요.");
        PlayResult = "Running";
        var host = new GameObject("NodeChoicePlayValidation") { hideFlags = HideFlags.DontSave };
        host.AddComponent<UIController>().StartCoroutine(GuardPlayChecks(host));
    }

    private static IEnumerator GuardPlayChecks(GameObject host)
    {
        var routine = PlayChecks(host);
        try
        {
            while (true)
            {
                object wait;
                try
                {
                    if (!routine.MoveNext()) break;
                    wait = routine.Current;
                }
                catch (Exception exception)
                {
                    PlayResult = "Failed: " + exception.Message;
                    throw;
                }
                yield return wait;
            }
        }
        finally
        {
            (routine as IDisposable)?.Dispose();
            UnityEngine.Object.Destroy(host);
        }
    }

    private static IEnumerator PlayChecks(GameObject host)
    {
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException(label);
            passed++;
        };
        var root = new GameObject("TemporaryChoices");
        root.transform.SetParent(host.transform);
        root.SetActive(false);
        var eventNode = ScriptableObject.CreateInstance<EventNode>();
        var storyNode = ScriptableObject.CreateInstance<StoryNode>();
        var save = UnityEngine.Object.FindObjectOfType<SaveManager>();
        bool saveEnabled = save != null && save.enabled;
        float previousScale = Time.timeScale;
        try
        {
            if (save != null) save.enabled = false;
            Time.timeScale = 1f;
            eventNode.nodeType = NodeType.EventNode;
            eventNode.nodeMessage = "이벤트 본문";
            eventNode.definition = ScriptableObject.CreateInstance<EventDefinition>();
            eventNode.definition.exitNode = storyNode;
            eventNode.definition.options.Add(new EventOption { id = "test", text = "이벤트 선택" });
            storyNode.nodeType = NodeType.StoryNode;
            storyNode.nodeMessage = "스토리 본문";
            storyNode.choices.Add(new Choice { choiceText = "스토리 선택" });
            var eventUI = Child<EventUIController>(root);
            eventUI.dialogueText = Child<TextMeshProUGUI>(eventUI.gameObject);
            var eb = Child<Button>(eventUI.gameObject);
            var el = Child<TextMeshProUGUI>(eb.gameObject);
            eventUI.choiceButtons = new[] { eb };
            var storyUI = Child<StoryUIController>(root);
            storyUI.dialogueText = Child<TextMeshProUGUI>(storyUI.gameObject);
            var sb = Child<Button>(storyUI.gameObject);
            var sl = Child<TextMeshProUGUI>(sb.gameObject);
            storyUI.choiceButtons = new[] { sb };
            root.SetActive(true);
            eventUI.UpdateUI(eventNode);
            storyUI.UpdateUI(storyNode);
            check(!eb.gameObject.activeSelf && !sb.gameObject.activeSelf, "두 화면 진입 직후 숨김");
            yield return new WaitForSeconds(.04f);
            check(!eb.gameObject.activeSelf && !sb.gameObject.activeSelf, "타이핑 중 숨김");
            yield return new WaitForSeconds(.5f);
            check(FindEventButton(eventUI) != null && eventUI.dialogueText.text == eventNode.nodeMessage, "Event 완료 후 표시");
            check(sb.gameObject.activeSelf && sl.text == "스토리 선택" && storyUI.dialogueText.text == storyNode.nodeMessage, "Story 완료 후 표시");
            eventNode.nodeMessage = new string('가', 30);
            eventUI.UpdateUI(eventNode);
            eventNode.nodeMessage = "새 본문";
            eventNode.definition.options[0].text = "새 선택";
            eventUI.UpdateUI(eventNode);
            yield return new WaitForSeconds(.9f);
            check(eventUI.dialogueText.text == "새 본문" && FindEventButton(eventUI).GetComponentInChildren<TMP_Text>().text.StartsWith("새 선택"), "중간 교체 시 이전 본문 취소");
            eventNode.nodeMessage = new string('나', 30);
            eventUI.UpdateUI(eventNode);
            eventUI.enabled = false;
            yield return new WaitForSeconds(.9f);
            check(!eb.gameObject.activeSelf, "컴포넌트 비활성화 시 이전 선택지 부활 방지");
            eventUI.enabled = true;
            eventNode.nodeMessage = "재진입";
            eventUI.UpdateUI(eventNode);
            yield return new WaitForSeconds(.4f);
            check(FindEventButton(eventUI) != null && eventUI.dialogueText.text == "재진입", "재활성화 후 표시");
            storyUI.UpdateUI(storyNode);
            storyUI.gameObject.SetActive(false);
            yield return new WaitForSeconds(.4f);
            check(!sb.gameObject.activeSelf, "Story 이탈 시 숨김 유지");
            PlayResult = $"Play Mode Node Choice 검사 {passed}개 통과";
        }
        finally
        {
            Time.timeScale = previousScale;
            if (save != null) save.enabled = saveEnabled;
            UnityEngine.Object.Destroy(eventNode.definition);
            UnityEngine.Object.Destroy(eventNode);
            UnityEngine.Object.Destroy(storyNode);
        }
    }

    [MenuItem("Tools/Validation/Node Choice UI")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException(label);
            passed++;
        };
        var root = new GameObject("NodeChoiceValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var eventNode = ScriptableObject.CreateInstance<EventNode>();
        var storyNode = ScriptableObject.CreateInstance<StoryNode>();
        EventUIController eventUI = null;
        StoryUIController storyUI = null;
        try
        {
            var button = Child<Button>(root);
            var label = Child<TextMeshProUGUI>(button.gameObject);
            label.gameObject.SetActive(false);
            var presenter = new ChoiceListPresenter(new[] { button });
            label.gameObject.SetActive(true);
            var choice = new Choice { choiceText = "문을 연다" };
            int clicks = 0;
            presenter.Present(new[] { choice }, _ => clicks++);
            check(button.gameObject.activeSelf && label.text == choice.choiceText, "비활성 라벨 검색 및 즉시 텍스트 설정");
            presenter.Present(new[] { choice }, _ => clicks++);
            button.onClick.Invoke();
            check(clicks == 1, "중복 리스너 없음");

            eventUI = Child<EventUIController>(root);
            eventUI.dialogueText = Child<TextMeshProUGUI>(root);
            eventUI.choiceButtons = new[] { button };
            Lifecycle(eventUI, "OnEnable");
            check(!button.gameObject.activeSelf, "Event 활성화 즉시 숨김");
            eventNode.nodeMessage = "이벤트 본문";
            eventNode.definition = ScriptableObject.CreateInstance<EventDefinition>();
            eventNode.definition.exitNode = storyNode;
            eventNode.definition.options.Add(new EventOption { id = "test", text = choice.choiceText });
            for (int visit = 0; visit < 2; visit++)
            {
                var sequence = eventUI.UpdateEventNode(eventNode);
                check(sequence.MoveNext() && !button.gameObject.activeSelf, "Event 본문 시작 전 선택지 숨김");
                Drain((IEnumerator)sequence.Current);
                check(FindEventButton(eventUI) == null, "Event 본문 완료 전 신규 버튼 숨김");
                check(!sequence.MoveNext() && FindEventButton(eventUI) != null, "Event 완료 후 신규 버튼 표시");
                check(FindEventButton(eventUI).GetComponentInChildren<TMP_Text>().text.StartsWith(choice.choiceText), "신규 버튼 문구 표시");
            }
            Lifecycle(eventUI, "OnDisable");
            check(!button.gameObject.activeSelf, "Event 비활성화 숨김");

            storyUI = Child<StoryUIController>(root);
            storyUI.dialogueText = Child<TextMeshProUGUI>(root);
            storyUI.choiceButtons = new[] { button };
            Lifecycle(storyUI, "OnEnable");
            check(!button.gameObject.activeSelf, "Story 활성화 즉시 숨김");
            storyNode.nodeMessage = "스토리 본문";
            storyNode.choices.Add(choice);
            ValidateSequence(storyUI.UpdateStoryNode(storyNode), storyUI.dialogueText, button, label, storyNode.nodeMessage, choice.choiceText, check);
            storyNode.nodeMessage = null;
            ValidateSequence(storyUI.UpdateStoryNode(storyNode), storyUI.dialogueText, button, label, "", choice.choiceText, check);
            storyNode.choices.Clear();
            Drain(storyUI.UpdateStoryNode(storyNode));
            check(!button.gameObject.activeSelf, "빈 선택지 숨김");
            return $"Node Choice UI 검사 {passed}개 통과 (Edit Mode 코루틴 순서 검사)";
        }
        finally
        {
            if (eventUI != null) Lifecycle(eventUI, "OnDisable");
            if (storyUI != null) Lifecycle(storyUI, "OnDisable");
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(eventNode.definition);
            UnityEngine.Object.DestroyImmediate(eventNode);
            UnityEngine.Object.DestroyImmediate(storyNode);
        }
    }

    private static Button FindEventButton(EventUIController ui)
    {
        foreach (var button in ui.GetComponentsInChildren<Button>(true))
            if (button.name == "EventAction_test" && button.gameObject.activeSelf && button.transform.parent.parent.parent.gameObject.activeSelf) return button;
        return null;
    }

    private static void ValidateSequence(IEnumerator sequence, TMP_Text body, Button button, TMP_Text label,
        string message, string choice, Action<bool, string> check)
    {
        check(sequence.MoveNext() && !button.gameObject.activeSelf, "본문 시작 전 선택지 숨김");
        Drain((IEnumerator)sequence.Current, () =>
        {
            if (button.gameObject.activeSelf) throw new InvalidOperationException("본문 출력 중 선택지 노출");
        });
        check(body.text == message && !button.gameObject.activeSelf, "본문 완료까지 선택지 숨김");
        check(!sequence.MoveNext() && button.gameObject.activeSelf && label.text == choice, "완료 후 채워진 선택지 표시");
    }

    private static void Drain(IEnumerator sequence, Action onYield = null)
    {
        while (sequence.MoveNext())
        {
            onYield?.Invoke();
            if (sequence.Current is IEnumerator nested) Drain(nested, onYield);
        }
    }

    private static T Child<T>(GameObject parent) where T : Component
    {
        var child = new GameObject(typeof(T).Name, typeof(RectTransform));
        child.transform.SetParent(parent.transform);
        return child.AddComponent<T>();
    }

    private static void Lifecycle(object target, string name) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
