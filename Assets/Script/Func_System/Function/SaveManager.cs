using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    public bool TryLoadGame(out SaveGameData data, out string error)
    {
        data = null;
        error = null;

        if (!File.Exists(savePath))
        {
            error = "저장 파일이 없습니다.";
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(savePath));
            if (data == null || data.version <= 0)
            {
                error = "지원하지 않는 이전 저장 형식입니다. 새 게임을 시작한 뒤 다시 저장해 주세요.";
                return false;
            }

            if (data.version != SaveGameData.CurrentVersion)
            {
                error = $"지원하지 않는 저장 버전입니다. (파일: {data.version}, 지원: {SaveGameData.CurrentVersion})";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = $"저장 파일을 읽지 못했습니다: {exception.Message}";
            return false;
        }
    }

    public bool TryRestoreGame(out PlayerData playerData, out ItemData itemData, out List<CurrencyData> currencies,
        out int goodAndEvil, out Node currentNode, out EquipmentData equipmentData, out string error)
    {
        playerData = null;
        itemData = null;
        currencies = null;
        goodAndEvil = 0;
        currentNode = null;
        equipmentData = null;

        if (!TryLoadGame(out SaveGameData data, out error))
        {
            return false;
        }

        currentNode = LoadNode(data.currentNodeId);
        if (currentNode == null)
        {
            error = $"저장된 노드를 찾을 수 없습니다: {data.currentNodeId}";
            return false;
        }

        playerData = data.player != null ? data.player.ToPlayerData() : new PlayerData();
        itemData = new ItemData { inventoryItems = LoadItems(data.inventoryItemIds) };
        currencies = LoadCurrencies(data.currencies);
        goodAndEvil = data.goodAndEvil;
        equipmentData = new EquipmentData
        {
            weaponItem = LoadItem<WeaponItem>(data.weaponItemId),
            armorItem = LoadItem<ArmorItem>(data.armorItemId),
            accessoryItem = LoadItem<AccessoryItem>(data.accessoryItemId)
        };
        playerData.equipmentData = equipmentData;
        return true;
    }

    public void SaveGame()
    {
        Player player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        Node node = NodeManager.Instance != null ? NodeManager.Instance.currentNode : null;
        if (player == null || node == null)
        {
            Debug.LogWarning("[SaveManager] 플레이어 또는 현재 노드가 준비되지 않아 저장하지 않았습니다.");
            return;
        }

        SaveGameData data = CreateSnapshot(player, node);
        WriteAtomically(JsonUtility.ToJson(data, true));
    }

    private SaveGameData CreateSnapshot(Player player, Node node)
    {
        SaveGameData data = new SaveGameData
        {
            currentNodeId = node.name,
            goodAndEvil = GameManager.Instance != null ? GameManager.Instance.goodAndEvil : 0,
            player = PlayerSaveData.FromPlayerData(player.GetCurrentData()),
            weaponItemId = GetItemId(player.equipmentData != null ? player.equipmentData.weaponItem : null),
            armorItemId = GetItemId(player.equipmentData != null ? player.equipmentData.armorItem : null),
            accessoryItemId = GetItemId(player.equipmentData != null ? player.equipmentData.accessoryItem : null)
        };

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.currencyList != null)
        {
            foreach (CurrencyData currency in CurrencyManager.Instance.currencyList)
            {
                if (currency != null)
                {
                    data.currencies.Add(new CurrencySaveData(currency.Name, currency.Amount));
                }
            }
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryItems != null)
        {
            foreach (BaseItem item in InventoryManager.Instance.inventoryItems)
            {
                string itemId = GetItemId(item);
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    data.inventoryItemIds.Add(itemId);
                }
            }
        }

        return data;
    }

    private void WriteAtomically(string json)
    {
        string temporaryPath = savePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(savePath))
            {
                File.Replace(temporaryPath, savePath, null);
            }
            else
            {
                File.Move(temporaryPath, savePath);
            }

            Debug.Log($"[SaveManager] 저장 완료: {savePath}");
        }
        catch (Exception exception)
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            Debug.LogError($"[SaveManager] 저장 실패: {exception.Message}");
        }
    }

    private static string GetItemId(BaseItem item)
    {
        return item != null ? item.itemID : null;
    }

    private static Node LoadNode(string nodeId)
    {
        return string.IsNullOrWhiteSpace(nodeId) ? null : Resources.Load<Node>($"Nodes/{nodeId}");
    }

    private static T LoadItem<T>(string itemId) where T : BaseItem
    {
        return string.IsNullOrWhiteSpace(itemId) ? null : Resources.Load<T>($"Items/{itemId}");
    }

    private static List<BaseItem> LoadItems(List<string> itemIds)
    {
        List<BaseItem> items = new List<BaseItem>();
        if (itemIds == null)
        {
            return items;
        }

        foreach (string itemId in itemIds)
        {
            BaseItem item = Resources.Load<BaseItem>($"Items/{itemId}");
            if (item == null)
            {
                Debug.LogWarning($"[SaveManager] 인벤토리 아이템을 찾지 못했습니다: {itemId}");
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    private static List<CurrencyData> LoadCurrencies(List<CurrencySaveData> savedCurrencies)
    {
        List<CurrencyData> currencies = new List<CurrencyData>();
        if (savedCurrencies == null)
        {
            return currencies;
        }

        foreach (CurrencySaveData savedCurrency in savedCurrencies)
        {
            if (savedCurrency != null && !string.IsNullOrWhiteSpace(savedCurrency.name))
            {
                currencies.Add(new CurrencyData(savedCurrency.name, savedCurrency.amount));
            }
        }

        return currencies;
    }
}
