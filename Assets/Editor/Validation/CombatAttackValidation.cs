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

    [MenuItem("Tools/Validation/Combat Hit Feedback")]
    public static void RunHitFeedbackMenu() => Debug.Log(RunHitFeedbackChecks());

    public static string RunHitFeedbackChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); passed++; };
        var root = new GameObject("EnemyHitFixture") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false); // 전역 매니저와 씬 이벤트를 연결하지 않는다.
        var group = new GameObject("BodyButtons", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        var random = UnityEngine.Random.state;
        try
        {
            var enemy = root.AddComponent<Enemy>();
            enemy.baseStats = new BaseStats(1, 1, 10);
            enemy.UpdateStats();
            typeof(Character).GetMethod("SetCurrentHPAndNotify", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(enemy, new object[] { enemy.MaxHP });
            int hits = 0;
            enemy.Damaged += () => hits++;
            enemy.TakeDamage(0); enemy.TakeDamage(-1);
            check(hits == 0, "피해 없는 호출은 피격 알림 없음");
            enemy.TakeDamage(1);
            check(hits == 1, "실제 피해는 피격 알림 1회");

            var controller = root.AddComponent<CombatUIController>();
            controller.attackButtons = group;
            var rect = (RectTransform)group.transform;
            var origin = new Vector2(17, 260);
            rect.anchoredPosition = origin;
            var buttonGo = new GameObject("BodyButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            buttonGo.transform.SetParent(group.transform, false);
            var unrelated = buttonGo.GetComponent<UnityEngine.UI.Image>();
            unrelated.color = Color.blue;
            var targetGo = new GameObject("TransitionTarget", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            targetGo.transform.SetParent(buttonGo.transform, false);
            var target = targetGo.GetComponent<UnityEngine.UI.Image>();
            var originalColor = new Color(.8f, .7f, .6f, .4f);
            target.color = originalColor;
            var button = buttonGo.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = target;
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            Action begin = () => InvokeFeedback(controller, "BeginEnemyHitFeedback");
            Action<float> tick = dt => InvokeFeedback(controller, "AdvanceEnemyHitFeedback", dt);
            begin();
            check(target.color == new Color(1, 0, 0, originalColor.a), "Button 전환 대상만 붉게 표시·알파 유지");
            check(unrelated.color == Color.blue && button.interactable, "다른 이미지·버튼 입력 설정 보존");
            check(Vector2.Distance(rect.anchoredPosition, origin) <= 6.001f, "그룹 흔들림 범위 제한");
            tick(.15f);
            check(target.color.g > 0 && target.color.g < originalColor.g, "중간 시점 색상 복원 진행");
            controller.UpdateCombatUI(enemy: enemy, player: null);
            begin();
            controller.UpdateCombatUI(enemy: enemy, player: null);
            check(target.color.r == 1 && target.color.g == 0, "동일 캐릭터 갱신 시 연출 유지");
            begin(); tick(.3f);
            check(target.color == originalColor && rect.anchoredPosition == origin, "연속 피격 후 0.3초 종료·누적 이동 없음");
            begin(); group.SetActive(false); tick(.01f);
            check(target.color == originalColor && rect.anchoredPosition == origin, "버튼 그룹 비활성 시 원상 복구");
            group.SetActive(true); begin();
            InvokeFeedback(controller, "OnDisable");
            check(target.color == originalColor && rect.anchoredPosition == origin, "화면 비활성 시 원상 복구");
            check(UnityEngine.Random.state.Equals(random), "연출은 게임플레이 난수 상태 보존");
            enemy.TakeDamage(int.MaxValue);
            enemy.TakeDamage(1);
            check(hits == 2, "치명 피해는 알림, 사망 후 중복 피격 제외");
            return $"Combat Hit Feedback {passed}개 통과 (Edit Mode, 임시 UI·수동 시간 진행)";
        }
        finally
        {
            UnityEngine.Random.state = random;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(group);
        }
    }

    private static void InvokeFeedback(CombatUIController controller, string method, params object[] args)
    {
        typeof(CombatUIController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, args);
    }

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
