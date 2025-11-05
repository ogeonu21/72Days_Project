using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvent
{
    //데미지 피격 이벤트
    public static event Action<int, int> OnTakeDamageEffect;
    public static void OnTakeDamage(int currentHP, int maxHP) => OnTakeDamageEffect?.Invoke(currentHP, maxHP);

    //노드 변경 이벤트
    public static event Action<Node> OnNodeChanged;
    public static void NotifyNodeChange(Node node) => OnNodeChanged?.Invoke(node);

    //캐릭터 UI 변경 이벤트
    public static event Action<Player, Enemy> OnCharacterUIChanged;
    public static void UpdateCharacterUI(Player player, Enemy enemy) => OnCharacterUIChanged?.Invoke(player, enemy);

    //Player 레벨업 이벤트
    public static event Action OnPlayerLevelUp;
    public static void PlayerLevelUp() => OnPlayerLevelUp?.Invoke();

    //재화 변경 이벤트
    public static event Action<CurrencyData> OnCurrencyChanged;
    public static void CurrencyChanged(CurrencyData data) => OnCurrencyChanged?.Invoke(data);

    //Node Text 변경 이벤트
    //각 UI컨트롤러별 NodeText 할당과 NodeTextUpdate 구독 필요
    public delegate IEnumerator OnNodeTextChanged(string text);
    public static event OnNodeTextChanged NodeTextUpdate;
    public static IEnumerator OnNodeTextUpdate(string text){
        yield return NodeTextUpdate?.Invoke(text);
    } 

}
