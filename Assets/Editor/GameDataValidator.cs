using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>읽기 전용 콘텐츠 검사. 오류가 있는 콘텐츠는 빌드에 포함하지 않는다.</summary>
public sealed class GameDataValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var errors = ValidateAll();
        if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
    }

    [MenuItem("Tools/Validate Game Data")]
    public static void ValidateMenu()
    {
        var errors = ValidateAll();
        foreach (var error in errors) Debug.LogError(error);
        Debug.Log($"[GameDataValidator] 검사 완료: 오류 {errors.Count}개");
    }

    public static List<string> ValidateAll()
    {
        var errors = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Node", new[] { "Assets/Resources/Nodes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ValidateNode(AssetDatabase.LoadAssetAtPath<Node>(path), path, errors);
        }
        var ids = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:BaseItem", new[] { "Assets/Resources/Items" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
            if (string.IsNullOrWhiteSpace(item.itemID) || !ids.Add(item.itemID)) errors.Add(path + ": 아이템 ID 누락/중복");
            if (item.name != item.itemID) errors.Add(path + ": 파일명과 아이템 ID 불일치");
        }
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/Resources/Characters" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy.itemDropRate > 0 && enemy.dropItem == null) errors.Add(path + ": 드롭 확률은 있으나 아이템 누락");
        }
        return errors;
    }

    public static void ValidateNode(Node node, string path, List<string> errors)
    {
        if (node == null) { errors.Add(path + ": 노드 누락"); return; }
        if (node.GetType().Name != node.nodeType.ToString()) errors.Add(path + ": 실제 타입과 nodeType 불일치");
        if (node is MainStoryNode main && main.nextNode == null) errors.Add(path + ": nextNode 누락");
        if (node is CombatNode combat)
        {
            if (combat.enemyData == null) errors.Add(path + ": 전투 적 누락");
            if (combat.successNode == null) errors.Add(path + ": successNode 누락");
            if (combat.failureNode == null) errors.Add(path + ": failureNode 누락");
        }
        List<Choice> choices = node is StoryNode story ? story.choices : node is EventNode ev ? ev.choices : null;
        if (!(node is StoryNode) && !(node is EventNode)) return;
        if (choices == null || choices.Count == 0) { errors.Add(path + ": 선택지 없음"); return; }
        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            string at = path + $": 선택지[{i}] ";
            if (choice == null) { errors.Add(at + "null"); continue; }
            if (string.IsNullOrWhiteSpace(choice.choiceText)) errors.Add(at + "문구 누락");
            if (choice.nextNode == null) errors.Add(at + "다음 노드 누락");
            if ((node is EventNode || choice.triggersEvent) && choice.baseEvent == null) errors.Add(at + "이벤트 누락");
        }
    }
}
