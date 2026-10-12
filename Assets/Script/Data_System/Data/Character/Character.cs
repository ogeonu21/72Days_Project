using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadOnlyAttribute : PropertyAttribute { }

public class Character : MonoBehaviour
{
    [Header("캐릭터 아이디")]
    public string ID;
    public string characterName;

    #region [UI Component]
    [Header("UI 요소")]
    public Slider hpSlider;
    public TMP_Text hpText;
    public TMP_Text nameText;
    public TMP_Text lvText;
    #endregion

    #region [Stats]
    [Header("기본 스탯")]
    // BaseStats는 보정 능력치 적용 이전
    public BaseStats baseStats;
    // Stats는 보정 능력치 적용 이후
    
    [Header("보정치")]
    [Tooltip("장비/버프 보정치")]
    public TuningStats tuningStats;

    [Header("최종 능력치")]
    private Stats stats;

    [Header("현재 체력")]
    [SerializeField, ReadOnly] protected int currentHP;
    #endregion

    #region [AreaData]
    //개별 부위 데이터베이스 => Enemy 객체에서 개별 확률 계산을 위해 적용.
    public AreaData[] areaDataDB = {
        new AreaData("머리", 0.4f, 0.6f, 1.6f),
        new AreaData("몸", 0.9f, 0.15f, 0.7f),
        new AreaData("팔", 0.65f, 0.3f, 1.1f),
        new AreaData("다리", 0.8f, 0.2f, 0.9f)
    };
    #endregion

    #region [Effect]

    protected int[] EffectTurn = new int[4];
    //기존 회피율 등등이 필요함.

    //공격력 감소량
    public const int AttackDown_Force = 1;
    public const int AttackDown_Turn = 3;
    //회피율 감소율
    public const float DodgeDown_Force = 0.05f;
    public const int DodgeDown_Turn = 3;
    public const int Bleeding_Damage = 3;
    public const int Bleeding_Turn = 3;
    #endregion

    #region [Events]
    public event Action onDied;
    public event Action StatsChanged;
    public event Action EffectsChanged;
    private CharacterEffectsUI effectsUI;
    private HealthBarAnimation healthBarAnimation;
    public int GetEffectTurns(CharacterEffect effect)
    {
        int index = (int)effect;
        return index >= 0 && index < 3 ? EffectTurn[index] : 0;
    }
    #endregion

    #region [initialize]
    //읽기 전용.
    public int CurrentHP => currentHP;
    public int MaxHP => stats.maxHP;
    public int AttackPower => stats.attackPower;
    public float DodgeRate => stats.dodgeRate;
    public float AccuracyRate => stats.accuracyRate;
    public int AttackRange => stats.attackRange;
    
    public bool IsDead => currentHP <= 0;
    
    //HP 수정하고 수정 사실을 알림.
    protected void SetCurrentHPAndNotify(int currentHP)
    {
        int nextHP = Mathf.Clamp(currentHP, 0, MaxHP);
        if (this.currentHP == nextHP) return;
        this.currentHP = nextHP;
        OnStatsChanged();
    }
    #endregion

    #region [State Update Function]
    //Stats Update
    //장비 변경, 스탯 변동 시에 작동.
    public void UpdateStats(bool changeBaseStats = false)
    {
        int previousMaxHP = MaxHP;
        bool wasAlive = !IsDead;
        stats = new Stats(baseStats, tuningStats);
        // 최대 체력 증가분만 보충하고 감소 시에는 새 상한으로 제한한다.
        // 하지만, 만약 장비를 꼈다 뺏다 하는 식의 버그성 플레이를 하려한다면? 그로인해서 최대체력을 속이려 한다면?
        // 레벨업으로 인한 체력 회복만 가능하도록, 혹은 장비를 처음 착용했을 때만 회복하도록
        // baseStats에 변동이 있을때만. 즉, 기본 스탯 str, dex, con이 체력을 회복할거임.
        if (changeBaseStats)
        {
            currentHP = wasAlive ? Mathf.Clamp(currentHP + Mathf.Max(0, MaxHP - previousMaxHP), 0, MaxHP) : 0;
        }
        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        OnStatsChanged();
    }

    protected virtual void OnStatsChanged()
    {
        UpdateHP_UI();
        StatsChanged?.Invoke();
    }

    protected void InitializeCharacter(string id, string displayName, BaseStats initialStats, TuningStats initialTuning)
    {
        if (!string.IsNullOrWhiteSpace(id)) ID = id;
        if (!string.IsNullOrWhiteSpace(displayName)) characterName = displayName;
        baseStats = initialStats;
        tuningStats = initialTuning;
        Array.Clear(EffectTurn, 0, EffectTurn.Length);
        EffectsChanged?.Invoke();
        currentHP = 0;
        stats = new Stats(baseStats, tuningStats);
    }

    protected TuningStats ApplyStatusEffects(TuningStats value)
    {
        if (EffectTurn[0] > 0) value.attackBonus -= AttackDown_Force;
        if (EffectTurn[1] > 0) value.dodgeBonus -= DodgeDown_Force;
        return value;
    }

    public virtual void UpdateTuningStats()
    {
        UpdateStats();
    }
    #endregion

    #region [Combat Function]

    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount < 0) return;
        SetCurrentHPAndNotify(Mathf.Max(0, currentHP - amount));

        if (currentHP <= 0)
        {
            Die();
        }
    }

    //Item 사용시 적용하기 위한 Heal 함수.
    public virtual void Heal(int amount)
    {
        if (IsDead || amount < 0) return;
        SetCurrentHPAndNotify((int)Math.Min(MaxHP, (long)currentHP + amount));
        Debug.Log($"{amount}만큼의 체력을 회복하였다.");

    }

    protected virtual void Die()
    {
        onDied?.Invoke();
        // 필요시 애니/이펙트/비활성화 등
        // gameObject.SetActive(false);
    }
    #endregion

    #region [Effect Function]

    //특수 효과 턴수 계산.
    //특수 효과 턴이 존재한다면 -1, 만약 -1하여 0이 된다면 효과 해제.
    public void CountEffect()
    {
        bool changed = false;
        for (int i = 0; i < 3; i++)
        {
            if (EffectTurn[i] > 0)
            {
                EffectTurn[i]--;
                changed = true;

                switch (i)
                {
                    //팔
                    case 0:
                        if (EffectTurn[0] == 0)
                        {
                            UpdateTuningStats();
                        }
                        break;
                    //다리
                    case 1:
                        if (EffectTurn[1] == 0)
                        {
                            UpdateTuningStats();
                        }
                        break;
                    case 2:
                    //복부 특수효과, 3의 데미지
                        TakeDamage(Bleeding_Damage);
                        break;
                    default:
                        break;
                }
            }   
        }
        if (changed) EffectsChanged?.Invoke();
    }

    //특수 효과 턴수 적용 함수.
    public void TakeEffect(AreaData data)
    {
        switch (data.label)
        {
            case "팔":
                EffectTurn[0] = AttackDown_Turn;
                break;
            case "다리":
                EffectTurn[1] = DodgeDown_Turn;
                break;
            case "몸":
                EffectTurn[2] = Bleeding_Turn;
                break;
            default:
                break;
        }
        UpdateTuningStats();
        EffectsChanged?.Invoke();
    }

    public void EffectReset()
    {
        for (int i = 0; i < EffectTurn.Length; i++)
        {
            EffectTurn[i] = 0;
        }
        UpdateTuningStats();
        EffectsChanged?.Invoke();
    }
    #endregion

    #region [UI Update]
    protected void UpdateHP_UI()
    {
        if (nameText != null) nameText.text = characterName;
        if (nameText != null && effectsUI == null)
        {
            effectsUI = nameText.GetComponent<CharacterEffectsUI>() ?? nameText.gameObject.AddComponent<CharacterEffectsUI>();
            effectsUI.Bind(this, nameText);
        }

        if (hpSlider != null)
        {
            if (healthBarAnimation == null || healthBarAnimation.gameObject != hpSlider.gameObject)
                healthBarAnimation = hpSlider.GetComponent<HealthBarAnimation>() ?? hpSlider.gameObject.AddComponent<HealthBarAnimation>();
            healthBarAnimation.SetValue(MaxHP > 0 ? (float)currentHP / MaxHP : 0f, Application.isPlaying);
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {MaxHP}";
        }
    }
    #endregion



    //이제 여기에 공격처리, 추가효과 적용 등을 구현.
    //사거리 판별은 combatManager에서 구현.
}
