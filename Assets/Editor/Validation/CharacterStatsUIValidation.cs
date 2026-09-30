using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>씬 및 저장 파일을 수정하지 않는 스탯 UI 이벤트 회귀 검사.</summary>
public static class CharacterStatsUIValidation
{
    [MenuItem("Tools/Validation/Character Stats UI")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        var root = new GameObject("StatsUIValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException(label);
            passed++;
        };
        try
        {
            var player = Child<Player>(root);
            player.InitializeFromData(new PlayerData());
            var enemy = Child<Enemy>(root);
            enemy.UpdateStats();
            var combat = Child<CombatUIController>(root);
            var label = Child<TextMeshProUGUI>(root);
            combat.dodgeRateText = new TMP_Text[] { label };
            combat.UpdateCombatUI(player, enemy);
            string before = label.text;
            player.tuningStats.attackBonus += 20;
            player.UpdateStats();
            check(label.text != before, "플레이어 공격력 즉시 반영");
            before = label.text;
            enemy.tuningStats.dodgeBonus += .1f;
            enemy.UpdateStats();
            check(label.text != before, "적 회피율 즉시 반영");

            var normal = Child<UIManager>(root);
            var strength = Child<TextMeshProUGUI>(root);
            typeof(UIManager).GetField("playerStatsView", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(normal, new PlayerStatsView(strength, null, null));
            Call(normal, "UpdateCharacter", player, enemy);
            player.baseStats.str += 1;
            player.UpdateStats();
            check(strength.text == " : " + player.baseStats.str, "일반 UI 즉시 반영");

            player.hpText = Child<TextMeshProUGUI>(root);
            int notifications = 0;
            player.StatsChanged += () => notifications++;
            player.TakeDamage(1);
            check(notifications == 1 && player.hpText.text == $"{player.CurrentHP} / {player.MaxHP}", "피해 통지 및 HP 표시");
            player.Heal(1);
            check(notifications == 2, "회복 통지");
            player.TakeDamage(0);
            player.Heal(0);
            check(notifications == 2, "HP 변화 없는 호출 중복 통지 방지");

            var replacement = Child<Player>(root);
            replacement.InitializeFromData(new PlayerData());
            combat.UpdateCombatUI(replacement, enemy);
            combat.UpdateCombatUI(replacement, enemy);
            Call(normal, "UpdateCharacter", replacement, enemy);
            label.text = "unchanged";
            strength.text = "unchanged";
            player.UpdateStats();
            check(label.text == "unchanged" && strength.text == "unchanged", "교체된 캐릭터 구독 해제");
            replacement.UpdateStats();
            check(label.text != "unchanged" && strength.text != "unchanged", "새 캐릭터 구독");
            Call(combat, "OnDisable");
            Call(normal, "OnDisable");
            label.text = "disabled";
            strength.text = "disabled";
            replacement.UpdateStats();
            enemy.UpdateStats();
            check(label.text == "disabled" && strength.text == "disabled", "비활성 UI 구독 해제");

            int hp = replacement.CurrentHP;
            replacement.tuningStats.hpBonus = 100;
            replacement.UpdateStats();
            check(replacement.CurrentHP == hp, "장비 최대 HP 증가 시 임의 회복 방지");
            replacement.Heal(100);
            replacement.tuningStats.hpBonus = 0;
            replacement.UpdateStats();
            check(replacement.CurrentHP == replacement.MaxHP, "최대 HP 감소 상한 적용");
            return $"StatsChanged UI 검사 {passed}개 통과";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static T Child<T>(GameObject root) where T : Component
    {
        var child = new GameObject(typeof(T).Name);
        child.transform.SetParent(root.transform);
        return child.AddComponent<T>();
    }

    private static void Call(object target, string name, params object[] args)
    {
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
