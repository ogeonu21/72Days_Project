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

    [MenuItem("Tools/Validation/Player Hit Feedback")]
    public static void RunPlayerHitFeedbackMenu() => Debug.Log(RunPlayerHitFeedbackChecks());

    public static string RunPlayerHitRuleChecks()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var strengthMethod = typeof(CombatUIController).GetMethod("CalculatePlayerHitStrength", flags);
        var alphaMethod = typeof(CombatUIController).GetMethod("CalculateBloodAlpha", flags);
        Func<int, int, float> strength = (damage, max) => (float)strengthMethod.Invoke(null, new object[] { damage, max });
        Func<int, int, float, float> alpha = (hp, max, value) => (float)alphaMethod.Invoke(null, new object[] { hp, max, value });
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); passed++; };
        check(strength(0, 100) == 0 && strength(-1, 100) == 0, "피해 없음은 강도0");
        check(strength(1, 0) == 0 && strength(1, -1) == 0, "잘못된 최대HP 방어");
        check(Math.Abs(strength(1, 100) - .1f) < .00001f, "1% 피해는 루트 강도10%");
        check(strength(25, 100) == .5f, "25% 피해는 루트 강도50%");
        check(strength(int.MaxValue, 100) == 1, "과잉 피해 강도 제한");
        check(strength(10, 100) == strength(100, 1000), "최대 체력 비례 정규화");
        check(alpha(99, 100, strength(1, 100)) > .2f, "초반 작은 피해도 혈흔 표시");
        check(alpha(75, 100, strength(25, 100)) > alpha(99, 100, strength(1, 100)), "피해량에 따른 혈흔 증가");
        check(alpha(0, 100, 1) <= .85f && alpha(0, 100, 1) > .8f, "혈흔 상한85%");
        check(alpha(50, 0, 1) == 0 && alpha(100, 100, 0) == 0, "유효하지 않은 혈흔 입력 방어");
        check(alpha(1, 100, .1f) > alpha(99, 100, .1f), "낮은 잔여HP의 추가 강조");
        return $"Player hit rules: {passed} passed";
    }

    public static string RunPlayerHitFeedbackChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        string rules = RunPlayerHitRuleChecks();
        var root = new GameObject("PlayerHitFixture") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var textGroup = new GameObject("CombatTextGroup", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        var hpGroup = new GameObject("PlayerHPBar", typeof(RectTransform), typeof(UnityEngine.UI.Slider)) { hideFlags = HideFlags.HideAndDontSave };
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); passed++; };
        Action<int, int, int> damageHandler = null;
        try
        {
            var player = root.AddComponent<Player>();
            player.InitializeFromData(new PlayerData { baseStats = new BaseStats(1, 1, 10) });
            player.hpSlider = hpGroup.GetComponent<UnityEngine.UI.Slider>();
            var controller = root.AddComponent<CombatUIController>();
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObject.transform.SetParent(textGroup.transform, false);
            controller.combatText = textObject.GetComponent<TMPro.TMP_Text>();
            var bloodObject = new GameObject("Blood", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bloodObject.transform.SetParent(textGroup.transform, false);
            controller.blood_Effect = bloodObject.GetComponent<UnityEngine.UI.Image>();
            controller.blood_Effect.color = new Color(1, 0, 0, 0);
            controller.UpdateCombatUI(player, null);
            var textRect = (RectTransform)textGroup.transform;
            var hpRect = (RectTransform)hpGroup.transform;
            Vector2 textOrigin = new Vector2(0, 12), hpOrigin = new Vector2(20, 25);
            textRect.anchoredPosition = textOrigin; hpRect.anchoredPosition = hpOrigin;
            int notifications = 0, lastDamage = 0;
            damageHandler = (hp, max, damage) =>
            {
                notifications++; lastDamage = damage;
                InvokeFeedback(controller, "HandleTakeDamageEffect", hp, max, damage);
            };
            GameEvent.OnTakeDamageEffect += damageHandler;
            player.TakeDamage(1);
            check(notifications == 1 && lastDamage == 1, "실제 피해를 UI에 전달");
            check(Vector2.Distance(textRect.anchoredPosition, textOrigin) <= 18.001f &&
                (textRect.anchoredPosition - textOrigin - (hpRect.anchoredPosition - hpOrigin)).sqrMagnitude < .001f, "텍스트 그룹과 HP바 같은 범위·방향으로 흔들림");
            InvokeFeedback(controller, "AdvancePlayerHitFeedback", .1f);
            check(controller.blood_Effect.color.a > .15f, "작은 피해 혈흔 페이드인");
            InvokeFeedback(controller, "AdvancePlayerHitFeedback", .2f);
            check(textRect.anchoredPosition == textOrigin && hpRect.anchoredPosition == hpOrigin, "0.3초 뒤 흔들림 복원");
            player.TakeDamage(1); player.TakeDamage(1);
            InvokeFeedback(controller, "AdvancePlayerHitFeedback", 1f);
            check(textRect.anchoredPosition == textOrigin && hpRect.anchoredPosition == hpOrigin && controller.blood_Effect.color.a == 0, "연속 피격 후 위치 누적·잔상 없음");
            player.TakeDamage(1);
            InvokeFeedback(controller, "OnDisable");
            check(textRect.anchoredPosition == textOrigin && hpRect.anchoredPosition == hpOrigin && controller.blood_Effect.color.a == 0, "화면 비활성 시 혈흔·위치 복원");
            int remaining = player.CurrentHP;
            player.TakeDamage(int.MaxValue);
            check(lastDamage == remaining, "치명 피해는 실제 감소한HP만 전달");
            int count = notifications;
            player.TakeDamage(1); player.TakeDamage(0); player.TakeDamage(-1);
            check(notifications == count, "사망 후·피해 없는 호출은 알림 없음");
            return $"{rules}; Player hit UI: {passed} passed (Edit Mode, 수동 시간 진행)";
        }
        finally
        {
            if (damageHandler != null) GameEvent.OnTakeDamageEffect -= damageHandler;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(textGroup);
            UnityEngine.Object.DestroyImmediate(hpGroup);
        }
    }

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
