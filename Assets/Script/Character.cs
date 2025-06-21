using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Character : MonoBehaviour
{

    [SerializeField]
    public Slider _hpBar;
    public TMP_Text _hpText;
    public TMP_Text nameText;

    [SerializeField]
    public float hp; //hp
    public float maxHP; //최대hp
    public float attack; //공격력
    public float avd; //회피율, %형태
    public int attackDistance = 1; //공격 사거리
    public float originalAttack;

    //디버프 관련 효과
    public float originalAvd; //원래 회피율
    private int evasionDebuffTurns;  //회피율 디버프 남은 턴 수
    public int originalAttackDistance; //원래 공격거리
    private int distanceDebuffTurns; //사거리 디버프 남은 턴 수

    public string name;

    public bool die = false;
    
    void Update()
    {
        _hpBar.value = (float)hp / maxHP;
        
    }

    //이름을 불러오는 함수
    protected void LoadName()
    {
        //만약, 함수를 호출한 오브젝트의 이름이 Player라면 Player 스테이터스 불러오기
        if (this.gameObject.name == "Player")
        {
            name = "당신";
        }
        else
        {
            name = LoadStageNum();
        }
    }

    protected string LoadStageNum()
    {
        StageEnemyName selectedCharacter = GameManager.Instance.stageDatas.stages[GameManager.Instance.currentStageNumber];

        if (selectedCharacter != null)
        {
            GameManager.Instance.currentStageNumber++;
            return selectedCharacter.name;
        }
        else
        {
            Debug.LogError("Character with name not found.");
        }
        return "error";
    }

    //스테이터스 정보를 불러오는 함수
    protected void LoadStatus(string characterName)
    {
        StatusData selectedCharacter = GameManager.Instance.characterDataCollection.characters.Find(character => character.name == characterName);
        if (selectedCharacter.type == "event")
        {
            Debug.Log("해당 character은 event character입니다. 특수 스크립트를 준비하세요.");
            GameManager.Instance.Invoke("nextStage", 5.0f);
        }
        else
        {
            if (selectedCharacter != null)
            {

                name = selectedCharacter.name;

                //플레이어의 스탯에 추가 스탯을 더하는 코드
                if (this.gameObject.name == "Player")
                {
                    selectedCharacter.str += Random.Range(3, 9);
                    selectedCharacter.dex += Random.Range(3, 9);
                    selectedCharacter.hpState += Random.Range(3, 11);

                }

                // 선택된 캐릭터의 스탯을 설정
                hp = selectedCharacter.str * 4.0f + selectedCharacter.hpState * 9.0f;
                attack = selectedCharacter.str * 2.0f + selectedCharacter.dex * 1.0f;
                avd = selectedCharacter.dex * 0.3f;
                maxHP = hp;
                attackDistance = Random.Range(0, 3) + 1;
                originalAttack = attack;
                originalAttackDistance = attackDistance;
                originalAvd = avd;

                Debug.Log($"Character {name} loaded with HP: {hp}, Attack: {attack}, Avoidance: {avd}, str: {selectedCharacter.str}, dex: {selectedCharacter.dex}, hpState: {selectedCharacter.hpState}");
            }
            else
            {
                Debug.LogError($"Character with name {characterName} not found.");
            }
        }
    }

    public void StatusPlus(int reward)
    {
        hp += reward * 9.0f;
        maxHP += reward * 9.0f;
        _hpBar.value = (float)hp / maxHP;
        _hpText.text = hp.ToString() + "/" + maxHP.ToString();
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        Debug.Log($"{name} take damage : {damage}");
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
        _hpBar.value = (float)hp / maxHP;
        _hpText.text = hp.ToString() + "/" + maxHP.ToString();
    }

    public void ApplyEvasionDebuff(float evasionReduction)
    {
        avd = originalAvd - evasionReduction;    // 회피율 감소
        evasionDebuffTurns = 3;         // 2턴 동안 지속
    }
    public void ApplyDistanceDebuff()
    {
        attackDistance -= 1;    // 사거리 감소
        distanceDebuffTurns = 3;         // 2턴 동안 지속
    }

    public void Die()
    {
        Debug.Log($"{name} has died.");
        die = true;
        // 캐릭터가 죽으면 게임 오버 처리 (패배 또는 승리)
    }

    public void EndTurn()
    {
        if (evasionDebuffTurns >= 1)
        {
            evasionDebuffTurns--;

            if (evasionDebuffTurns == 0)
            {
                avd = originalAvd;  // 디버프 끝나면 회피율 복구
            }
        }
        if (distanceDebuffTurns > 0)
        {
            distanceDebuffTurns--;
            if (distanceDebuffTurns == 0)
            {
                attackDistance = originalAttackDistance;
            }
        }
    }


}
