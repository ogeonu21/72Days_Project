using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Potion", menuName = "Items/Consumable/Potion")]
public class PotionItem : ConsumableItem
{

    public int health;


    public override void Use(Player player)
    {
        if(quantity > 0){
            if (isConsumable)
            {
                //인벤토리에서 아이템 제거.
                player.Heal(health);
                quantity--;
            }
        }
        if(quantity == 0)
        {
            //파괴 동작 실행.
            InventoryManager.Instance.RemoveFromInventory(this);
        }
        else InventoryManager.Instance.UpdateItemUI(this);
    }

}
