using System;
using UnityEngine;

public static class JsonHelper
{
    // JSON 배열 문자열을 받아 실제 클래스 배열로 변환해주는 함수
    public static T[] FromJson<T>(string json)
    {
        // JSON 배열을 유니티가 인식할 수 있는 "items" 객체로 감쌈
        string newJson = "{ \"items\": " + json + " }";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}