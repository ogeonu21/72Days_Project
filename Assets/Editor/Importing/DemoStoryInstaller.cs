using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>명시적으로 승인된 데모 교체 전용. 일반 시트 가져오기의 보존 정책은 유지한다.</summary>
public static class DemoStoryInstaller
{
    public static void ApplyFromDirectory(string directory)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Edit Mode에서만 데모를 교체할 수 있습니다.");
        var names = new[] { "ItemData", "StatusData", "NodeData", "EventData", "EventChoiceData", "RewardData" };
        var json = names.Select(n => File.ReadAllText(Path.Combine(directory, n + ".json"))).ToArray();
        DataImporter.ValidateSnapshot(json[0], json[1], json[2], json[3], json[4], json[5]);
        var nodes = SheetJson.Read<NodeDataRaw>(json[2], true);
        var events = SheetJson.Read<EventDataRaw>(json[3]);
        var ids = new HashSet<string>(nodes.Select(n => n.NodeID), StringComparer.Ordinal);
        var eventIds = new HashSet<string>(events.Select(e => e.EventID), StringComparer.Ordinal);
        if (nodes.Length != 67 || events.Length != 17 || !ids.Contains("DEMO_01"))
            throw new InvalidOperationException("승인된 데모 스냅샷(67노드/17이벤트/DEMO_01)이 아닙니다.");
        var battles = nodes.Where(n => n.NodeType == nameof(NodeType.CombatNode)).ToArray();
        if (battles.Length != 6 || battles.Any(n => n.FailureNode != "F11") || !ids.Contains("F11"))
            throw new InvalidOperationException("데모 전투 6개의 사망 분기는 F11이어야 합니다.");
        Action<string> check = id => { if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id)) throw new InvalidOperationException("데모 외부 노드 참조: " + id); };
        foreach (var n in nodes)
        {
            check(n.NextNode); check(n.SuccessNode); check(n.FailureNode);
            check(n.Choice1_NextNode); check(n.Choice2_NextNode); check(n.Choice3_NextNode);
            if (!string.IsNullOrWhiteSpace(n.EventDefinitionID) && !eventIds.Contains(n.EventDefinitionID))
                throw new InvalidOperationException("데모 외부 이벤트 참조: " + n.EventDefinitionID);
        }
        foreach (var e in events) check(e.ExitNodeID);
        foreach (var c in SheetJson.Read<EventChoiceDataRaw>(json[4])) check(c.NextNodeID);
        var obsolete = Paths("Nodes").Where(p => !ids.Contains(Path.GetFileNameWithoutExtension(p)))
            .Concat(Paths("EventDefinitions").Where(p => !eventIds.Contains(Path.GetFileNameWithoutExtension(p)))).ToArray();
        // 삭제 전에 참조 범위와 복구본을 확보한다. 유지할 자산이 삭제 대상에 의존하면 중단한다.
        var obsoleteSet = new HashSet<string>(obsolete);
        foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)))
        {
            if (obsoleteSet.Contains(path) || path.StartsWith("Assets/Resources/Nodes/", StringComparison.Ordinal) || path.StartsWith("Assets/Resources/EventDefinitions/", StringComparison.Ordinal)) continue;
            if (AssetDatabase.GetDependencies(path, false).Any(obsoleteSet.Contains))
                throw new InvalidOperationException("삭제 대상에 외부 자산이 의존합니다: " + path);
        }
        string backup = Path.Combine(directory, "asset-backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        foreach (var folder in new[] { "Nodes", "EventDefinitions", "Items", "Characters" })
            foreach (var path in Paths(folder))
                foreach (var source in new[] { path, path + ".meta" })
                {
                    string target = Path.Combine(backup, source);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, false);
                }
        DataImporter.ApplySnapshot(json[0], json[1], json[2], json[3], json[4], json[5]);
        foreach (var path in obsolete)
            if (!AssetDatabase.DeleteAsset(path)) throw new IOException("이전 데모 자산 삭제 실패: " + path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (Paths("Nodes").Length != 67 || Paths("EventDefinitions").Length != 17 || Resources.Load<Node>("Nodes/DEMO_01") == null)
            throw new InvalidOperationException("적용 후 데모 자산 개수/시작 노드 불일치");
        foreach (var row in nodes)
        {
            var node = Resources.Load<Node>("Nodes/" + row.NodeID);
            if (node == null || node.nodeMessage != SheetJson.Multiline(row.NodeMessage))
                throw new InvalidOperationException("적용 후 노드 본문 불일치: " + row.NodeID);
            if (node is CombatNode battle && (battle.failureNode == null || battle.failureNode.name != "F11"))
                throw new InvalidOperationException("적용 후 사망 분기 불일치: " + row.NodeID);
        }
        foreach (var path in Paths("Nodes").Concat(Paths("EventDefinitions")))
        {
            var serialized = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
            var property = serialized.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    throw new InvalidOperationException("누락된 자산 참조: " + path + ":" + property.propertyPath);
        }
        File.WriteAllText(Path.Combine(directory, "unity-apply-result.txt"),
            "Edit Mode 적용 성공: 67노드/17이벤트, 시작 DEMO_01, 전투 6개→F11, 전체 본문 원격 스냅샷 일치, 누락 참조 없음.\n복구본: " + backup);
        Debug.Log("[DemoStoryInstaller] 67노드/17이벤트 적용 완료. 기존 아이템·캐릭터 보존. 복구본: " + backup);
    }

    private static string[] Paths(string folder) => AssetDatabase.FindAssets("", new[] { "Assets/Resources/" + folder })
        .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)).ToArray();
}
