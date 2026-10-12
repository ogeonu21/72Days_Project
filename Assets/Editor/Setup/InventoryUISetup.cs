using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>기존 인벤토리 프리팹과 GameWindow에 실제 편집 가능한 UI를 연결하는 1회 설치 도구.</summary>
public static class InventoryUISetup
{
    private const string ScenePath = "Assets/Scenes/GameWindow.unity";
    private const string SlotPath = "Assets/Prefabs/Inventory_Slot_UI.prefab";
    private static TMP_FontAsset font;
    private static readonly Color Ink = new Color(.06f, .075f, .1f, 1);
    private static readonly Color Gold = new Color(.88f, .73f, .42f, 1);

    [MenuItem("Tools/Inventory/Install Inventory UI")]
    public static void InstallMenu() => Debug.Log(Install());

    [MenuItem("Tools/Inventory/Install Player Stats Details")]
    public static void InstallStatsMenu() => Debug.Log(InstallStatsDetails());

    public static string InstallStatsDetails()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 설치하세요.");
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("GameWindow의 미저장 변경을 먼저 저장하세요.");
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InventoryUI>(true)).Single();
            if (ui.playerStatsDetails != null) return "상세 능력치 UI가 이미 연결되어 있습니다.";
            var status = ui.transform.Find("Status_UIGroup");
            if (status == null) throw new InvalidOperationException("Status_UIGroup을 찾지 못했습니다.");
            EnsureFont();
            Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "상세 능력치 UI 연결");
            var graphic = status.GetComponent<Image>() ?? Undo.AddComponent<Image>(status.gameObject);
            graphic.color = new Color(.14f, .18f, .22f, 1);
            graphic.raycastTarget = true;
            var open = status.GetComponent<Button>() ?? Undo.AddComponent<Button>(status.gameObject);
            open.targetGraphic = graphic;
            UnityEventTools.AddPersistentListener(open.onClick, ui.OpenPlayerStats);
            Text(status, "DetailsHint", "눌러서 상세 능력치 보기", .1f, -.24f, .9f, 0, 22, Gold).alignment = TextAlignmentOptions.Center;
            var overlay = Image(ui.transform, "PlayerStatsDetails", new Color(0, 0, 0, .86f), 0, 0, 1, 1);
            overlay.gameObject.SetActive(false);
            var view = Undo.AddComponent<PlayerStatsDetailsUI>(overlay.gameObject);
            ui.playerStatsDetails = view;
            var card = Image(overlay.transform, "Card", Ink, .045f, .07f, .955f, .94f).transform;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            Text(card, "Title", "상세 능력치", .06f, .90f, .72f, .97f, 46, Gold);
            Button(card, "Close", "닫기", .78f, .91f, .95f, .97f, view.Close);
            view.healthText = Text(card, "HealthValue", "체력", .07f, .84f, .93f, .89f, 34, Color.white);
            var track = Image(card, "HealthBar", new Color(.18f, .22f, .25f), .07f, .79f, .93f, .825f);
            var fill = Image(track.transform, "Fill", new Color(.38f, .72f, .48f), 0, 0, 1, 1);
            view.healthBar = track.gameObject.AddComponent<Slider>();
            view.healthBar.fillRect = fill.rectTransform;
            view.healthBar.minValue = 0; view.healthBar.maxValue = 1;
            view.healthBar.interactable = false;
            track.raycastTarget = fill.raycastTarget = false;
            view.attributesText = Text(card, "Attributes", "STR / DEX / CON", .07f, .70f, .93f, .76f, 34, Gold);
            view.attackText = Text(card, "Attack", "공격력", .07f, .54f, .93f, .68f, 32, Color.white);
            view.dodgeText = Text(card, "Dodge", "회피율", .07f, .38f, .93f, .52f, 32, Color.white);
            view.accuracyText = Text(card, "Accuracy", "명중 보정", .07f, .20f, .93f, .36f, 30, Color.white);
            view.rangeText = Text(card, "Range", "사거리", .07f, .08f, .93f, .18f, 32, Color.white);
            Text(card, "Note", "기타 보정에는 버프·디버프 등이 포함됩니다.", .07f, .025f, .93f, .07f, 24, Gold);
            foreach (var label in card.GetComponentsInChildren<TMP_Text>(true))
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 20;
                label.fontSizeMax = label.fontSize;
            }
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Status_UIGroup 클릭 및 상세 능력치 UI 연결 완료";
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    public static string Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 설치하세요.");
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (!opened && scene.isDirty) throw new InvalidOperationException("GameWindow의 미저장 변경을 먼저 저장하세요.");
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            var canvas = roots.First(r => r.name == "UICanvas");
            var panel = canvas.transform.Find("ExtraUIGroup/InventoryUI/Inventory_Panel");
            if (panel.GetComponent<InventoryUI>() != null) return "InventoryUI가 이미 설치되어 있습니다. 기존 연결을 유지합니다.";
            EnsureFont();
            ConfigureIcons();
            ConfigureSlotPrefab();
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "인벤토리 UI 연결");
            var ui = Undo.AddComponent<InventoryUI>(panel.gameObject);
            ui.inventory = roots.SelectMany(r => r.GetComponentsInChildren<InventoryManager>(true)).Single();
            ui.characters = null; // 시작 씬에서 DontDestroyOnLoad로 유지되는 전역 매니저
            ui.uiManager = roots.SelectMany(r => r.GetComponentsInChildren<UIManager>(true)).Single();
            Rect(panel, 0, .06f, 1, 1, 16);
            var background = panel.GetComponent<Image>();
            background.color = Ink;
            var close = panel.Find("Inventory_Quit_Button");
            Rect(close, .86f, .91f, .97f, .98f, 0);
            Text(panel, "InventoryTitle", "배낭", .06f, .91f, .6f, .98f, 58, Gold);
            Text(panel, "EquipmentTitle", "장착 장비", .06f, .86f, .8f, .90f, 32, Gold);
            var equipment = panel.Find("Equipment_UIGroup");
            Rect(equipment, .06f, .62f, .94f, .85f, 0);
            string[] equipmentPaths = { "Helmet_Armor_UI_Icon", "Chestplate_Armor_UI_Icon", "Boots_Armor_UI_Icon", "Gloves_Armor_UI_Icon", "Weapon_UI_Icon", "Accesory_UI_Icon" };
            ui.equipmentSlots = new InventorySlotUI[6];
            for (int i = 0; i < equipmentPaths.Length; i++)
            {
                var group = equipment.Find(equipmentPaths[i]);
                Rect(group, (i % 3) / 3f, 1 - (i / 3 + 1) / 2f, (i % 3 + 1) / 3f, 1 - (i / 3) / 2f, 10);
                var slot = group.GetComponentInChildren<InventorySlotUI>(true);
                if (slot == null) throw new InvalidOperationException("장비 슬롯 프리팹 연결 누락: " + group.name);
                Rect(slot.transform, 0, 0, 1, 1, 0);
                ui.equipmentSlots[i] = slot;
            }
            Rect(panel.Find("Status_UIGroup"), .08f, .52f, .92f, .61f, 0);
            ui.capacityText = Text(panel, "Capacity", "소지품  0 / 20", .06f, .47f, .94f, .52f, 34, Gold);
            var grid = panel.Find("Inventory_UIGroup");
            Rect(grid, .06f, .055f, .94f, .465f, 0);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.cellSize = new Vector2(164, 164);
            layout.spacing = new Vector2(14, 14);
            layout.childAlignment = TextAnchor.MiddleCenter;
            var gridSlots = grid.GetComponentsInChildren<InventorySlotUI>(true);
            if (gridSlots.Length < 20) throw new InvalidOperationException("인벤토리 슬롯이 20개보다 적습니다.");
            ui.slots = gridSlots.Take(20).ToArray();
            for (int i = 0; i < gridSlots.Length; i++) gridSlots[i].gameObject.SetActive(i < 20);
            ui.emptyText = Text(panel, "EmptyHint", "아이템을 획득하면 이곳에 보관됩니다.", .06f, .005f, .94f, .045f, 26, new Color(.65f, .7f, .76f));
            ui.emptyText.alignment = TextAlignmentOptions.Center;
            BuildDetails(ui, panel);
            panel.gameObject.SetActive(false);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssetIfDirty(font);
            return "인벤토리 UI 설치 완료: 20슬롯, 6장비칸, 상세/사용/장착/해제/수량별 버리기 및 Addressables 연결";
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    private static void EnsureFont()
    {
        const string path = "Assets/TextMesh Pro/Fonts/Inventory Dynamic SDF.asset";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/DungGeunMo.ttf");
        font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Inventory Dynamic SDF";
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(font, path);
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        AssetDatabase.SaveAssetIfDirty(font);
    }

    private static void ConfigureIcons()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) throw new InvalidOperationException("Addressables 설정이 없습니다.");
        var group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>("Assets/AddressableAssetsData/AssetGroups/Item_Icon.asset");
        if (group == null) throw new InvalidOperationException("사용자가 만든 Item_Icon 그룹을 찾지 못했습니다.");
        string potionPath = "Assets/Resources/Items/Potion_01.asset";
        if (AssetDatabase.LoadAssetAtPath<BaseItem>(potionPath) == null)
        {
            var potion = ScriptableObject.CreateInstance<PotionItem>();
            potion.itemID = "Potion_01";
            potion.itemName = "회복 물약";
            potion.itemDescription = "지친 몸을 회복시키는 물약입니다. 사용하면 체력을 15 회복합니다.";
            potion.itemCategory = ItemCategory.Potion;
            potion.isConsumable = true;
            potion.health = 15;
            potion.itemValue = 50;
            AssetDatabase.CreateAsset(potion, potionPath);
            Undo.RegisterCreatedObjectUndo(potion, "회복 물약 예제 생성");
        }
        foreach (string guid in AssetDatabase.FindAssets("t:BaseItem", new[] { "Assets/Resources/Items" }))
        {
            var item = AssetDatabase.LoadAssetAtPath<BaseItem>(AssetDatabase.GUIDToAssetPath(guid));
            if (!string.IsNullOrWhiteSpace(item.itemIcon)) continue;
            string file = item is ArmorItem armor ? ArmorIcon(armor.armorType) :
                item is PotionItem ? "22_Potion.png" : item is AccessoryItem ? "16_Gold_ring.png" : "38_sword.png";
            string path = "Assets/ClassicPixelRPGUI/Artwok/Icons/" + file;
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) throw new InvalidOperationException("Sprite 없음: " + path);
            string iconGuid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(iconGuid);
            if (entry == null)
            {
                Undo.RecordObject(group, "아이콘 Addressable 등록");
                entry = settings.CreateOrMoveEntry(iconGuid, group);
                entry.address = "ItemIcons/" + System.IO.Path.GetFileNameWithoutExtension(file);
            }
            Undo.RecordObject(item, "아이콘 주소 연결");
            item.itemIcon = entry.address;
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssetIfDirty(item);
        }
        AssetDatabase.SaveAssetIfDirty(group);
        AssetDatabase.SaveAssetIfDirty(settings);
    }

    private static string ArmorIcon(ArmorType type)
    {
        switch (type) { case ArmorType.Helmet: return "04_helm.png"; case ArmorType.Chestplate: return "05_chest.png"; case ArmorType.Boots: return "07_boots.png"; default: return "35_glowes.png"; }
    }

    private static void ConfigureSlotPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(SlotPath);
        try
        {
            if (root.GetComponent<InventorySlotUI>()?.icon != null) return;
            var slot = root.GetComponent<InventorySlotUI>() ?? root.AddComponent<InventorySlotUI>();
            slot.button = root.GetComponent<Button>() ?? root.AddComponent<Button>();
            slot.button.targetGraphic = root.GetComponent<Image>();
            // 기존 수량 텍스트는 숨기고 프리팹의 시각 구조는 보존한다.
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            slot.selection = Image(root.transform, "Selection", new Color(1, .82f, .35f, .22f), 0, 0, 1, 1);
            slot.selection.raycastTarget = false;
            slot.selection.enabled = false;
            var iconImage = Image(root.transform, "ItemIcon", Color.white, .2f, .22f, .8f, .82f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            slot.icon = iconImage.gameObject.AddComponent<AddressableItemIcon>();
            slot.icon.image = iconImage;
            slot.icon.placeholder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ClassicPixelRPGUI/Artwok/UI/Icon_inventory_bag.png");
            slot.nameText = Text(root.transform, "ItemName", "", .04f, .02f, .96f, .23f, 24, Color.white);
            slot.nameText.alignment = TextAlignmentOptions.Center;
            slot.nameText.overflowMode = TextOverflowModes.Ellipsis;
            slot.nameText.enableWordWrapping = false;
            slot.quantityText = Text(root.transform, "StackQuantity", "", .50f, .76f, .95f, .98f, 30, Color.white);
            slot.quantityText.alignment = TextAlignmentOptions.TopRight;
            slot.equippedText = Text(root.transform, "Equipped", "", .05f, .77f, .55f, .98f, 22, Gold);
            PrefabUtility.SaveAsPrefabAsset(root, SlotPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildDetails(InventoryUI ui, Transform panel)
    {
        var overlay = Image(panel, "ItemDetails", new Color(0, 0, 0, .8f), 0, 0, 1, 1).transform;
        ui.detailPanel = overlay.gameObject;
        var card = Image(overlay, "Card", Ink, .055f, .13f, .945f, .89f).transform;
        var border = card.gameObject.AddComponent<Outline>(); border.effectColor = Gold; border.effectDistance = new Vector2(3, -3);
        ui.itemNameText = Text(card, "ItemName", "아이템 정보", .08f, .87f, .82f, .96f, 48, Gold);
        Button(card, "Close", "닫기", .81f, .9f, .97f, .98f, ui.CloseDetails);
        var icon = Image(card, "Icon", Color.white, .36f, .67f, .64f, .86f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        ui.detailIcon = icon.gameObject.AddComponent<AddressableItemIcon>();
        ui.detailIcon.image = icon;
        ui.detailIcon.placeholder = ui.slots[0].icon.placeholder;
        var scrollObject = RectObject(card, "InfoScroll", .07f, .24f, .93f, .65f);
        var scroll = scrollObject.gameObject.AddComponent<ScrollRect>();
        var viewportImage = Image(scrollObject, "Viewport", Color.white, 0, 0, 1, 1);
        var mask = viewportImage.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var content = RectObject(viewportImage.transform, "Content", 0, 1, 1, 1);
        content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 28; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ui.descriptionText = Text(content, "Description", "", 0, 0, 1, 1, 34, Color.white);
        ui.statsText = Text(content, "Stats", "", 0, 0, 1, 1, 32, new Color(.7f, .8f, .85f));
        scroll.viewport = viewportImage.rectTransform; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        ui.feedbackText = Text(card, "Feedback", "", .07f, .16f, .93f, .23f, 27, new Color(1, .62f, .43f));
        ui.actionButton = Button(card, "Action", "장착", .07f, .04f, .48f, .14f, ui.ActivateSelected);
        ui.actionLabel = ui.actionButton.GetComponentInChildren<TMP_Text>();
        ui.discardButton = Button(card, "Discard", "버리기", .52f, .04f, .93f, .14f, ui.BeginDiscard);
        var confirm = Image(overlay, "DiscardConfirmation", new Color(0, 0, 0, .92f), 0, 0, 1, 1).transform;
        ui.confirmPanel = confirm.gameObject;
        var confirmCard = Image(confirm, "Card", Ink, .08f, .29f, .92f, .74f).transform;
        var outline = confirmCard.gameObject.AddComponent<Outline>(); outline.effectColor = Gold;
        ui.confirmText = Text(confirmCard, "Question", "버리시겠습니까?", .08f, .62f, .92f, .92f, 36, Color.white);
        ui.confirmText.alignment = TextAlignmentOptions.Center;
        ui.discardQuantityText = Text(confirmCard, "Quantity", "1개", .3f, .39f, .7f, .55f, 38, Gold);
        ui.discardQuantityText.alignment = TextAlignmentOptions.Center;
        ui.minusButton = Button(confirmCard, "Minus", "-", .1f, .4f, .26f, .55f, ui.DecreaseDiscard);
        ui.plusButton = Button(confirmCard, "Plus", "+", .74f, .4f, .90f, .55f, ui.IncreaseDiscard);
        ui.allButton = Button(confirmCard, "All", "전체 수량", .31f, .24f, .69f, .37f, ui.DiscardAll);
        Button(confirmCard, "Cancel", "취소", .08f, .06f, .47f, .21f, ui.CancelDiscard);
        ui.confirmButton = Button(confirmCard, "Confirm", "버리기 확인", .53f, .06f, .92f, .21f, ui.ConfirmDiscard);
        ui.confirmPanel.SetActive(false);
        ui.detailPanel.SetActive(false);
    }

    private static RectTransform RectObject(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        Undo.RegisterCreatedObjectUndo(go, "인벤토리 UI 생성");
        go.transform.SetParent(parent, false);
        Rect(go.transform, x0, y0, x1, y1, 0);
        return (RectTransform)go.transform;
    }
    private static void Rect(Transform transform, float x0, float y0, float x1, float y1, float inset)
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        rect.localScale = Vector3.one;
    }
    private static Image Image(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
    {
        var image = RectObject(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }
    private static TMP_Text Text(Transform parent, string name, string value, float x0, float y0, float x1, float y1, float size, Color color)
    {
        var text = RectObject(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.text = value; text.color = color;
        text.raycastTarget = false; text.enableWordWrapping = true;
        return text;
    }
    private static Button Button(Transform parent, string name, string label, float x0, float y0, float x1, float y1, UnityAction action)
    {
        var image = Image(parent, name, new Color(.21f, .26f, .3f), x0, y0, x1, y1);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1, .9f, .7f); colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.4f, .4f, .4f); button.colors = colors;
        var text = Text(image.transform, "Label", label, .04f, .03f, .96f, .97f, 32, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
}
