using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : MonoBehaviour
{

    public TMP_Text combatText;
    public GameObject Character_Board;
    public GameObject attackButtons;
    public Image blood_Effect;

    private StoryManager storyManager;
    //private CombatManager combatManager;
    public Player player;
    public Enemy enemy;

    
    void Start()
    {
        //StoryManager에서 Combat의 시작을 받아서 UI를 업데이트함
        //CombatManger에서 CombatTextChanged라는 이벤트를 구독, UpdateCombatUI를 실행해야함.

        //이거는 아무래도 UI쪽에서 건드려야겠다.
        player.onHPChanged += UpdateHPUI;
        enemy.onHPChanged += UpdateHPUI;
    }

    public void UpdateCombatUI(Choice node)
    {
        Debug.Log("Detected Event!");
        
    }

    public void UpdateHPUI(int currentHP, int maxHP)
    {
        
    }
}
