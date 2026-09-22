using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerStatsDetailsValidation
{
    public static string RunChecks()
    {
        int passed = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (!ok) throw new InvalidOperationException(message);
            passed++;
        };
        var source = Resources.FindObjectsOfTypeAll<InventoryUI>().Single(x => x.gameObject.scene.name == "GameWindow");
        check(source.playerStatsDetails != null, "씬 상세 페이지 연결");
        var open = source.transform.Find("Status_UIGroup").GetComponent<Button>();
        check(open != null && Enumerable.Range(0, open.onClick.GetPersistentEventCount())
            .Any(i => open.onClick.GetPersistentTarget(i) == source && open.onClick.GetPersistentMethodName(i) == "OpenPlayerStats"), "진입 버튼 연결");
        var root = new GameObject("StatsDetailsValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var weapon = ScriptableObject.CreateInstance<WeaponItem>();
        var armor = ScriptableObject.CreateInstance<ArmorItem>();
        var accessory = ScriptableObject.CreateInstance<AccessoryItem>();
        PlayerStatsDetailsUI view = null;
        try
        {
            var player = root.AddComponent<Player>();
            player.InitializeFromData(new PlayerData { baseStats = new BaseStats(2, 3, 4) });
            weapon.attackBonus = 17; weapon.range = 2;
            armor.hpBonus = 20; armor.dodgeBonus = .1f;
            accessory.dodgeBonus = .04f;
            player.equipmentData.weaponItem = weapon;
            player.equipmentData.armorItem[0] = armor;
            player.equipmentData.accessoryItem = accessory;
            player.UpdateTuningStats();
            view = UnityEngine.Object.Instantiate(source.playerStatsDetails, root.transform);
            view.Bind(player);
            check(player.AttackPower == 31 && view.attackText.text.Contains("장비 17"), "공격력 장비 분해");
            check(view.attributesText.text.Contains("STR  2") && view.attributesText.text.Contains("DEX  3") && view.attributesText.text.Contains("CON  4"), "기본 능력치 표시");
            check(Mathf.Approximately(player.DodgeRate, .2f) && view.dodgeText.text.Contains("20.00%"), "방어구 장신구 회피 합산");
            check(view.accuracyText.text.Contains("4.50") && view.rangeText.text.Contains("사거리  2"), "명중 보정 및 사거리 표시");
            root.SetActive(true);
            view.gameObject.SetActive(true);
            if (!Application.isPlaying) Lifecycle(view, "OnEnable");
            player.TakeDamage(10);
            check(view.healthText.text == $"체력  {player.CurrentHP} / {player.MaxHP}" &&
                Mathf.Approximately(view.healthBar.value, (float)player.CurrentHP / player.MaxHP), "HP 이벤트 갱신");
            player.TakeEffect(new AreaData("팔", 1, 1, 1));
            check(view.attackText.text.Contains("기타 보정 -5") && player.AttackPower == 26, "디버프 별도 표시");
            player.baseStats.dex = 100;
            player.UpdateStats();
            check(view.dodgeText.text.Contains("70.00%"), "회피 상한 표시");
            view.Close();
            if (!Application.isPlaying) Lifecycle(view, "OnDisable");
            var previous = view.healthText.text;
            player.TakeDamage(1);
            check(view.healthText.text == previous, "닫힌 페이지 이벤트 해제");
            view.gameObject.SetActive(true);
            if (!Application.isPlaying) Lifecycle(view, "OnEnable");
            check(view.healthText.text != previous, "재진입 최신 값");
            view.Bind(null);
            check(view.healthBar.value == 0 && view.attackText.text == "—", "플레이어 없음 처리");
            return $"상세 능력치 UI {passed}개 통과 ({(Application.isPlaying ? "Play" : "Edit")} Mode)";
        }
        finally
        {
            if (view != null) view.Bind(null);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(weapon);
            UnityEngine.Object.DestroyImmediate(armor);
            UnityEngine.Object.DestroyImmediate(accessory);
        }
    }

    private static void Lifecycle(object target, string name) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
