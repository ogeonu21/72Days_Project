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
            choice.requiredFlag = EditorGUILayout.TextField("Required Flag", choice.requiredFlag);
            choice.setFlag = EditorGUILayout.TextField("Set Flag", choice.setFlag);

            //선택지 제거
            if (GUILayout.Button("Remove Choice")) node.choices.RemoveAt(i);

            EditorGUILayout.EndVertical();
        }

        //선택지 추가
        if (GUILayout.Button("Add Choice"))
        {
            node.choices.Add(new Choice());
        }

        //공간 나누기
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Combat Settings", EditorStyles.boldLabel);
        node.triggersCombat = EditorGUILayout.Toggle("Triggers Combat", node.triggersCombat);
        node.fallbackNode = (StoryNode)EditorGUILayout.ObjectField("Fallback Node", node.fallbackNode, typeof(StoryNode), false);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
