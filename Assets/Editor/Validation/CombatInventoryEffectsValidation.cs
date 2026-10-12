using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>전투 인벤토리 잠금과 양쪽 캐릭터 효과 표시. 실제 씬/세이브는 변경하지 않는다.</summary>
public static class CombatInventoryEffectsValidation
{
    [MenuItem("Tools/Validation/Combat Inventory and Effects")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); passed++; };
        var bindings = new Dictionary<FieldInfo, object>();
        foreach (var type in new[] { typeof(GameEvent), typeof(PlayerEvent) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                { bindings[field] = field.GetValue(null); field.SetValue(null, null); }
        var root = new GameObject("CombatInventoryEffectsFixture") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        var combatNode = ScriptableObject.CreateInstance<CombatNode>();
        var storyNode = ScriptableObject.CreateInstance<StoryNode>();
        float oldTime = Time.timeScale;
        var oldRandom = UnityEngine.Random.state;
        try
        {
            var manager = Child<UIManager>(root);
            var combat = Child<CombatManager>(root);
            var inventory = Child<InventoryUI>(root);
            inventory.uiManager = manager;
            inventory.gameObject.SetActive(false);
            var log = Child<EventResultLog>(root);
            var template = Child<TextMeshProUGUI>(log.gameObject);
            template.gameObject.SetActive(false);
            Set(log, "lineTemplate", template);
            Set(manager, "resultLog", log);
            Set(manager, "inventoryCombatSource", combat);
            Time.timeScale = .75f;
            combatNode.nodeType = NodeType.CombatNode;
            storyNode.nodeType = NodeType.StoryNode;
            Call(manager, "UpdateInventoryAccess", combatNode);
            manager.OpenUI(inventory.gameObject);
            check(!inventory.gameObject.activeSelf && Time.timeScale == .75f, "도입 대화에서도 열기 차단·시간 유지");
            log.Tick(0);
            check(log.transform.Find("EventLogLine").GetComponent<TMP_Text>().text == "전투 중에는 사용할 수 없습니다.", "정확한 결과 로그 문구");
            combat.combatActive = true;
            manager.OpenUI(inventory.gameObject);
            check(!inventory.gameObject.activeSelf, "전투 중 열기 차단");
            combat.combatActive = false;
            check(manager.TryBlockInventory(), "전투 결과 대화 동안 잠금 유지");
            int before = ((Queue<string>)Get(log, "pending")).Count;
            inventory.OpenPlayerStats(); inventory.SelectInventorySlot(0); inventory.SelectEquipmentSlot(0);
            inventory.ActivateSelected(); inventory.BeginDiscard(); inventory.ConfirmDiscard();
            inventory.IncreaseDiscard(); inventory.DecreaseDiscard(); inventory.DiscardAll();
            check(((Queue<string>)Get(log, "pending")).Count == before + 9, "남아 있던 인벤토리 콜백도 전부 차단");
            Call(manager, "UpdateInventoryAccess", storyNode);
            check(!manager.TryBlockInventory(), "비전투 노드에서 해제");
            manager.OpenUI(inventory.gameObject);
            check(inventory.gameObject.activeSelf && Time.timeScale == 0, "비전투 인벤토리 정상 열기");
            manager.CloseUI(inventory.gameObject);
            check(!inventory.gameObject.activeSelf && Time.timeScale == .75f, "닫기 후 시간 복원");
            combat.combatActive = true;
            check(manager.TryBlockInventory(), "노드 변경 알림 이전에도 진행 중 전투 잠금");

            enemyData.baseStats = new BaseStats(10, 10, 10);
            foreach (bool isPlayer in new[] { true, false })
            {
                Character character = isPlayer ? (Character)Child<Player>(root) : Child<Enemy>(root);
                character.nameText = Child<TextMeshProUGUI>(character.gameObject);
                character.nameText.rectTransform.sizeDelta = new Vector2(700, 60);
                if (isPlayer) ((Player)character).InitializeFromData(new PlayerData { baseStats = new BaseStats(10, 10, 10) });
                else ((Enemy)character).InitializeFromData(enemyData);
                var view = character.nameText.GetComponent<CharacterEffectsUI>();
                // Edit Mode 임시 객체에서는 생명주기 메시지를 명시적으로 호출한다.
                Call(view, "OnEnable");
                int notifications = 0;
                character.EffectsChanged += () => notifications++;
                check(view != null && Visible(view) == 0, "효과 없는 이름표");
                int attack = character.AttackPower;
                float dodge = character.DodgeRate;
                character.TakeEffect(character.areaDataDB[2]);
                check(Visible(view) == 1 && BadgeText(view, "공격력 감소") == "x3", "공격력 감소 아이콘·턴");
                character.TakeEffect(character.areaDataDB[3]);
                character.TakeEffect(character.areaDataDB[1]);
                check(Visible(view) == 3 && BadgeText(view, "출혈") == "x2", "세 효과 동시 표시");
                check(character.AttackPower == attack - 5 && Mathf.Approximately(character.DodgeRate, dodge - .05f), "기존 효과 계산 유지");
                int hp = character.CurrentHP;
                character.CountEffect();
                check(BadgeText(view, "공격력 감소") == "x2" && BadgeText(view, "회피율 감소") == "x2" && BadgeText(view, "출혈") == "x1", "라운드 감소 즉시 표시");
                check(character.CurrentHP == hp - 3 && notifications == 4, "출혈 피해·효과 통지");
                character.CountEffect();
                check(Visible(view) == 2 && BadgeText(view, "공격력 감소") == "x1", "출혈 만료 숨김");
                character.CountEffect();
                check(Visible(view) == 0 && character.nameText.margin.z == 0, "모든 효과 만료·이름 영역 복원");
                check(character.AttackPower == attack && Mathf.Approximately(character.DodgeRate, dodge), "만료 후 스탯 복원");
                character.TakeEffect(character.areaDataDB[2]); character.CountEffect(); character.TakeEffect(character.areaDataDB[2]);
                check(BadgeText(view, "공격력 감소") == "x3" && Visible(view) == 1, "재적용 시 지속시간 갱신·아이콘 중복 없음");
                Call(view, "OnDisable"); character.CountEffect();
                check(BadgeText(view, "공격력 감소") == "x3", "비활성 뷰 구독 해제");
                Call(view, "OnEnable"); Call(view, "OnEnable");
                check(BadgeText(view, "공격력 감소") == "x2", "재활성 시 현재 상태 반영");
                character.EffectReset();
                check(Visible(view) == 0, "효과 초기화 숨김");
                check(character.nameText.GetComponents<CharacterEffectsUI>().Length == 1 && view.transform.Find("StatusEffects").childCount == 3, "UI 중복 생성 없음");
                check(!view.GetComponentInChildren<CharacterEffectIcon>(true).raycastTarget, "아이콘이 입력을 가로채지 않음");
                check(view.GetComponentInChildren<CharacterEffectIcon>(true).GetComponent<CanvasRenderer>() != null, "아이콘 렌더러 연결");
                var iconNames = new[] { "attack-down", "dodge-down", "bleeding" };
                foreach (var icon in view.GetComponentsInChildren<CharacterEffectIcon>(true))
                {
                    check(icon.sprite != null && icon.sprite.name == iconNames[(int)icon.Effect], "효과별 생성 스프라이트 매핑");
                    check(icon.color == Color.white && icon.preserveAspect && !icon.raycastTarget, "원본 색상·비율·입력 유지");
                    check(icon.sprite.texture.filterMode == FilterMode.Point && icon.sprite.texture.mipmapCount == 1, "Point 필터·밉맵 없음");
                }
                character.TakeEffect(character.areaDataDB[1]);
                if (isPlayer) ((Player)character).InitializeFromData(new PlayerData());
                else ((Enemy)character).InitializeFromData(enemyData);
                check(Visible(view) == 0, "캐릭터 재초기화 시 이전 효과 제거");
                Call(view, "OnDisable");
            }
            return $"Combat inventory/effects: {passed} passed (Edit Mode, 실제 세이브 변경 없음)";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(enemyData);
            UnityEngine.Object.DestroyImmediate(combatNode);
            UnityEngine.Object.DestroyImmediate(storyNode);
            Time.timeScale = oldTime;
            UnityEngine.Random.state = oldRandom;
            foreach (var pair in bindings) pair.Key.SetValue(null, pair.Value);
        }
    }
    private static int Visible(CharacterEffectsUI view)
    {
        int count = 0;
        foreach (Transform badge in view.transform.Find("StatusEffects")) if (badge.gameObject.activeSelf) count++;
        return count;
    }

    /// <summary>격리된 미리보기 씬에서 실제 Player/Enemy 프리팹의 헤더만 렌더링한다.</summary>
    public static string CapturePreview()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var texture = new RenderTexture(1080, 460, 24);
        var previous = RenderTexture.active;
        Texture2D capture = null;
        var bindings = new Dictionary<FieldInfo, object>();
        foreach (var type in new[] { typeof(GameEvent), typeof(PlayerEvent) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                { bindings[field] = field.GetValue(null); field.SetValue(null, null); }
        try
        {
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 230;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .12f, .14f);
            camera.targetTexture = texture;
            camera.cullingMask = 1 << 31;
            var canvasObject = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 460);
            int index = 0;
            foreach (string path in new[] { "Assets/Prefabs/Player.prefab", "Assets/Prefabs/Enemy.prefab" })
            {
                var parent = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
                parent.SetParent(canvas.transform, false);
                parent.sizeDelta = new Vector2(1080, 180);
                parent.anchoredPosition = new Vector2(0, index == 0 ? 110 : -110);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = UnityEngine.Object.Instantiate(prefab, parent);
                // 실제 프리팹은 플레이어/적을 세로로 배치한다. 미리보기에서는 각각의 헤더 슬롯으로 정규화한다.
                ((RectTransform)instance.transform).anchoredPosition3D = new Vector3(0, -90, 0);
                instance.SetActive(true);
                var character = instance.GetComponent<Character>();
                character.characterName = index == 0 ? "플레이어" : "훈련소 경비병";
                character.baseStats = new BaseStats(10, 10, 10);
                character.UpdateStats();
                typeof(Character).GetField("currentHP", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(character, 120);
                character.TakeEffect(character.areaDataDB[2]);
                character.TakeEffect(character.areaDataDB[3]);
                character.TakeEffect(character.areaDataDB[1]);
                if (character.lvText != null) character.lvText.text = "LV.3";
                character.nameText.GetComponent<CharacterEffectsUI>().Refresh();
                index++;
            }
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            Canvas.ForceUpdateCanvases();
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = texture;
            capture = new Texture2D(1080, 460, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 1080, 460), 0, 0); capture.Apply();
            string directory = System.IO.Path.GetFullPath("Docs/AI/Captures/CombatEffects");
            System.IO.Directory.CreateDirectory(directory);
            string output = System.IO.Path.Combine(directory, "player-enemy-effects.png");
            System.IO.File.WriteAllBytes(output, capture.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
            foreach (var pair in bindings) pair.Key.SetValue(null, pair.Value);
        }
    }
    private static string BadgeText(CharacterEffectsUI view, string label) => view.transform.Find("StatusEffects/" + label + "/Turns").GetComponent<TMP_Text>().text;
    private static T Child<T>(GameObject root) where T : Component
    {
        var go = new GameObject(typeof(T).Name, typeof(RectTransform)); go.transform.SetParent(root.transform, false); return go.AddComponent<T>();
    }
    private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
}
