using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon System/Weapon")]
public class Weapon : ScriptableObject
{
    public string weaponName; // 무기 이름
    public float damage; // 무기 데미지
    public int range; // 무기 사거리
    public int durability; // 무기 내구도

    // 무기의 내구도를 소모하는 함수
    public bool UseWeapon()
    {
        durability--;
        if (durability <= 0)
        {
            return true;
        }
        return false;
    }

}