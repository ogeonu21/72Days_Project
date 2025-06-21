using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonController : MonoBehaviour
{
    [SerializeField]
    public TMP_Text[] avdButton = new TMP_Text[6];
    public Enemy enemy;


    // Update is called once per frame
    void FixedUpdate()
    {
        avdButton[0].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[2] - enemy.avd).ToString() + "%";
        avdButton[1].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[2] - enemy.avd).ToString() + "%";
        avdButton[2].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[3] - enemy.avd).ToString() + "%";
        avdButton[3].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[3] - enemy.avd).ToString() + "%";
        avdButton[4].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[1] - enemy.avd).ToString() + "%";
        avdButton[5].text = "¸íÁß·ü : " + (BattleManager.Instance.hitRatioArray[0] - enemy.avd).ToString() + "%";
    }
}
