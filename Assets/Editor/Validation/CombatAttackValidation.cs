using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>실제 AttackTurn의 피해·내구도 처리를 Edit Mode에서 검사한다. 클릭 대기 전까지만 실행한다.</summary>
public static class CombatAttackValidation
{
    [MenuItem("Tools/Validation/Combat Attack")]
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
        var random = UnityEngine.Random.state;
        var root = new GameObject("CombatAttackValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var armor = ScriptableObject.CreateInstance<ArmorItem>();
        var weapon = ScriptableObject.CreateInstance<WeaponItem>();
        try
        {
            var combat = root.AddComponent<CombatManager>();
            var player = root.AddComponent<Player>();
            var enemy = root.AddComponent<Enemy>();
            player.InitializeFromData(new PlayerData { baseStats = new BaseStats(1, 0, 100) });
            enemy.baseStats = new BaseStats(1, 0, 100);
            enemy.UpdateStats();
            typeof(Character).GetMethod("SetCurrentHPAndNotify", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(enemy, new object[] { enemy.MaxHP });
            var hit = new AreaData("머리", 100, 0, 1);

            int hp = player.CurrentHP;
            Attack(combat, enemy, player, hit, 1);
            check(player.CurrentHP < hp, "방어구 네 슬롯 미착용 상태에서 적 공격 완료");
            armor.durability = 100;
            player.equipmentData.armorItem[1] = armor;
            hp = player.CurrentHP;
            Attack(combat, enemy, player, hit, 1);
            check(player.CurrentHP < hp && armor.durability == 99, "일부 장착 상태 피해 및 내구도 1회 감소");
            player.equipmentData.armorItem = null;
            Attack(combat, enemy, player, hit, 1);
            check(armor.durability == 99, "방어구 배열 누락 상태 처리");
            player.equipmentData = null;
            hp = player.CurrentHP;
            Attack(combat, enemy, player, hit, 1);
            check(player.CurrentHP < hp, "장비 데이터 없음 처리");
            player.equipmentData = new EquipmentData { weaponItem = weapon };
            weapon.durability = 100;
            for (int i = 0; i < 3; i++)
            {
                hp = enemy.CurrentHP;
                Attack(combat, player, enemy, hit, 0);
                check(enemy.CurrentHP < hp, "플레이어 공격 " + i);
                hp = player.CurrentHP;
                Attack(combat, enemy, player, hit, 1);
                check(player.CurrentHP < hp, "적 반격 " + i);
            }
            check(weapon.durability == 97, "플레이어 무기 내구도 유지");
            return $"Combat Attack {passed}개 통과 (Edit Mode, 공격 처리 및 교대 호출)";
        }
        finally
        {
            UnityEngine.Random.state = random;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(armor);
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    private static void Attack(CombatManager combat, Character attacker, Character target, AreaData area, int index)
    {
        var routine = (IEnumerator)typeof(CombatManager).GetMethod("AttackTurn", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(combat, new object[] { attacker, target, area, index });
        try
        {
            if (!routine.MoveNext()) throw new InvalidOperationException("공격 후 로그 출력 단계에 도달하지 못했습니다.");
        }
        finally { (routine as IDisposable)?.Dispose(); }
    }
}
