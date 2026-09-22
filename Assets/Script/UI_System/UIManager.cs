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

    //플레이어의 레벨업 이벤트를 반복실행하기 위한 특별 변수. 레벨 변동량을 받아와서 eventExecute를 반복실행
    private int levelDifference = 0;
    //UI를 열 때, 타이므 스토푸를 실행하기 위해 이전 타임 스케일을 저장하기 위한 특별 변수.
    private float prevTimeScale;

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
    }

    private void OnEnable()
    {
        characterSource = CharacterManager.Instance;
        if (characterSource != null)
        {
            characterSource.OnCharacterReady += UpdateCharacter;
            UpdateCharacter(characterSource.currentPlayer, characterSource.currentEnemy);
        }
    }

    private void OnDisable()
    {
        if (characterSource != null)
            characterSource.OnCharacterReady -= UpdateCharacter;
        if (player != null)
            player.StatsChanged -= UpdateCharacterStatsUI;
        player = null;
        enemy = null;
        characterSource = null;
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독을 해지
        GameEvent.OnNodeChanged -= UpdateUI;
        PlayerEvent.OnPlayerLevelUp -= UpdateLevelUpUI;
        CurrencyEvent.OnCurrencyChanged -= UpdateCurrencyUI;
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
    //플레이어나 적 인스턴스에 변화가 있을때, 참조를 다시 연결.
    private void UpdateCharacter(Player player, Enemy enemy)
    {
        if (this.player != null)
            this.player.StatsChanged -= UpdateCharacterStatsUI;
        this.enemy = enemy;
        this.player = player;
        if (this.player != null)
            this.player.StatsChanged += UpdateCharacterStatsUI;
        UpdateCharacterStatsUI();
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
    private void UpdateLevelUpUI(int levelDifference)
    {
        this.levelDifference = levelDifference;
        levelUpModal.Open();
    }
    //이게 왜 이벤트 UI에 연결되어있지?
    public void StatUpEventExecute(BaseEvent baseEvent)
    {
        if (baseEvent == null)
        {
            Debug.LogWarning("[UIManager] 실행할 레벨업 이벤트가 없습니다.");
            return;
        }
        //레벨 변동량이 1보다 큰지 체크. 레벨 변동량이 0보다 크다는 것은 해당 이벤트를 실행할 수 있는 권한이 있다는 의미.
    
        if (this.levelDifference > 0)
        {
            this.levelDifference--;
            baseEvent.Execute();
        }
        //레벨 변동량이 0이라면, 즉 이벤트 실행 권한을 모두 소진했다면 창을 닫음.
        if (this.levelDifference == 0)
        {
            levelUpModal.Close();   
        }
        
    }
    #endregion

    #region [Currency UI Control]
    private void UpdateCurrencyUI(CurrencyData data)
    {
        hudView.ShowCurrency(data);
    }
    #endregion

    #region [Character Stats UI Control]
    
    //얘는 플레이어의 스탯 표기만을 업데이트하는 녀석.
    private void UpdateCharacterStatsUI()
    {
        playerStatsView.Set(player);
    }
    #endregion

    #region [UI Control]
    public void OpenUI(GameObject UI)
    {
        if (UI != null && UI.activeSelf) return;
        if (UI != null)
        {
            //타이므 스토푸!!! 근데 시발 원래 이렇게 하면 안되는뎅
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0;
            
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
            //나중에 중복으로 UI가 활성화될 때 문제가 발생하겠지만.... 아직은 알빠노? 나중에 고쳐라.
            if (Time.timeScale == 0)
            {
                Time.timeScale = prevTimeScale;
            }
            UI.SetActive(false);
        }else
        {
            Debug.LogWarning($"<color=yellow>[UIMANAGER] </color>UI가 이미 활성화되어 있거나 null입니다.</color>");
        }
    }

    #endregion
}
