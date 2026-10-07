using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>실제 씬·세이브 대신 임시 객체로 선택지와 입력 처리를 검사한다.</summary>
public static class CombatChoiceValidation
{
    [MenuItem("Tools/Validation/Combat Choices")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); passed++; };
        check(!CombatRules.ResolveEscape(0, 0), "0% 도주 실패");
        check(CombatRules.ResolveEscape(1, 1), "100% 도주 성공");
        check(CombatRules.ResolveEscape(.5f, .499f), "확률 미만 성공");
        check(!CombatRules.ResolveEscape(.5f, .5f), "경계값 실패");
        check(!CombatRules.ResolveEscape(float.NaN, 0), "잘못된 확률 실패");
        check(typeof(EventUIRouter).BaseType == typeof(ChoiceUIRouter), "이벤트 공통 부모");
        check(typeof(CombatUIRouter).BaseType == typeof(ChoiceUIRouter), "전투 공통 부모");

        var inventoryField = SingletonField<InventoryManager>();
        var combatField = SingletonField<CombatManager>();
        var characterField = SingletonField<CharacterManager>();
        object oldInventory = inventoryField.GetValue(null), oldCombat = combatField.GetValue(null), oldCharacter = characterField.GetValue(null);
        var root = new GameObject("CombatChoiceFixture") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var ui = new GameObject("CombatChoiceUI", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        var potion = ScriptableObject.CreateInstance<PotionItem>();
        var exit = ScriptableObject.CreateInstance<StoryNode>();
        try
        {
            var player = root.AddComponent<Player>();
            player.baseStats = new BaseStats(1, 1, 100);
            player.UpdateStats();
            typeof(Character).GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 1);
            var inventory = root.AddComponent<InventoryManager>();
            var combat = root.AddComponent<CombatManager>();
            var source = root.AddComponent<CharacterManager>();
            inventoryField.SetValue(null, inventory); combatField.SetValue(null, combat); characterField.SetValue(null, source);
            typeof(CharacterManager).GetProperty("currentPlayer").SetValue(source, player);
            Set(combat, "player", player);
            Set(combat, "escapeNode", exit);
            combat.combatActive = combat.onPlayerTurn = true;
            var attack = new PlayerInputData(PlayerBehaviour.Attack, player.areaDataDB[0], null);
            check(combat.TrySubmitInput(attack, out _), "공격 입력 수락");
            check(!combat.TrySubmitInput(attack, out _), "같은 턴 중복 입력 거부");
            combat.onPlayerTurn = true;
            check(!combat.TrySubmitInput(new PlayerInputData((PlayerBehaviour)999, default, null), out _) && combat.onPlayerTurn, "잘못된 입력은 턴 보존");
            Set(combat, "escapeNode", null);
            check(!combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Run, default, null), out _) && combat.onPlayerTurn, "출구 없는 도주는 턴 보존");
            Set(combat, "escapeNode", exit);
            potion.itemName = "검사 물약"; potion.itemID = "fixture"; potion.health = 1; potion.quantity = 2; potion.isConsumable = true;
            inventory.inventoryItems.Add(potion);
            check(combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Use, default, potion), out _), "아이템 입력 수락");
            check(potion.quantity == 1 && player.CurrentHP == 2 && !combat.onPlayerTurn, "회복·수량·턴 소비");
            check(!combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Use, default, potion), out _) && potion.quantity == 1, "중복 소비 방지");
            combat.onPlayerTurn = true;
            inventory.inventoryItems.Clear();
            check(!combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Use, default, potion), out _) && combat.onPlayerTurn, "소실된 아이템은 턴 보존");

            var buttonGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(ui.transform, false);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonGo.transform, false);
            buttonGo.SetActive(false);
            var router = ui.AddComponent<CombatUIRouter>();
            var bodyGo = new GameObject("BodyAttack", typeof(RectTransform), typeof(Button));
            bodyGo.transform.SetParent(root.transform, false);
            var body = bodyGo.GetComponent<Button>();
            body.onClick.AddListener(() => router.AttackAreaInput(0));
            var bodyButtons = new[] { body };
            router.Configure(buttonGo.GetComponent<Button>(), TMP_Settings.defaultFontAsset, bodyButtons);
            check(VisibleButtons(ui).Length == 2, "추가 행동은 아이템·도주만 표시");
            check(body.gameObject.activeSelf && body.interactable, "기존 인체 공격 버튼 유지");
            body.onClick.Invoke();
            check(!combat.onPlayerTurn && !body.interactable && body.gameObject.activeSelf, "부위 클릭 공격 후 숨기지 않고 입력만 잠금");
            combat.onPlayerTurn = true;
            router.Configure(buttonGo.GetComponent<Button>(), TMP_Settings.defaultFontAsset, bodyButtons);
            Click(ui, "아이템 사용");
            check(VisibleButtons(ui).Length == 2 && !VisibleButtons(ui)[0].interactable, "빈 소모품 안내와 뒤로");
            inventory.inventoryItems.Add(potion);
            inventory.UpdateItemUI(potion);
            check(VisibleButtons(ui).Any(b => b.GetComponentInChildren<TMP_Text>().text.Contains("×1")), "소모품 수량 표시 갱신");
            var itemButton = VisibleButtons(ui).First(b => b.GetComponentInChildren<TMP_Text>().text.Contains("×1"));
            var staleClick = itemButton.onClick;
            Click(ui, "뒤로");
            staleClick.Invoke();
            check(potion.quantity == 1 && combat.onPlayerTurn, "이전 목록 클릭 무시");
            Click(ui, "도주", true);
            check(!combat.onPlayerTurn && VisibleButtons(ui).Length == 0, "도주 제출 후 선택지 숨김");
            combat.onPlayerTurn = true;
            router.Configure(buttonGo.GetComponent<Button>(), TMP_Settings.defaultFontAsset, bodyButtons);
            router.Configure(buttonGo.GetComponent<Button>(), TMP_Settings.defaultFontAsset, bodyButtons);
            check(VisibleButtons(ui).Length == 2, "반복 구성 중복 목록 없음");
            combat.CancelCombat();
            check(!combat.CanChooseAction && VisibleButtons(ui).Length == 0, "취소 시 입력·화면 닫힘");
            combat.combatActive = combat.onPlayerTurn = true;
            typeof(Character).GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, player.MaxHP);
            check(!combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Use, default, potion), out _) && potion.quantity == 1 && combat.onPlayerTurn, "체력 가득 참은 소비·턴 차감 없음");
            typeof(Character).GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 1);
            check(combat.TrySubmitInput(new PlayerInputData(PlayerBehaviour.Use, default, potion), out _) && inventory.inventoryItems.Count == 0, "마지막 소모품 사용 후 목록 제거");
            return "Combat choices: " + passed + " passed";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(ui);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(potion);
            UnityEngine.Object.DestroyImmediate(exit);
            inventoryField.SetValue(null, oldInventory); combatField.SetValue(null, oldCombat); characterField.SetValue(null, oldCharacter);
        }
    }

    private static FieldInfo SingletonField<T>() where T : MonoBehaviour => typeof(SingleTon<T>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static Button[] VisibleButtons(GameObject root) => root.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy).ToArray();
    private static void Click(GameObject root, string label, bool prefix = false)
    {
        var button = VisibleButtons(root).First(b => prefix ? b.GetComponentInChildren<TMP_Text>().text.StartsWith(label) : b.GetComponentInChildren<TMP_Text>().text == label);
        button.onClick.Invoke();
    }
}
