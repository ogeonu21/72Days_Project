using System;
using System.Collections.Generic;

[Serializable]
public class WeaponData
{
    public string weaponName;
    public float damage;
    public int range;
    public int durability;
}

[Serializable]
public class WeaponDataCollection
{
    public List<WeaponData> weapons;
}
