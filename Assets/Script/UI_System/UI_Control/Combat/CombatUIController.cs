using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : MonoBehaviour
{
    #region [변수 그룹]
    public TMP_Text combatText;
    public GameObject attackButtons;
    public Image blood_Effect;

    private CombatManager combatManager;

    #endregion
    //private CombatManager combatManager;

    private void Awake()
    {
        combatManager = CombatManager.Instance;
        combatManager.CombatUIUpdate += UpdateCombatUI;
        combatManager.onTextUpdate += UpdateCombatText;
    }
    
    void Start()
    {
        //StoryManager에서 Combat의 시작을 받아서 UI를 업데이트함
        //CombatManger에서 CombatTextChanged라는 이벤트를 구독, UpdateCombatUI를 실행해야함.

        //이거는 아무래도 UI쪽에서 건드려야겠다.
        
    }

    public void UpdateCombatUI(Character target)
    {
       

    }

    IEnumerator UpdateCombatText(string text)
    {
        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(combatText, text, 0.05f));
    }

}
