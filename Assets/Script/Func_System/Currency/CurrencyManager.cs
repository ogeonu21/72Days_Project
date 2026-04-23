using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 게임 내 재화 관리 매니저 클래스

public class CurrencyManager : SingleTon<CurrencyManager>, ICurrency
{
    public List<CurrencyData> currencyList = new List<CurrencyData>();

    public void InitializeManager()
    {
        currencyList.Clear();
        InitializeCurrencies();
    }

    private void InitializeCurrencies()
    {
        //초기화를 위해 currencyData 리스트 추가.
        if (currencyList.Count == 0)
        {
            CurrencyData gold = new CurrencyData("Gold", 0);
            currencyList.Add(gold);
        }
    }

    public CurrencyData GetCurrencyData(string currencyName)
    {
        return currencyList.FirstOrDefault(c => c.Name.Equals(currencyName, System.StringComparison.OrdinalIgnoreCase));
    }

    public int GetAmount(string currencyName)
    {
        CurrencyData data = GetCurrencyData(currencyName);
        return data != null ? data.Amount : 99999;
    }

    public void SetAmount(string currencyName, int newAmount)
    {
        CurrencyData data = GetCurrencyData(currencyName);
        if (data != null)
        {
            data.SetAmount(newAmount);
            Debug.Log($"재화 {currencyName}의 잔액이 {newAmount}로 설정되었다.");
        }
        CurrencyEvent.CurrencyChanged(GetCurrencyData(currencyName));
    }

    public void Increase(string currencyName, int amount)
    {
        if (amount <= 0) return;
        CurrencyData data = GetCurrencyData(currencyName);
        if (data != null)
        {
            data.SetAmount(data.Amount + amount);
            Debug.Log($"재화 {currencyName}의 잔액이 {data.Amount}로 설정되었다.");
        }
    }

    public bool Decrease(string currencyName, int amount)
    {
        if (amount <= 0) return false;
        CurrencyData data = GetCurrencyData(currencyName);
        if (data == null) return false;

        if (data.Amount >= amount)
        {
            data.SetAmount(data.Amount - amount);
            Debug.Log($"재화 {currencyName}의 잔액이 {data.Amount}로 설정되었다.");
            return true;
        }
        else
        {
            Debug.Log("잔액 부족. 결제 실패.");
            return false;
        }
    }
}
