using System;
using UnityEngine;

// Currency 데이터 구조

[System.Serializable]
public class CurrencyData
{
    [SerializeField]
    private string name;

    [SerializeField]
    private int amount;

    public string Name => name;
    public int Amount => amount;

    public CurrencyData(string name, int initialAmount = 0)
    {
        this.name = name;
        SetAmount(initialAmount);
    }

    public void SetAmount(int amount)
    {
        this.amount = Mathf.Max(0, amount);
        CurrencyEvent.CurrencyChanged(this);
    }
}
