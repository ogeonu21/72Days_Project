using System;
using System.Collections;

public static class CurrencyEvent
{
    //재화 변경 이벤트
    public static event Action<CurrencyData> OnCurrencyChanged;
    public static void CurrencyChanged(CurrencyData data) => OnCurrencyChanged?.Invoke(data);
}