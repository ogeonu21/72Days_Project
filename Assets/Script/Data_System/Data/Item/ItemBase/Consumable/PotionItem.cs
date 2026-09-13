using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Potion", menuName = "Items/Potion")]
public class PotionItem : BaseItem
{

    public int health;


    public override void Use(Player player)
    {
        if (isConsumable)
        {
            //인벤토리에서 아이템 제거.
            player.Heal(health);
        }
    }

}
