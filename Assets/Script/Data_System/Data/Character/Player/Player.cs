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

    //장비 관련
    public EquipmentData equipmentData = new EquipmentData();
    //플레이어 성향
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
    // 플레이어 데이터를 새롭게 생성하여 초기화
    public void InitializeFromData(PlayerData data)
    {
        InitializePlayer(data, false);
    }

    // 플레이어 데이터를 로드하여 초기화
    public void LoadFromData(PlayerData data)
    {
        InitializePlayer(data, true);
    }

    //플레이어 데이터를 초기화
    private void InitializePlayer(PlayerData data, bool restoreHealth)
    {
        // 오류 반환
        if (data == null) throw new ArgumentNullException(nameof(data));
        // Character 초기화 함수를 불러오는거야.
        // 공통 분모를 초기화
        InitializeCharacter(data.id, data.displayName, data.baseStats, data.tuningStats);
        // 플레이어에게만 있는 데이터를 초기화.
        // 레벨 초기화
        exp = Mathf.Max(0, data.exp);
        lv = Mathf.Max(1, data.lv);
        // 성향 초기화
        tendency = data.tendency;
        // 장비 초기화
        equipmentData = data.equipmentData?.Copy() ?? new EquipmentData();

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
        data.equipmentData = equipmentData?.Copy() ?? new EquipmentData();

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
        //요구 경험치 계산
        int requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));
        int previousLv = lv;

        while (requiredExpForLvUP > 0 && exp >= requiredExpForLvUP)
        {
            //경험치와 레벨 처리
            exp -= requiredExpForLvUP;
            lv++;
            //10만큼 회복.
            Heal(10);
            //이거를 다시 계산할 필요가 있나? 있지. 2번 연속으로 레벨업을 한다면?
            requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));
        }
        if (previousLv < lv)
        {
            UpdateLV_UI(lv - previousLv);
            //레벨업 이벤트 발생.
            //이벤트를 중복 발생시켜야할듯?
            PlayerEvent.PlayerLevelUp(lv - previousLv);
        }
    }

    private void UpdateLV_UI(int levelDifference = 0)
    {
        if (lvText != null) lvText.text = "Lv." + lv;
        if (levelDifference > 0)
        {
            Debug.Log($"레벨업! {levelDifference}레벨 상승. 현재 레벨: {lv}");
        }
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
        equipmentData.EnsureArmorSlots();
        if (item is ArmorItem armor && !EquipmentData.IsValidArmorType(armor.armorType))
        {
            Debug.LogWarning("유효하지 않은 방어구 부위입니다.");
            return;
        }
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
                    equipmentData.weaponItem = null;
                }
                equipmentData.weaponItem = weaponItem;
                break;
            case ItemCategory.Armor:
                ArmorItem armorItem = item as ArmorItem;
                if (equipmentData.armorItem[(int)armorItem.armorType] != null)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.armorItem[(int)armorItem.armorType] = null;
                }
                equipmentData.armorItem[(int)armorItem.armorType] = armorItem;
                break;
            case ItemCategory.Accessory:
                AccessoryItem accessoryItem = item as AccessoryItem;
                if (equipmentData.accessoryItem != null)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.accessoryItem = null;
                }
                equipmentData.accessoryItem = accessoryItem;
                break;
            default:
                Debug.LogWarning("알 수 없는 장비 유형입니다.");
                break;
        }
        //장착 후 인벤토리 UI 업데이트

        //장착 후 스탯 업데이트.
        UpdateTuningStats();
    }
    //장비 아이템 장착 해제시 적용
    public void ReleaseItem(EquipmentItem item)
    {
        if (item == null || equipmentData == null) return;
        equipmentData.EnsureArmorSlots();
        switch (item.itemCategory)
        {
            case ItemCategory.Weapon:
                WeaponItem weaponItem = item as WeaponItem;
                if (weaponItem != null && equipmentData.weaponItem == weaponItem)
                {
                    //장착중인 것이 확인되었으니 장착 해제
                    equipmentData.weaponItem = null;
                }
                break;
            case ItemCategory.Armor:
                ArmorItem armorItem = item as ArmorItem;
                if (armorItem != null && EquipmentData.IsValidArmorType(armorItem.armorType) &&
                    equipmentData.armorItem[(int)armorItem.armorType] == armorItem)
                {
                    //기존 장착 아이템 해제.
                    equipmentData.armorItem[(int)armorItem.armorType] = null;
                }
                break;
            case ItemCategory.Accessory:
                AccessoryItem accessoryItem = item as AccessoryItem;
                if (accessoryItem != null && equipmentData.accessoryItem == accessoryItem)
                {
                    equipmentData.accessoryItem = null;
                }
                break;
            default:
                Debug.LogWarning("알 수 없는 장비 유형입니다.");
                break;
        }
        //장착 해제 후 인벤토리 UI 업데이트

        //장착 해제 후 스탯 업데이트
        UpdateTuningStats();
    }


    public void RestoreEquipment(EquipmentData restoredEquipment)
    {
        int savedHP = currentHP;
        equipmentData = restoredEquipment?.Copy() ?? new EquipmentData();
        UpdateTuningStats();
        SetCurrentHPAndNotify(savedHP);
    }

    #endregion

    #region [Tuning Control]
    public override void UpdateTuningStats()
    {
        if (equipmentData == null) equipmentData = new EquipmentData();
        equipmentData.EnsureArmorSlots();
        tuningStats = ApplyStatusEffects(GetEquipmentTuningStats());
        UpdateStats();
    }

    // 실제 계산과 상세 UI가 동일한 장비 합산 결과를 사용한다.
    public TuningStats GetEquipmentTuningStats()
    {
        var result = new TuningStats();
        if (equipmentData == null) return result;
        if (equipmentData.weaponItem != null)
        {
            result.attackBonus = equipmentData.weaponItem.attackBonus;
            result.rangeBonus = equipmentData.weaponItem.range;
        }
        if (equipmentData.armorItem != null)
            foreach (var item in equipmentData.armorItem)
            {
                if (item == null) continue;
                result.hpBonus += item.hpBonus;
                result.dodgeBonus += item.dodgeBonus;
            }
        if (equipmentData.accessoryItem != null)
            result.dodgeBonus += equipmentData.accessoryItem.dodgeBonus;
        return result;
    }
    #endregion

    #region [BaseStats Control]

    public void UpdateBaseStats(string name, int amount)
    {
        switch (name) {
            case "str" :
                baseStats.str += amount;
                break;
            case "con" :
                baseStats.con += amount;
                break;
            case "dex" :
                baseStats.dex += amount;
                break;
            default :
                Debug.Log("<color = blue>[Player.cs]</color> UpdateBaseStats 입력 오류가 발생하였습니다. 정확한 baseStats name을 입력하세요.");
                break;
        }
        UpdateStats(true);
    }
    #endregion
}
