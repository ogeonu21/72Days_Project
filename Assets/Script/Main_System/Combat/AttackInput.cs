using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackInput : MonoBehaviour
{
    public void AttackAreaInput(string name)
    {
        switch (AreaDataDB.GetArea(name, out var area).label)
        {
            case "¸Ó¸®":
                break;
            case "¸ö":
                break;
            default:
                break;
        }
    }


}
