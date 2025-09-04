using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Items/Weapon")]
public class WeaponItem : BaseItem
{
    public int weaponDamageAmount;
    public int weaponAttackDistance;
    public int durability;



    public override void Use(Player player)
    {
        //아이템 장착 함수.
        //튜닝 수치 업데이트.
        //player에게 장착 아이템 정보 제공 및 수치 업데이트.
    }

    public void Release(Player player)
    {
        //아이템 해제.
        //player에게 아이템 해제하도록 명령, 수치 업데이트.
    }
}
 