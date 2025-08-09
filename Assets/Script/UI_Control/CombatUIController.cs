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

    
    void Start()
    {
        //StoryManager에서 Combat의 시작을 받아서 UI를 업데이트함
        storyManager = StoryManager.Instance;
        storyManager.OnCombatNodeStart += UpdateCombatUI;
        //combatManager 초기화
    }

    public void UpdateCombatUI(Choice node)
    {
        //모든 CombatUI활성화
        

    }

    
}
