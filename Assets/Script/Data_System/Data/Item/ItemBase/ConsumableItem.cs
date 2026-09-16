using UnityEngine;

[System.Serializable]
public class ConsumableItem : BaseItem
{
    public int quantity; //수량

    // public int weaponDamageAmount;
    // public int weaponAttackDistance;
    public override void Equip(Player player)
    {
    }
    public override void Use(Player player)
    {
        if(quantity <= 0){ return;}
    }
}
 