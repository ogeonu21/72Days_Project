using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class PlayerTendencyValidation
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void ImportIcons()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/UI/Tendency" }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 72;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    [MenuItem("Tools/Validation/Player Tendency")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int count = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); count++; };
        var root = new GameObject("TendencyFixture") { hideFlags = HideFlags.HideAndDontSave };
        var callbacks = new Dictionary<FieldInfo, object>();
        foreach (var type in new[] { typeof(GameEvent), typeof(PlayerEvent) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                { callbacks[field] = field.GetValue(null); field.SetValue(null, null); }
        try
        {
            int[] values = { int.MinValue, -8, -7, -4, -3, 0, 3, 4, 7, 8, int.MaxValue };
            int[] indices = { 0, 0, 1, 1, 2, 2, 2, 3, 3, 4, 4 };
            for (int i = 0; i < values.Length; i++)
                check(TendencyRules.GetIconIndex(values[i]) == indices[i], "단계 경계 " + values[i]);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.sizeDelta = new Vector2(220, 80);
            var view = PlayerTendencyUI.Ensure(label);
            var player = root.AddComponent<Player>();
            view.Bind(player);
            var icon = view.GetComponentInChildren<UnityEngine.UI.Image>();
            string[] names = { "realist-strong", "realist", "neutral", "idealist", "idealist-strong" };
            int[] tiers = { -8, -4, 0, 4, 8 };
            for (int i = 0; i < tiers.Length; i++)
            {
                player.ChangeTendency(tiers[i] - player.tendency);
                check(icon.sprite != null && icon.sprite.name == names[i], "이벤트로 아이콘 갱신 " + names[i]);
                check(label.text == TendencyRules.GetLabel(tiers[i]), "한글 성향 이름 " + names[i]);
                check(icon.sprite.rect.size == new Vector2(72,72) && icon.sprite.texture.width == 72 && icon.sprite.texture.height == 72, "72×72 " + names[i]);
                check(icon.sprite.texture.filterMode == FilterMode.Point && icon.sprite.texture.mipmapCount == 1, "픽셀 필터 " + names[i]);
            }
            check(icon.rectTransform.sizeDelta == new Vector2(72,72) && !icon.raycastTarget && !label.raycastTarget, "크기·입력 방해 없음");
            check(PlayerTendencyUI.Ensure(label) == view && label.transform.childCount == 1, "중복 생성 방지");
            player.ResetTendency();
            check(icon.sprite.name == "neutral" && label.text == "중립", "리셋 반영");
            player.LoadFromData(new PlayerData { tendency = -4 });
            check(icon.sprite.name == "realist", "기존 플레이어에 저장 데이터 로드 반영");
            check(player.GetCurrentData().tendency == -4, "저장 수치 유지");
            var secondObject = new GameObject("SecondPlayer");
            secondObject.transform.SetParent(root.transform);
            var second = secondObject.AddComponent<Player>();
            second.ChangeTendency(8);
            view.Bind(second);
            player.ChangeTendency(100);
            check(icon.sprite.name == "idealist-strong", "교체 시 이전 플레이어 구독 해제");
            typeof(PlayerTendencyUI).GetMethod("OnDisable", PrivateInstance).Invoke(view, null);
            second.ResetTendency();
            check(icon.sprite.name == "idealist-strong", "숨김 시 구독 해제");
            typeof(PlayerTendencyUI).GetMethod("OnEnable", PrivateInstance).Invoke(view, null);
            check(icon.sprite.name == "neutral", "재표시 동기화");
            view.Bind(null);
            second.ChangeTendency(-8);
            check(icon.sprite.name == "neutral", "연결 해제 시 이전 수치 제거");
            return $"Player tendency: {count} checks passed (Edit Mode, 실제 저장 파일 변경 없음)";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var callback in callbacks) callback.Key.SetValue(null, callback.Value);
        }
    }
}
