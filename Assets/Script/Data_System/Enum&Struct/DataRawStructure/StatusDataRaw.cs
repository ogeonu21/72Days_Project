using UnityEngine;

[System.Serializable]
public class StatusDataRaw
{
    //기본 정보
    public string Type, ID, Name;
    //스탯 정보
    public int STR, DEX, CON;
    //보너스 정보
    public int AttackBonus, HpBonus, DodgeBonus, RangeBonus;
    //드랍테이블
    public string DropItemCategory;
    public string DropItemID;
    public float ItemDropRate;
    public int DropGold;



}