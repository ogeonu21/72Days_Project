using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : MonoBehaviour
{
    #region [변수 그룹]
    public TMP_Text combatText;
    public GameObject attackButtons;
    public Image blood_Effect;

    public TMP_Text[] dodgeRateText;

    //Manager
    private CombatManager combatManager;
    #endregion

    private Coroutine bloodEffectCoroutine; // 피격 효과 코루틴을 제어하기 위한 변수


    #region [initialize]
    private void Awake()
    {
        combatManager = CombatManager.Instance;
        combatManager.CombatUIUpdate += UpdateCombatUI;
        combatManager.onTextUpdate += UpdateCombatText;
    }

    private void OnEnable()
    {
        GameEvent.OnTakeDamageEffect += HandleTakeDamageEffect;
    }
    private void OnDisable()
    {
        GameEvent.OnTakeDamageEffect -= HandleTakeDamageEffect;
    }
    #endregion

    #region [Effect]
    private void HandleTakeDamageEffect(int currentHP, int maxHP)
    {
        // 이전에 실행 중이던 피격 효과가 있다면 중지
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

    public void UpdateCombatUI(Player player, Enemy enemy)
    {
        AreaData data;

        for (int i = 0; i < 6; i++)
        {
            switch (i)
            {
                case 0:
                case 1:
                    dodgeRateText[i].text = (AreaDataDB.GetArea("팔",out data).hitRate - enemy.DodgeRate + player.AccuracyRate) * 100 + "%";
                    break;
                case 2:
                case 3:
                    dodgeRateText[i].text = (AreaDataDB.GetArea("다리", out data).hitRate - enemy.DodgeRate + player.AccuracyRate) * 100 + "%";
                    break;
                case 4:
                    dodgeRateText[i].text = (AreaDataDB.GetArea("몸", out data).hitRate - enemy.DodgeRate + player.AccuracyRate) * 100 + "%";
                    break;
                case 5:
                    dodgeRateText[i].text = (AreaDataDB.GetArea("머리", out data).hitRate - enemy.DodgeRate + player.AccuracyRate) * 100 + "%";
                    break;
                default:
                    break;
            }
        }

    }

    IEnumerator UpdateCombatText(string text)
    {
        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(combatText, text, 0.05f));
    }

}
