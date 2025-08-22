using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Node), true)]
public class NodeEditor : Editor
{
    private SerializedProperty nodeTypeProp;

    private void OnEnable()
    {
        // "nodeType" 프로퍼티를 찾습니다.
        nodeTypeProp = serializedObject.FindProperty("nodeType");
    }

    public override void OnInspectorGUI()
    {
        // Inspector GUI를 업데이트하기 시작합니다.
        serializedObject.Update();

        // 노드 타입 선택을 위한 Enum 팝업을 그립니다.
        NodeType newType = (NodeType)EditorGUILayout.EnumPopup("Node Type", (NodeType)nodeTypeProp.enumValueIndex);

        // 노드 타입이 변경되었는지 확인합니다.
        if (newType != (NodeType)nodeTypeProp.enumValueIndex)
        {
            // 노드 타입 변경을 처리하는 함수를 호출합니다.
            ChangeNodeType(newType);
        }

        // 기본 Inspector GUI를 그립니다.
        DrawDefaultInspector();
    }

    private void ChangeNodeType(NodeType newType)
    {
        // 현재 선택된 노드 인스턴스를 가져옵니다.
        Node current = (Node)target;
        Node newNode = null;

        // 새로운 타입에 따라 다른 노드 인스턴스를 생성합니다.
        switch (newType)
        {
            case NodeType.MainStoryNode:
                newNode = ScriptableObject.CreateInstance<MainStoryNode>();
                break;
            case NodeType.StoryNode:
                newNode = ScriptableObject.CreateInstance<StoryNode>();
                break;
            case NodeType.CombatNode:
                newNode = ScriptableObject.CreateInstance<CombatNode>();
                break;
            case NodeType.EventNode:
                newNode = ScriptableObject.CreateInstance<EventNode>();
                break;
            case NodeType.EndingNode:
                newNode = ScriptableObject.CreateInstance<EndingNode>();
                break;
            default:
                Debug.LogError("알 수 없는 노드 타입입니다.");
                return;
        }

        if (newNode != null)
        {
            // 기존 노드에서 새 노드로 데이터 복사
            EditorUtility.CopySerialized(current, newNode);
            newNode.nodeType = newType;

            // 기존 에셋을 삭제하고 새 에셋으로 저장
            string path = AssetDatabase.GetAssetPath(current);
            AssetDatabase.CreateAsset(newNode, path);
            AssetDatabase.SaveAssets();

            // Inspector에서 새 노드를 선택하도록 갱신
            EditorUtility.SetDirty(newNode);
            Selection.activeObject = newNode;
        }
    }
}