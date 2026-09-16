using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "AccessoryItem", menuName = "Items/Equipment/Accessory")]
public class AccessoryItem : EquipmentItem
{
    public float dodgeBonus;
    //퀘스트 관련 설정 추가 필요.
    public string questID; // 이 악세서리가 관련된 퀘스트 ID

    
}