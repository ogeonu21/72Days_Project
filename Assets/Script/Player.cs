using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public float damage;
    public int range;
    public int durability;
}
[System.Serializable]
public class WeaponDataCollection
{
    public List<WeaponData> weapons;
}

public class Player : Character
{
    [SerializeField]
    public TMP_Text equipWeaponText;
    public Weapon equippedWeapon;
    public Image bloodEffectImage;

    private WeaponDataCollection weaponDataCollection;
    
    public void ReLoadObject()
    {
        LoadName();
        LoadStatus(name); //특정 캐릭터 이름의 스테이터스 정보를 불러옴
        if (nameText != null)
        {
            nameText.text = name;
            _hpBar.value = (float)hp / maxHP;
            _hpText.text = hp.ToString() + "/" + maxHP.ToString();
        }
        originalAttack = attack;
        originalAttackDistance = attackDistance;

        Weapon initialWeapon = CreateWeapon("맨주먹");
        if (initialWeapon != null)
        {
            EquipWeapon(initialWeapon);
        }
        else
        {
            Debug.LogError("무기 장착에 실패하였습니다.");
        }

    }

    public void EquipWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;
        attack = originalAttack + equippedWeapon.damage;
        attackDistance = originalAttackDistance + equippedWeapon.range;
    }

    public void durabilityReduce()
    {
        if (equippedWeapon != null)
        {
            bool breakable = equippedWeapon.UseWeapon();
            if (breakable)
            {
                attack = originalAttack;
                attackDistance = originalAttackDistance;
                Weapon initialWeapon = CreateWeapon("맨주먹");
                if (initialWeapon != null)
                {
                    Debug.Log("무기가 파괴되었습니다.");
                    EquipWeapon(initialWeapon);
                }
                else
                {
                    Debug.LogError("무기 장착에 실패하였습니다.");
                }
            }
        }
    }

    public Weapon CreateWeapon(string name)
    {
        // WeaponData 스크립터블 오브젝트 인스턴스 생성
        Weapon newWeapon = ScriptableObject.CreateInstance<Weapon>();
        
        TextAsset jsonFile = Resources.Load<TextAsset>("WeaponData");

        if (jsonFile != null)
        {
            // JSON 파일을 CharacterDataCollection 객체로 파싱
            weaponDataCollection = JsonUtility.FromJson<WeaponDataCollection>(jsonFile.text);
            WeaponData selectedWeapon = weaponDataCollection.weapons.Find(weapon => weapon.weaponName == name);
            if (selectedWeapon != null)
            {
                newWeapon.weaponName = selectedWeapon.weaponName;
                newWeapon.damage = selectedWeapon.damage;
                newWeapon.range = selectedWeapon.range;
                newWeapon.durability = selectedWeapon.durability;
                Debug.Log($"무기 {newWeapon.weaponName} 생성됨. 데미지: {newWeapon.damage}, 사거리: {newWeapon.range}, 내구도: {newWeapon.durability}");
                return newWeapon;
            }
            else
            {
                Debug.Log("무기를 찾는데 실패하였다.");
            }
            return null;
        }
        else
        {
            Debug.LogError("CharacterStatus.json file not found in Resources.");
            return null;
        }

        
    }


    public void Attaking(string attackArea)
    {
        if (BattleManager.Instance.isPlayerTurn)
        {
            BattleManager.Instance.isPlayerTurn = false;
            BattleManager.Instance.turnStart(attackArea);
        }
        else
        {
            Debug.Log("아직 플레이어의 턴이 아니다. 기다리자.");
        }

    }

    // Update is called once per frame
    void Update()
    {
        equipWeaponText.text = "장착무기: " + equippedWeapon.weaponName + "\n" + "남은 내구도 : " + equippedWeapon.durability;
    }

    public IEnumerator BloodEffect()
    {
        Color color = bloodEffectImage.color;
        color.a = 1 - (float)hp / maxHP;
        while (color.a > 0.0f)
        {
            color.a -= Time.deltaTime / 1.5f;
            bloodEffectImage.color = color;
            yield return null;
        }
    }
    
}
