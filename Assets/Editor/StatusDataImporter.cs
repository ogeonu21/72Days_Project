using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

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

        for (int i = 2; i < lines.Length; i++) // 첫 줄은 헤더
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 7)
            {
                Debug.LogWarning($"라인 무시됨 (데이터 부족): {line}");
                continue;
            }

            string type = parts[0];
            string name = parts[1];

            // 안전하게 파싱
            float.TryParse(parts[2], out float hp);
            float.TryParse(parts[3], out float maxHP);
            float.TryParse(parts[4], out float attack);
            float.TryParse(parts[5], out float avoid);
            int.TryParse(parts[6], out int attackDistance);

            Status status = new Status(hp, maxHP, attack, avoid, attackDistance);



            if (type == "player")
            {
                PlayerStatusData data = ScriptableObject.CreateInstance<PlayerStatusData>();
                data.name = name;  // 이름 지정
                data.playerBaseName = name;
                data.baseStatus = status;
                SaveAsset(data, $"Assets/Resources/PlayerStatus/{name}.asset");
            }
            else if (type == "npc")
            {
                NPCStatusData data = ScriptableObject.CreateInstance<NPCStatusData>();
                data.name = name;
                data.npcName = name;
                data.baseStatus = status;
                SaveAsset(data, $"Assets/Resources/NPCStatus/{name}.asset");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV 데이터 import 완료!");
    }

    private void SaveAsset(ScriptableObject obj, string path)
    {
        string folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (existingAsset != null)
        {
            // 기존 자산 덮어쓰기
            EditorUtility.CopySerialized(obj, existingAsset);
            EditorUtility.SetDirty(existingAsset);
        }
        else
        {
            // 새 자산 생성
            AssetDatabase.CreateAsset(obj, path);
        }
    }

}