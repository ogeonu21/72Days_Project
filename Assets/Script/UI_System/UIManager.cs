using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class UIManager : SingleTon<UIManager>
{
    #region [변수 그룹]
    //Object
    [SerializeField]
    private List<UIController> uiControllers = new List<UIController>();
    //다른 Object들도 있어야 함.

    //Instance
    private Player player;
    private Enemy enemy;
    private NodeScreenRouter screenRouter;
    private HudView hudView;
    private PlayerStatsView playerStatsView;
    private LevelUpModal levelUpModal;
    private CharacterManager characterSource;

    #region [UI 그룹]
    [Header("Level Up UI")]
    [SerializeField] private GameObject levelUpUI;

    [Header("State UI")]
    [SerializeField] private TMP_Text dayCountText;
    [SerializeField] private TMP_Text locationText;

    [Header("Currency UI")]
    [SerializeField] private TMP_Text goldText;

    [Header("Inventory Character UI")]
    [SerializeField] private TMP_Text inventoryCharacterSTRText;
    [SerializeField] private TMP_Text inventoryCharacterDEXText;
    [SerializeField] private TMP_Text inventoryCharacterCONText;
    #endregion

    #endregion

    #region [initialize]
    protected override void Awake()
    {
        base.Awake();
        hudView = new HudView(dayCountText, locationText, goldText);
        playerStatsView = new PlayerStatsView(inventoryCharacterSTRText, inventoryCharacterDEXText, inventoryCharacterCONText);
        levelUpModal = new LevelUpModal(levelUpUI);
        InitializeUIControllers();
        screenRouter = new NodeScreenRouter(uiControllers);
        GameEvent.OnNodeChanged += UpdateUI;
        PlayerEvent.OnPlayerLevelUp += UpdateLevelUpUI;
        CurrencyEvent.OnCurrencyChanged += UpdateCurrencyUI;
        PlayerEvent.onStatsChanged += UpdateCharacterStatsUI;

        characterSource = CharacterManager.Instance;
        if (characterSource != null)
            characterSource.OnCharacterReady += UpdateCharacter;
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독을 해지
        GameEvent.OnNodeChanged -= UpdateUI;
        PlayerEvent.OnPlayerLevelUp -= UpdateLevelUpUI;
        CurrencyEvent.OnCurrencyChanged -= UpdateCurrencyUI;
        PlayerEvent.onStatsChanged -= UpdateCharacterStatsUI;
        levelUpModal?.Close();
        if (characterSource != null)
        {
            characterSource.OnCharacterReady -= UpdateCharacter;
        }

    }

    private void InitializeUIControllers()
    {
        foreach (var controller in uiControllers)
        {
            if (controller == null)
            {
                Debug.LogError("UI 컨트롤러 목록에 null이 있습니다. Inspector를 확인해주세요!");
            }
        }
    }

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
        playerStatsView.Show(player);
    }
    #endregion

    #region [Node UI Control]
    private void UpdateUI(Node node)
    {
        if (node == null) return;
        hudView.ShowNode(node);
        screenRouter.Show(node, enemy);
    }

    #endregion

    #region [UI_Func]
    public void OnPauseButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToMain();
        }
    }
    #endregion

    #region [Lv UI Control]
    private void UpdateLevelUpUI()
    {
        levelUpModal.Open();
    }

    public void EventExecute(BaseEvent baseEvent)
    {
        if (baseEvent == null)
        {
            Debug.LogWarning("[UIManager] 실행할 레벨업 이벤트가 없습니다.");
            return;
        }
        levelUpModal.Close();
        baseEvent.Execute();
        
    }
    #endregion

    #region [Currency UI Control]
    private void UpdateCurrencyUI(CurrencyData data)
    {
        hudView.ShowCurrency(data);
    }
    #endregion

    #region [Character Stats UI Control]
    private void UpdateCharacterStatsUI()
    {
        playerStatsView.Show(player);
    }
    #endregion

    #region [UI Control]
    public void OpenUI(GameObject UI)
    {
        if (UI != null)
        {
            
            Debug.Log($"<color=yellow>[UIMANAGER] </color>UI가 활성화되었습니다: {UI.name}</color>");
            UI.SetActive(true);    
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[UIMANAGER] </color>UI가 이미 활성화되어 있거나 null입니다.</color>");
        }
    }
    public void CloseUI(GameObject UI)
    {
        if(UI != null)
        {
            UI.SetActive(false);
        }else
        {
            Debug.LogWarning($"<color=yellow>[UIMANAGER] </color>UI가 이미 활성화되어 있거나 null입니다.</color>");
        }
    }

    #endregion
}
