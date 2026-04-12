using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class SaveManager : SingleTon<SaveManager>
{
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        savePath = Path.Combine(Application.persistentDataPath, "savedata.json");
    }

    protected void OnEnable()
    {
        GameEvent.OnSaveGame += SaveGame;
    }

    protected void OnDisable()
    {
        GameEvent.OnSaveGame -= SaveGame;
    }

    public void SaveData(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string encodedJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        try
        {
            File.WriteAllText(savePath, encodedJson);
            Debug.Log($"<color=orange>[SaveManager]</color>게임 데이터 저장 성공 : {savePath}");
        }
        catch(System.Exception e)
        {
            Debug.LogError($"<color=orange>[SaveManager]</color>게임 데이터 저장 실패 : {e.Message}");
        }
    }

    public SaveData LoadData()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"<color=orange>[SaveManager]</color>저장된 파일이 없습니다. 새로운 SaveData를 생성합니다.");
            return new SaveData();
        }

        try
        {
            string encodedJson = File.ReadAllText(savePath);
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJson));

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"<color=orange>[SaveManager]</color>게임 데이터를 불러왔습니다 : {savePath}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=orange>[SaveManager]</color>게임 데이터 불러오기 실패하였습니다. 새로운 SaveData를 생성합니다. : {e.Message}");
            return new SaveData();
        }

    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.playerData = CharacterManager.Instance.currentPlayer.GetCurrentData();
        data.currentNode = NodeManager.Instance.currentNode;
        data.goodAndEvil = GameManager.Instance.goodAndEvil;
        data.currencyList = CurrencyManager.Instance.currencyList;
        data.itemData.inventoryItems = InventoryManager.Instance.inventoryItems;

        SaveManager.Instance.SaveData(data);
    }
}

[System.Serializable]
public class SaveData
{
    public PlayerData playerData; //플레이어 데이터
    public Node currentNode; //현재 진행중인 Node;
    public int goodAndEvil; //선악 수치
    public ItemData itemData;
    public List<CurrencyData> currencyList;
    // item Data
    // 진행도 관련한 스택. 선    행, 악행 등의 스택.

    public SaveData()
    {
        playerData = new PlayerData();
        currentNode = ScriptableObject.CreateInstance<Node>();
        goodAndEvil = 0;
        itemData = new ItemData();
        currencyList = new List<CurrencyData>();
    }
}