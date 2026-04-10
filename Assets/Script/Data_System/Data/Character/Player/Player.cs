using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Security.AccessControl;

public class Player : Character
{
    #region [경험치 배율]
    private const float BASE_EXP = 12.1f;
    private const float EXP_GROWTH_RATE = 1.33f;
    #endregion

    #region [플레이어 경험치 & 장비 데이터]
    //레벨 관련
    private int exp;
    public int lv;

    //장비 관련 - 추후 구현 예정
    public EquipmentData equipmentData;

    #endregion


    //public event Action OnPlayerLvUp;

    #region [Initialize]
    public void InitializeFromData(PlayerData data)
    {
        if (data == null) return;

        ID = string.IsNullOrWhiteSpace(data.id) ? ID : data.id;
        characterName = string.IsNullOrWhiteSpace(data.displayName) ? characterName : data.displayName;

        baseStats = data.baseStats;
        tuningStats = data.tuningStats;

        exp = data.exp;
        lv = data.lv;

        UpdateStats();
        SetCurrentHPAndNotify(MaxHP);
        UpdateLV_UI();
    }


    //인자를 하나 더 받자. initialmode, loadmode.

    public void LoadFromData(PlayerData data)
    {
        if (data == null) return;

        ID = string.IsNullOrWhiteSpace(data.id) ? ID : data.id;
        characterName = string.IsNullOrWhiteSpace(data.displayName) ? characterName : data.displayName;

        baseStats = data.baseStats;
        tuningStats = data.tuningStats;

        exp = data.exp;
        lv = data.lv;

        UpdateStats();
        SetCurrentHPAndNotify(data.currentHP);
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

        return data;
    }
    #endregion

    #region [Override]

    //데미지 피격 함수.
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        GameEvent.OnTakeDamage(currentHP, MaxHP);
    }
    #endregion

    #region [LV Control]

    //경험치를 올리는 함수.
    public void GetExp(int exp)
    {
        this.exp += exp;
        UpdateLv();
    }


    private void UpdateLv()
    {
        int requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));

        if (exp >= requiredExpForLvUP)
        {
            exp -= requiredExpForLvUP;
            lv++;

            //10만큼 회복.
            Heal(10);


            UpdateLV_UI();

            //레벨업 이벤트 발생.
            GameEvent.PlayerLevelUp();
        }
    }

    private void UpdateLV_UI()
    {
        lvText.text = "Lv." + lv;
    }
    #endregion

    #region [Equipment Control]
    public void EquipItem(EquipmentItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("장착할 아이템이 없습니다.");
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

    #endregion

    #region [Tuning Control]
    public override void UpdateTuningStats()
    {
        //장비 스탯 적용
        tuningStats.attackBonus = equipmentData.weaponItem != null ? equipmentData.weaponItem.attackBonus : 0;
        tuningStats.hpBonus = equipmentData.armorItem != null ? equipmentData.armorItem.hpBonus : 0;
        tuningStats.dodgeBonus = (equipmentData.armorItem != null ? equipmentData.armorItem.dodgeBonus : 0) + (equipmentData.accessoryItem != null ? equipmentData.accessoryItem.dodgeBonus : 0);

        //특수 효과에 따른 스탯 조정.
        tuningStats.attackBonus = tuningStats.attackBonus + (EffectTurn[0] > 0 ? -5 : 0);
        tuningStats.dodgeBonus = tuningStats.dodgeBonus + (EffectTurn[1] > 0 ? -0.05f : 0);

        //최종 스탯 업데이트.
        UpdateStats();
    }
    #endregion
}

