using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class CombatManager : SingleTon<CombatManager>
{
    #region [변수 그룹]
    private StoryManager storyManager;


    public Enemy enemy;
    public Player player;

    private bool combatActive;
    #endregion

    private new void Awake()
    {
        base.Awake();
        storyManager = StoryManager.Instance;
        storyManager.OnCombatNodeStart += LoadData;

    }

    private void LoadData(Choice node)
    {
        string enemyID = node.combatEnemyID;
        var enemyData = Resources.Load<EnemyDefinition>($"NPCStats/{enemyID}");
        if (enemyData != null)
        {
            enemy?.InitializeFromDefinition(enemyData);
        }
        else
        {
            Debug.LogWarning($"{enemyID} : EnemyDefinition UnFound");
        }
        
    }

    public void StartCombat()
    {
        if (!combatActive)
        {
            //UIUpdate
        }
    }


    //private bool 전투활성화여부

    /*public void StartCombat(){
     * 
     *  if 전투 활성화가 false야?
     *      true로 바꾸고
     *      Enemy Data를 로드.
     *      UI Update.
     *      
     * }
     * 
     * public void EndCombat(){
     *      자 전투 끝!
     *      전투 활성화 여부를 false로 바꾸자.
     * }
     * 
     * public AttackInput(){
     *      플레이어의 공격을 입력받으면 그제서야 턴을 시작해야함.
     * }
     * 
     * private IEnumerator CombatLoopStart(){
     *      if 전투가 활성화 되어있니?
     *          활성화 되어 있지 않으면 yield break;
     *      선공 판별
     *      Enemy 공격 AI돌리기.
     *      첫번째 턴 실행!
     *      살았나? 죽었나?
     *      두번째 턴 실행!
     *      살앗나? 죽었나?
     * }
     * IEnumerator 턴 실행 (공격자가 누구인지 받기)
     *      주사위 굴리기. 적 회피율 받아서 확인.
     *      성공?
     *          yes => 코루틴 시작(특수효과 판정)
     *          no => 넘어가.
     
     *      특수효과 성공?
     *          yes => 데미지 입히기.
     *          코루틴 시작 (공격 메세지 출력 & 특수효과 적중 메세지 출력)
     *          no => 데미지 입히기
     *          코루틴 시작 (공격 메세지 출력)
     *          
     * IEnumerator 특수효과 판정(공격 부위)
     *      주사위 굴리기!
     *      성공?
     *          yes => 특수효과 적용. Charater.
     *          no => 넘어가.
     * 
    */
}
