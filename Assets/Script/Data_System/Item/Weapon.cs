using UnityEngine;

[System.Serializable]
public class Weapon
{
    public string weaponName;
    public float damage;
    public int range;
    public int durability;

    public bool UseWeapon()
    {
        durability--;
        Debug.Log($"{weaponName} 사용됨. 남은 내구도: {durability}");
        return durability <= 0;
    }
}
