using System;
using UnityEngine;

public class Player : Character
{
    #region [경험치 배율]
    private const float BASE_EXP = 12.1f;
    private const float EXP_GROWTH_RATE = 1.08f;
    #endregion

    #region [플레이어 경험치 & 장비 데이터]
    //레벨 관련
    private int exp;
    public int lv;

    //장비 관련 - 추후 구현 예정
    public EquipmentData equipmentData = new EquipmentData();
    public int tendency { get; private set; }
    public event Action<int> TendencyChanged;

    public void ChangeTendency(int amount)
    {
        tendency += amount;
        TendencyChanged?.Invoke(tendency);
    }

    public void ResetTendency()
    {
        tendency = 0;
        TendencyChanged?.Invoke(tendency);
    }

    #endregion


    //public event Action OnPlayerLvUp;

    #region [Initialize]
    public void InitializeFromData(PlayerData data)
    {
        InitializePlayer(data, false);
    }


    //인자를 하나 더 받자. initialmode, loadmode.

    public void LoadFromData(PlayerData data)
    {
        InitializePlayer(data, true);
    }

    private void InitializePlayer(PlayerData data, bool restoreHealth)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        InitializeCharacter(data.id, data.displayName, data.baseStats, data.tuningStats);
        exp = Mathf.Max(0, data.exp);
        lv = Mathf.Max(1, data.lv);
        tendency = data.tendency;
        equipmentData = new EquipmentData
        {
            weaponItem = data.equipmentData?.weaponItem,
            armorItem = data.equipmentData?.armorItem,
            accessoryItem = data.equipmentData?.accessoryItem
        };
        // 저장된 tuningStats는 이미 장비 보정을 포함하므로 로드 시 중복 가산하지 않는다.
        UpdateStats();
        SetCurrentHPAndNotify(restoreHealth ? data.currentHP : MaxHP);
        UpdateLV_UI();
    }
    #endregion

    #region [Data]
    //현재 플레이어 정보를 저장하기 위한 함수.
    public PlayerData GetCurrentData()
    {
        PlayerData data = new PlayerData();

        data.baseStats = this.baseStats;
        data.tuningStats = this.tuningStats;

        data.id = this.ID;
        data.displayName = this.characterName;

        data.currentHP = this.currentHP;
        data.exp = this.exp;
        data.lv = this.lv;
        data.tendency = tendency;
        data.equipmentData = new EquipmentData
        {
            weaponItem = equipmentData?.weaponItem,
            armorItem = equipmentData?.armorItem,
            accessoryItem = equipmentData?.accessoryItem
        };

        return data;
    }
    #endregion

    #region [Override]

    protected override void OnStatsChanged()
    {
        base.OnStatsChanged();
        PlayerEvent.OnStatsChanged();
    }

    //데미지 피격 함수.
    public override void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        base.TakeDamage(amount);

        GameEvent.OnTakeDamage(currentHP, MaxHP);
    }
    #endregion

    #region [LV Control]

    //경험치를 올리는 함수.
    public void GetExp(int exp)
    {
        if (exp <= 0) return;
        this.exp += exp;
        UpdateLv();
    }


    private void UpdateLv()
    {
        int requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));

        while (requiredExpForLvUP > 0 && exp >= requiredExpForLvUP)
        {
            exp -= requiredExpForLvUP;
            lv++;

            //10만큼 회복.
            Heal(10);


            UpdateLV_UI();

            //레벨업 이벤트 발생.
            PlayerEvent.PlayerLevelUp();
            requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));
        }
    }

    private void UpdateLV_UI()
    {
        if (lvText != null) lvText.text = "Lv." + lv;
    }
    #endregion

    #region [Equipment Control]
    public void EquipItem(EquipmentItem item)
    {
        if (item == null || item.durability <= 0)
        {
            Debug.LogWarning("장착할 아이템이 없습니다.");
            return;
        }
        if (equipmentData == null) equipmentData = new EquipmentData();
        if ((item.itemCategory == ItemCategory.Weapon && !(item is WeaponItem)) ||
            (item.itemCategory == ItemCategory.Armor && !(item is ArmorItem)) ||
            (item.itemCategory == ItemCategory.Accessory && !(item is AccessoryItem)))
        {
            Debug.LogWarning("장비 종류와 실제 데이터 타입이 일치하지 않습니다.");
            return;
        }
        //장착 아이템 정보 업데이트.
        switch (item.itemCategory)
        {
            case ItemCategory.Weapon:
                WeaponItem weaponItem = item as WeaponItem;
                if (equipmentData.weaponItem != null)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.weaponItem.Release(this);
                }
                weaponItem.Use(this);
                equipmentData.weaponItem = weaponItem;
                break;
            case ItemCategory.Armor:
                ArmorItem armorItem = item as ArmorItem;
                if (equipmentData.armorItem != null)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.armorItem.Release(this);
                }
                armorItem.Use(this);
                equipmentData.armorItem = armorItem;
                break;
            case ItemCategory.Accessory:
                AccessoryItem accessoryItem = item as AccessoryItem;
                if (equipmentData.accessoryItem != null)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.accessoryItem.Release(this);
                }
                accessoryItem.Use(this);
                equipmentData.accessoryItem = accessoryItem;
                break;
            default:
                Debug.LogWarning("알 수 없는 장비 유형입니다.");
                break;
        }

        //장착 후 스탯 업데이트.
        UpdateTuningStats();
    }

    public void RestoreEquipment(EquipmentData restoredEquipment)
    {
        int savedHP = currentHP;
        equipmentData = restoredEquipment ?? new EquipmentData();
        UpdateTuningStats();
        SetCurrentHPAndNotify(savedHP);
    }

    #endregion

    #region [Tuning Control]
    public override void UpdateTuningStats()
    {
        if (equipmentData == null) equipmentData = new EquipmentData();
        //장비 스탯 적용
        tuningStats.attackBonus = equipmentData.weaponItem != null ? equipmentData.weaponItem.attackBonus : 0;
        tuningStats.hpBonus = equipmentData.armorItem != null ? equipmentData.armorItem.hpBonus : 0;
        tuningStats.dodgeBonus = (equipmentData.armorItem != null ? equipmentData.armorItem.dodgeBonus : 0) + (equipmentData.accessoryItem != null ? equipmentData.accessoryItem.dodgeBonus : 0);

        //특수 효과에 따른 스탯 조정.
        tuningStats = ApplyStatusEffects(tuningStats);

        //최종 스탯 업데이트.
        UpdateStats();
    }
    #endregion
}
