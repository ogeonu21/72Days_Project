using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : UIController, IUpdatableUI
{
    #region [변수 그룹]
    public TMP_Text combatText;
    public GameObject attackButtons;
    public Image blood_Effect;

    public TMP_Text[] dodgeRateText;

    //Manager
    private CombatManager combatManager;
    private CharacterManager characterSource;
    private Player player;
    private Enemy enemy;
    private CombatUIRouter choiceRouter;
    [SerializeField] private Button choiceTemplate;
    private Button[] bodyButtons;
    #endregion

    private Coroutine bloodEffectCoroutine; // 피격 효과 코루틴을 제어하기 위한 변수

    private const float EnemyHitDuration = 0.3f;
    private const float EnemyShakeDistance = 6f; // Canvas 기준 거리. 해상도 배율은 CanvasScaler가 적용한다.
    private readonly List<UnityEngine.UI.Image> enemyHitImages = new List<UnityEngine.UI.Image>();
    private readonly List<Color> enemyHitColors = new List<Color>();
    // 연출 난수가 전투 명중·보상 난수 시퀀스를 바꾸지 않도록 분리한다.
    private readonly System.Random enemyHitRandom = new System.Random();
    private RectTransform enemyHitGroup;
    private Vector2 enemyHitOrigin;
    private float enemyHitElapsed;
    private bool enemyHitActive;


    #region [initialize]

    protected override void OnEnable()
    {
        base.OnEnable();
        NodeText = combatText;
        combatManager = CombatManager.Instance;
        if (combatManager != null)
            combatManager.CombatUIUpdate += UpdateCombatUI;
        characterSource = CharacterManager.Instance;
        if (characterSource != null)
        {
            characterSource.OnCharacterReady += UpdateCombatUI;
            UpdateCombatUI(characterSource.currentPlayer, characterSource.currentEnemy);
        }
        GameEvent.OnTakeDamageEffect += HandleTakeDamageEffect;
    }
    
    protected override void OnDisable()
    {
        if (combatManager != null) combatManager.CancelCombat();
        base.OnDisable();
        NodeText = null;
        if (combatManager != null)
            combatManager.CombatUIUpdate -= UpdateCombatUI;
        if (characterSource != null)
            characterSource.OnCharacterReady -= UpdateCombatUI;
        UnbindCharacters();
        GameEvent.OnTakeDamageEffect -= HandleTakeDamageEffect;
    }
    #endregion

    #region [Effect]

    private void HandleEnemyDamaged()
    {
        if (isActiveAndEnabled && attackButtons != null && attackButtons.activeInHierarchy)
            BeginEnemyHitFeedback();
    }

    private void BeginEnemyHitFeedback()
    {
        ResetEnemyHitFeedback();
        if (attackButtons == null) return;
        enemyHitGroup = attackButtons.transform as RectTransform;
        if (enemyHitGroup == null) return;
        enemyHitOrigin = enemyHitGroup.anchoredPosition;
        // 기존 PlayerInput 부위 버튼의 클릭 전환 대상 Image만 사용한다. 라벨은 착색하지 않는다.
        if (bodyButtons == null) bodyButtons = attackButtons.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        enemyHitImages.Clear();
        enemyHitColors.Clear();
        foreach (var button in bodyButtons)
        {
            var image = button != null ? button.targetGraphic as UnityEngine.UI.Image : null;
            if (image == null || enemyHitImages.Contains(image)) continue;
            enemyHitImages.Add(image);
            enemyHitColors.Add(image.color);
        }
        enemyHitElapsed = 0;
        enemyHitActive = true;
        ApplyEnemyHitFeedback();
    }

    private void LateUpdate()
    {
        if (enemyHitActive) AdvanceEnemyHitFeedback(Time.unscaledDeltaTime);
    }

    private void AdvanceEnemyHitFeedback(float deltaTime)
    {
        if (!enemyHitActive) return;
        if (enemyHitGroup == null || !enemyHitGroup.gameObject.activeInHierarchy)
        { ResetEnemyHitFeedback(); return; }
        enemyHitElapsed += Mathf.Max(0, deltaTime);
        if (enemyHitElapsed >= EnemyHitDuration) { ResetEnemyHitFeedback(); return; }
        ApplyEnemyHitFeedback();
    }

    private void ApplyEnemyHitFeedback()
    {
        float strength = 1 - Mathf.Clamp01(enemyHitElapsed / EnemyHitDuration);
        for (int i = 0; i < enemyHitImages.Count; i++)
        {
            if (enemyHitImages[i] == null) continue;
            Color original = enemyHitColors[i];
            enemyHitImages[i].color = Color.Lerp(original, new Color(1, 0, 0, original.a), strength);
        }
        var jitter = new Vector2((float)enemyHitRandom.NextDouble() * 2 - 1,
            (float)enemyHitRandom.NextDouble() * 2 - 1);
        enemyHitGroup.anchoredPosition = enemyHitOrigin + Vector2.ClampMagnitude(jitter, 1) * (EnemyShakeDistance * strength);
    }

    private void ResetEnemyHitFeedback()
    {
        if (!enemyHitActive) return;
        for (int i = 0; i < enemyHitImages.Count; i++)
            if (enemyHitImages[i] != null) enemyHitImages[i].color = enemyHitColors[i];
        if (enemyHitGroup != null) enemyHitGroup.anchoredPosition = enemyHitOrigin;
        enemyHitActive = false;
    }

    //데미지 피격 효과
    private void HandleTakeDamageEffect(int currentHP, int maxHP)
    {
        if (bloodEffectCoroutine != null)
        {
            StopCoroutine(bloodEffectCoroutine);
        }
        // 새로운 피격 효과 코루틴 시작
        bloodEffectCoroutine = StartCoroutine(ShowBloodScreenEffect(currentHP, maxHP));
    }

    private IEnumerator ShowBloodScreenEffect(int currentHP, int maxHP)
    {
        // 1. 체력 비율 계산 (체력이 낮을수록 효과가 강해짐)
        float healthPercent = (float)currentHP / maxHP;
        // 체력이 50%일 때 alpha 0.5, 체력이 0%일 때 alpha 1.0이 되도록 목표 투명도 설정
        float targetAlpha = Mathf.Max(0f, 0.7f - healthPercent);

        float fadeDuration = 0.1f; // 페이드인 시간
        float lingerDuration = 0.2f; // 효과 유지 시간
        float fadeOutDuration = 0.4f; // 페이드아웃 시간

        Color currentColor = blood_Effect.color;

        // 2. 페이드인
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            currentColor.a = Mathf.Lerp(0, targetAlpha, timer / fadeDuration);
            blood_Effect.color = currentColor;
            yield return null;
        }
        currentColor.a = targetAlpha;
        blood_Effect.color = currentColor;

        // 3. 효과 유지
        yield return new WaitForSeconds(lingerDuration);

        // 4. 페이드아웃
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            currentColor.a = Mathf.Lerp(targetAlpha, 0, timer / fadeOutDuration);
            blood_Effect.color = currentColor;
            yield return null;
        }
        currentColor.a = 0;
        blood_Effect.color = currentColor;

        // 코루틴 종료 후 참조 초기화
        bloodEffectCoroutine = null;
    }

    #endregion
    //너는 뭐니?
    public void UpdateUI(Node node)
    {
        ResetEnemyHitFeedback();
        if (attackButtons != null)
        {
            choiceRouter = attackButtons.GetComponent<CombatUIRouter>() ?? attackButtons.AddComponent<CombatUIRouter>();
            if (bodyButtons == null) bodyButtons = attackButtons.GetComponentsInChildren<Button>(true);
            if (choiceTemplate == null)
            {
                // 인체 부위 버튼이 아니라 기존 이벤트 선택지의 모양을 재사용한다.
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    foreach (var eventUI in canvas.GetComponentsInChildren<EventUIController>(true))
                        if (eventUI.choiceButtons != null && eventUI.choiceButtons.Length > 0 && eventUI.choiceButtons[0] != null)
                        { choiceTemplate = eventUI.choiceButtons[0]; break; }
            }
            choiceRouter.Configure(choiceTemplate, combatText != null ? combatText.font : null, bodyButtons, combatText);
        }
        combatManager.CombatNodeStart(node);
    }

    //피격률 등 표시
    public void UpdateCombatUI(Player player, Enemy enemy)
    {
        // 출혈 처리 직후에도 같은 참조로 갱신된다. 진행 중인 피격 연출을 취소하지 않는다.
        if (this.player == player && this.enemy == enemy) { RefreshCombatStats(); return; }
        UnbindCharacters();
        this.player = player;
        this.enemy = enemy;
        if (player != null) player.StatsChanged += RefreshCombatStats;
        if (enemy != null)
        {
            enemy.StatsChanged += RefreshCombatStats;
            enemy.Damaged += HandleEnemyDamaged;
        }
        RefreshCombatStats();
    }

    private void UnbindCharacters()
    {
        if (player != null) player.StatsChanged -= RefreshCombatStats;
        if (enemy != null)
        {
            enemy.StatsChanged -= RefreshCombatStats;
            enemy.Damaged -= HandleEnemyDamaged;
        }
        ResetEnemyHitFeedback();
        player = null;
        enemy = null;
    }

    private void RefreshCombatStats()
    {
        if (dodgeRateText == null) return;
        for (int i = 0; i < Mathf.Min(6, dodgeRateText.Length); i++)
        {
            if (dodgeRateText[i] == null) continue;
            int areaIndex = i < 2 ? i : i / 2 + 1;
            if (player == null || enemy == null || enemy.areaDataDB == null || areaIndex >= enemy.areaDataDB.Length)
            {
                dodgeRateText[i].text = string.Empty;
                continue;
            }
            string damageText;
            string dodgeText;

            if (i < 2)
            {
                damageText = $"{Mathf.RoundToInt(player.AttackPower * 0.95f * enemy.areaDataDB[i].damageMultiplier)} ~ {Mathf.RoundToInt(player.AttackPower * 1.05f * enemy.areaDataDB[i].damageMultiplier)}";
                dodgeText = $"{(Mathf.Min((enemy.areaDataDB[i].hitRate - enemy.DodgeRate + player.AccuracyRate), 1.0f) * 100) :F2}%";
            }
            else
            {
                damageText = $"{Mathf.RoundToInt(player.AttackPower * 0.95f * enemy.areaDataDB[i/2 + 1].damageMultiplier)} ~ {Mathf.RoundToInt(player.AttackPower * 1.05f * enemy.areaDataDB[i/2 + 1].damageMultiplier)}";
                dodgeText = $"{(Mathf.Min((enemy.areaDataDB[i / 2 + 1].hitRate - enemy.DodgeRate + player.AccuracyRate), 1.0f) * 100) :F2}%";
            }
            
            dodgeRateText[i].text = $"{damageText}\n{dodgeText}";
        }

    }

}
