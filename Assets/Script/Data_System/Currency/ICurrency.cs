using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICurrency
{
    string name { get; } //재화 이름 (읽기 전용)

    int GetAmount(string currencyName);

    void SetAmount(string currencyName, int newAmount);

    void Increase(string currencyName, int amount);

    //차감이 성공했는지 return해야함.
    bool Decrease(string currencyName, int amount);
}
