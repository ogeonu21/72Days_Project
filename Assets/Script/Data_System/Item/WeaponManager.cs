using UnityEngine;

public class WeaponManager
{
    /*private Weapon currentWeapon;
    private readonly Player player;
    private WeaponDataCollection weaponData;

    public WeaponManager(Player owner)
    {
        player = owner;
        LoadWeaponData();
    }

    private void LoadWeaponData()
    {
        TextAsset json = Resources.Load<TextAsset>("WeaponData");
        if (json == null)
        {
            Debug.LogError("WeaponData.json not found in Resources.");
            return;
        }

        weaponData = JsonUtility.FromJson<WeaponDataCollection>(json.text);
        if (weaponData == null || weaponData.weapons == null)
        {
            Debug.LogError("WeaponData 파싱 실패. JSON 구조 확인 필요.");
        }
    }

    public Weapon CreateWeaponFromData(string weaponName)
    {
        if (weaponData == null || weaponData.weapons == null) return null;

        var data = weaponData.weapons.Find(w => w.weaponName == weaponName);
        if (data == null)
        {
            Debug.LogWarning($"Weapon '{weaponName}' not found.");
            return null;
        }

        return new Weapon
        {
            weaponName = data.weaponName,
            damage = data.damage,
            range = data.range,
            durability = data.durability
        };
    }

    public void Equip(Weapon weapon)
    {
        currentWeapon = weapon;
        player.attack = player.originalAttack + weapon.damage;
        player.attackDistance = player.originalAttackDistance + weapon.range;

        Debug.Log($"{player.characterName} 장착: {weapon.weaponName} | 공격력: {player.attack} | 사거리: {player.attackDistance}");
    }

    public void ReduceDurability()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.UseWeapon())
        {
            Debug.Log("무기 파괴됨. 맨주먹으로 교체.");
            var fallback = CreateWeaponFromData("맨주먹");
            if (fallback != null) Equip(fallback);
        }
    }

    public Weapon GetCurrentWeapon() => currentWeapon;*/
}
