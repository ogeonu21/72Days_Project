using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

public class StatusDataImporter : EditorWindow
{
    private TextAsset csvFile;

    [MenuItem("Tools/Import Status CSV")]
    public static void ShowWindow()
    {
        GetWindow<StatusDataImporter>("CSV Status Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV 파일을 선택하세요", EditorStyles.boldLabel);
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);

        if (GUILayout.Button("Import and Generate ScriptableObjects"))
        {
            if (csvFile != null)
                ImportCSV(csvFile.text);
            else
                Debug.LogWarning("CSV 파일이 없습니다.");
        }
    }

    private void ImportCSV(string csv)
    {
        string[] lines = csv.Split('\n');

        for (int i = 3; i < lines.Length; i++) // 첫 줄과 둘째 줄은 제외
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 10)
            {
                Debug.LogWarning($"라인 무시됨 (데이터 부족): {line}");
                continue;
            }

            #region [Data Parsing]
            string type = parts[0].Trim();
            string id = parts[1].Trim();
            string name = parts[2].Trim();

            // 안전하게 파싱
            int.TryParse(parts[3], out int str);
            int.TryParse(parts[4], out int dex);
            int.TryParse(parts[5], out int con);
            int.TryParse(parts[6], out int attackBonus);
            int.TryParse(parts[7], out int hpBonus);
            float.TryParse(parts[8], out float dodgeBonus);
            int.TryParse(parts[9], out int rangeBonus);

            #endregion

            if (type != "npc") continue;

            #region [Update SO]
            string path = $"Assets/Resources/NPCStats/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            bool created = false;
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                created = true;
            }

            def.id = id;
            def.displayName = string.IsNullOrEmpty(name) ? id : name;
            def.baseStats = new BaseStats { str = str, dex = dex, con = con };
            def.attackBonus = attackBonus;
            def.hpBonus = hpBonus;
            def.dodgeBonus = Mathf.Clamp01(dodgeBonus); // 0~1 범위 권장
            def.rangeBonus = rangeBonus;
            #endregion

            if (created) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV 데이터 import 완료.");
    }

}