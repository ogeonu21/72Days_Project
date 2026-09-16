using UnityEngine;

[System.Serializable]
public abstract class BaseItem : ScriptableObject
{
    public string itemID;
    public string itemName;
    public string itemDescription;
    [Tooltip("Addressables에 등록된 Sprite 주소입니다.")]
    public string itemIcon;
    public bool isConsumable;
    public int itemValue;
    public ItemCategory itemCategory;

    // 아이템 사용 시 호출될 공통 메서드
    public abstract void Equip(Player player);
    public abstract void Use(Player player);
    
}
