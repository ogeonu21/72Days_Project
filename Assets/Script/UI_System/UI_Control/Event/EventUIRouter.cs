using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 종류별 패널의 표시만 담당한다. 지급과 이동은 이벤트 실행부에 위임한다.</summary>
public sealed class EventUIRouter : ChoiceUIRouter
{
    //노드 설정
    private EventNode node;
    private bool awaitingResult;
    [SerializeField] private EventResultLog resultLog;
    [SerializeField] private TMP_Text resultText;
    private Coroutine resultPresentation;

    //버튼 리스트
    private readonly List<(EventOption option, Button button)> optionButtons = new List<(EventOption, Button)>();

    //플레이어 오브젝트
    private Player observedPlayer;

    //매니저
    private InventoryManager observedInventory;
    private EventManager observedEvents;

    //버튼 표현
    public void Present(EventNode value, Button buttonTemplate, TMP_FontAsset textFont)
    {
        //왜 맨 처음에 숨기고 시작할까.
        Hide();
        //값을 설정하고
        node = value; template = buttonTemplate; font = textFont;
        //오류 체크하고
        if (node == null || template == null) return;

        if (node.definition == null || !OpenChoices(node.definition.kind.ToString(), buttonTemplate, textFont)) return;
        //관찰자, 매니저 설정
        observedPlayer = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        observedInventory = InventoryManager.Instance;
        observedEvents = EventManager.Instance;
        //이벤트 구독
        if (observedPlayer != null) observedPlayer.TendencyChanged += OnTendencyChanged;
        if (observedInventory != null) observedInventory.InventoryChanged += RefreshAvailability;
        if (observedEvents != null) observedEvents.ProgressChanged += RefreshAvailability;
        PlayerEvent.onStatsChanged += RefreshAvailability;
        CurrencyEvent.OnCurrencyChanged += OnCurrencyChanged;
        //옵션을 보여줌.
        ShowOptions();
    }

    public void Hide()
    {
        if (resultPresentation != null) StopCoroutine(resultPresentation);
        resultPresentation = null;
        if (resultText != null) resultText.gameObject.SetActive(false);
        var controller = GetComponent<EventUIController>();
        if (controller != null && controller.dialogueText != null) controller.dialogueText.gameObject.SetActive(true);
        //숨길때 이벤트 구독을 해제
        if (observedPlayer != null) observedPlayer.TendencyChanged -= OnTendencyChanged;
        if (observedInventory != null) observedInventory.InventoryChanged -= RefreshAvailability;
        if (observedEvents != null) observedEvents.ProgressChanged -= RefreshAvailability;
        PlayerEvent.onStatsChanged -= RefreshAvailability;
        CurrencyEvent.OnCurrencyChanged -= OnCurrencyChanged;
        //연결 해제
        observedPlayer = null; observedInventory = null; observedEvents = null;
        //optionButtons 리스트 초기화
        optionButtons.Clear();
        //패널까지 싹다 비활성화
        HideChoices();
        awaitingResult = false;
    }
    //비활성화될 때, Hide()로 숨김.
    protected override void OnDisable() => Hide();

    //옵션을 보여줌.
    private void ShowOptions()
    {
        awaitingResult = false;
        //새로운 컨텐츠를 만듬.
        optionButtons.Clear();
        var content = NewContent();
        //니놈이로구나!! EventDefinition의 title을 띄우는 것이!!!
        //Label(content, string.IsNullOrWhiteSpace(node.definition.title) ? node.definition.kind.ToString() : node.definition.title);
        foreach (var option in node.definition.options)
        {
            var captured = option;
            string label = option.text + (option.goldCost > 0 ? $"  ·  {option.goldCost} 골드" : "");
            var button = AddChoice(content, label, () => Choose(captured));
            button.name = "EventAction_" + option.id;
            optionButtons.Add((option, button));
        }
        // 이미 수령한 이벤트를 재방문해도 출구를 제공한다.
        if (node.definition.exitNode != null)
            //떠나기 버튼을 제공하네?
            //이게 문제야.
            AddChoice(content, "떠나기", () => NodeManager.Instance.GoToNode(node.definition.exitNode));
        RefreshAvailability();
    }

    private void OnTendencyChanged(int value) => RefreshAvailability();
    private void OnCurrencyChanged(CurrencyData value) => RefreshAvailability();

    public void RefreshAvailability()
    {
        if (awaitingResult || activePanel == null || !activePanel.activeSelf) return;
        foreach (var entry in optionButtons)
        {
            string reason = observedEvents != null ? observedEvents.GetUnavailableReason(node, entry.option) : "이벤트 시스템을 불러오는 중입니다.";
            entry.button.gameObject.SetActive(reason == null);
            //entry의 button의 interactable이 비활성화되어있는가 활성화되어있는가?
            //entry.button.interactable = reason == null;
            /*
            //이 부분은 미충족 사유를 보여주기 위한 것이잖아. 지금은 불필요해.
            var text = entry.button.GetComponentInChildren<TMP_Text>(true);
            text.text = entry.option.text + (entry.option.goldCost > 0 ? $" · 비용 {entry.option.goldCost} 골드" : "")
                + (reason == null ? "" : "\n[잠김] " + reason);
            text.color = reason == null ? Color.black : new Color(1f, 1f, 1f);
            // 여러 조건의 미충족 사유가 잘리지 않도록 버튼 높이를 늘린다.
            var layout = entry.button.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = Mathf.Max(95, 48 + text.text.Split('\n').Length * 48);
            */
           
        }
    }

    private void Choose(EventOption option)
    {
        if (awaitingResult || option == null) return;
        var controller = GetComponent<EventUIController>();
        if (resultLog == null || controller == null || controller.dialogueText == null)
        {
            Debug.LogError("[EventUI] 결과 로그 또는 dialogue 텍스트 연결이 없습니다.");
            return;
        }
        awaitingResult = true;
        activePanel.SetActive(false);
        var result = EventManager.Instance.Execute(node, option);
        resultLog.Enqueue(result.message);
        resultPresentation = StartCoroutine(PresentResult(result.completedQuestOption ?? option, result.success, result.chanceFailed));
    }

    private IEnumerator PresentResult(EventOption option, bool success, bool chanceFailed = false)
    {
        // 선택 버튼 클릭이 결과 확인 입력으로 재사용되지 않도록 프레임을 분리한다.
        yield return null;
        var controller = GetComponent<EventUIController>();
        var dialogue = controller != null ? controller.dialogueText : null;
        string narrative = success ? option.resultText : (chanceFailed ? option.failureText : null);
        if (success && option.action == EventActionKind.CompleteQuest && string.IsNullOrWhiteSpace(narrative))
            narrative = "퀘스트 완료: " + option.questId;
        if (!string.IsNullOrWhiteSpace(narrative))
        {
            dialogue.gameObject.SetActive(true);
            yield return TypewriterEffect.TypeTextCoroutine(dialogue, narrative);
            // 공통 대기 함수가 기존 터치 해제와 새 입력을 구분한다.
            yield return WaitForClick.WaitClick();
        }
        resultPresentation = null;
        if (NodeManager.Instance == null || NodeManager.Instance.currentNode != node) yield break;
        if (success && option.nextNode != null) NodeManager.Instance.GoToNode(option.nextNode);
        else
        {
            activePanel.SetActive(true);
            ShowOptions();
        }
    }
}
