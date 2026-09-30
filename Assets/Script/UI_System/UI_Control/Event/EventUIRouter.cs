using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 종류별 패널의 표시만 담당한다. 지급과 이동은 이벤트 실행부에 위임한다.</summary>
public sealed class EventUIRouter : MonoBehaviour
{
    //이건 뭐지?
    private readonly Dictionary<EventKind, GameObject> panels = new Dictionary<EventKind, GameObject>();

    //노드 설정
    private EventNode node;
    //UI설정
    private Button template;
    private TMP_FontAsset font;
    private GameObject activePanel;

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

        //kinde 값을 out소싱하고
        if (!panels.TryGetValue(node.definition.kind, out activePanel))
        {
            //오브젝트를 만들어서 부모 설정하고
            activePanel = new GameObject(node.definition.kind + "EventPanel", typeof(RectTransform), typeof(Image));
            //앵커 설정
            activePanel.transform.SetParent(transform, false);
            var rect = (RectTransform)activePanel.transform;
            rect.anchorMin = new Vector2(.05f, .04f); rect.anchorMax = new Vector2(.95f, .43f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            //색상 설정할건데 완전 쌩투명으로
            activePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            //생성한 것을 panel에 더함
            panels.Add(node.definition.kind, activePanel);
        }
        //활성화
        activePanel.SetActive(true);

        activePanel.transform.SetAsLastSibling();
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
        foreach (var panel in panels.Values) if (panel != null) panel.SetActive(false);
        awaitingResult = false;
    }
    //비활성화될 때, Hide()로 숨김.
    private void OnDisable() => Hide();

    //새로운 콘텐츠 만들기
    private Transform NewContent()
    {
        //optionBUttons 리스트 초기화
        optionButtons.Clear();
        //activePanel의 자식 수만큼 반복
        for (int i = activePanel.transform.childCount - 1; i >= 0; i--)
        {
            //child는 i번째 자식 오브젝트
            var child = activePanel.transform.GetChild(i);
            //비활서화 해두고
            child.gameObject.SetActive(false);
            //앱이 작동중이면 파괴한다고? 아하 초기화하려고
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        //스크롤을 생성함. 즉, 영역을 만들어서 스크롤할 수 있는 환경을 만드는 것임.
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollObject.transform.SetParent(activePanel.transform, false);
        //앵커 설정.
        var rect = (RectTransform)scrollObject.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 12); rect.offsetMax = new Vector2(-12, -12);
        //색상 설정. 완전 쌩투명으로 한다.
        scrollObject.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        //콘텐츠를 생성함
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        //컨텐츠의 자식관계 설정
        content.transform.SetParent(rect, false);
        //컨텐츠의 앵커 설정
        var cr = (RectTransform)content.transform;
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1);
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        //수직정렬그룹 component를 가진 layout의 값을 설정하고, layout을 설정하는거임.
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true;
        //얘를 true로 하면?
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = rect; scroll.content = cr; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return cr;
    }

    //parent의 텍스트를 설정함.
    //검은색으로 설정해보자.
    //사실 이 라벨은 필요없는데 말이야 진짜로.
    //이 놈은 이벤트의 title을 만들어내는거임
    //로그 기록도 얘가 담당하네
    private void Label(Transform parent, string value)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = 48; text.text = value; text.raycastTarget = false;
        text.color = Color.white;
    }

    //이번엔 버튼을 만드는 것.
    private Button Button(Transform parent, string label, Action action)
    {
        //템플릿을 가져와서 parent에 button Object 생성.
        var button = Instantiate(template, parent);
        //버튼의 이름과 이벤트리스너를 연결함.
        button.name = "EventAction";
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => action());
        button.gameObject.SetActive(true);
        var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
        //이건 최소 높이인뎅.
        layout.minHeight = layout.preferredHeight = 95;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        //버튼에 들어갈 텍스트의 설정을 건드림.
        text.text = label; text.font = font; text.enableAutoSizing = true; text.fontSizeMin = 48; text.fontSizeMax = 48;
        button.interactable = true;
        return button;
    }

    //옵션을 보여줌.
    private void ShowOptions()
    {
        awaitingResult = false;
        //새로운 컨텐츠를 만듬.
        var content = NewContent();
        //니놈이로구나!! EventDefinition의 title을 띄우는 것이!!!
        //Label(content, string.IsNullOrWhiteSpace(node.definition.title) ? node.definition.kind.ToString() : node.definition.title);
        foreach (var option in node.definition.options)
        {
            var captured = option;
            string label = option.text + (option.goldCost > 0 ? $"  ·  {option.goldCost} 골드" : "");
            var button = Button(content, label, () => Choose(captured));
            button.name = "EventAction_" + option.id;
            optionButtons.Add((option, button));
        }
        // 이미 수령한 이벤트를 재방문해도 출구를 제공한다.
        if (node.definition.exitNode != null)
            //떠나기 버튼을 제공하네?
            //이게 문제야.
            Button(content, "떠나기", () => NodeManager.Instance.GoToNode(node.definition.exitNode));
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
        resultPresentation = StartCoroutine(PresentResult(option, result.success, result.chanceFailed));
    }

    private IEnumerator PresentResult(EventOption option, bool success, bool chanceFailed = false)
    {
        // 선택 버튼 클릭이 결과 확인 입력으로 재사용되지 않도록 프레임을 분리한다.
        yield return null;
        var controller = GetComponent<EventUIController>();
        var dialogue = controller != null ? controller.dialogueText : null;
        string narrative = success ? option.resultText : (chanceFailed ? option.failureText : null);
        if (!string.IsNullOrWhiteSpace(narrative))
        {
            dialogue.gameObject.SetActive(true);
            yield return TypewriterEffect.TypeTextCoroutine(dialogue, narrative);
            // 타이핑 중 누른 채 유지한 터치도 완료 클릭으로 처리하지 않는다.
            while (Input.GetMouseButton(0) || Input.touchCount > 0) yield return null;
            yield return null;
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
