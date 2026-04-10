using UnityEngine;

[System.Serializable]
public abstract class BaseItem : ScriptableObject
{
    public string itemID;
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public bool isConsumable;
    public int itemValue;
    public ItemCategory itemCategory;

    // 아이템 사용 시 호출될 공통 메서드
    public abstract void Use(Player player);
    
}