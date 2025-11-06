
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RewardSystem : SingleTon<RewardSystem>
{
    public void OnEnable()
    {
        RewardEvent.OnRewardProcess += ProcessReward;
    }
    public void OnDisable()
    {
        RewardEvent.OnRewardProcess -= ProcessReward;
    }

    private IEnumerator ProcessReward(Enemy enemy, Player player)
    {
        yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 {enemy.GetExpReward()}만큼의 경험치를 획득하였다.");
        yield return StartCoroutine(WaitForClick.WaitClick());

        player.GetExp(enemy.GetExpReward());

        if(CalculateFunction.Roll(0.2f))
        {


            
            yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 흠...을 획득하였다.");
            yield return StartCoroutine(WaitForClick.WaitClick());

        }
        yield return null;

    }

    //보상 활성화

    //전투 노드중인가?
        

    //보상 목록 체크
        //아이템, 경험치, 골드
        //경험치는 몹의 레벨 비례.
        //아이템은 20% 확률로 획득.
        //골드는 몹의 레벨과 진행 일자에 비례.
    
    //보상 지급

    //보상 비활성화

}
