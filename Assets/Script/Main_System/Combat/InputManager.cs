using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager : MonoBehaviour
{
    
    public void AttackAreaInput(string name)
    {
        switch (name)
        {
            case "Head":
                break;
            case "Body":
                break;
            case "Leg":
                break;
            case "Arm":
                break;
            default:
                Debug.Log("병신아 뭘 누른거야");
                break;
        }
    }


}
