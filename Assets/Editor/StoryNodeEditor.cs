using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(StoryNode))]
public class StoryNodeEditor : Editor
{  
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        StoryNode node = (StoryNode)target;

        EditorGUILayout.LabelField("Dialogue Text", EditorStyles.boldLabel);
        node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, GUILayout.MinHeight(60));

        //공간 나누기
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);
        for (int i = 0; i < node.choices.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            var choice = node.choices[i];

            choice.choiceText = EditorGUILayout.TextField("Choice Text", choice.choiceText);
            choice.nextNode = (StoryNode)EditorGUILayout.ObjectField("Next Node", choice.nextNode, typeof(StoryNode), false);

            //선택지 조건 표기. 지금 당장은 필요 없는 기능.
            choice.requiredFlag = EditorGUILayout.TextField("Required Flag", choice.requiredFlag);
            choice.setFlag = EditorGUILayout.TextField("Set Flag", choice.setFlag);

            //
            EditorGUILayout.LabelField("Combat Settings", EditorStyles.boldLabel);
            choice.triggersCombat = EditorGUILayout.Toggle("Triggers Combat", choice.triggersCombat);
            choice.combatEnemyID = EditorGUILayout.TextField("CombatEnemyID", choice.combatEnemyID);

            //선택지 제거
            if (GUILayout.Button("Remove Choice")) node.choices.RemoveAt(i);

            EditorGUILayout.EndVertical();
        }

        //선택지 추가
        if (GUILayout.Button("Add Choice"))
        {
            node.choices.Add(new Choice());
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
