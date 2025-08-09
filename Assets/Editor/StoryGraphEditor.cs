// StoryGraphEditor.cs - Bolt-style FlowGraph 기반 시각 대화 노드 에디터 (포트 텍스트 + 삭제 + 무한 확장)
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class StoryGraphEditor : EditorWindow
{
    private Vector2 scrollPos;
    private List<StoryNode> nodes = new();
    private Dictionary<StoryNode, Rect> nodeRects = new();
    private StoryNode selectedNode;
    private Vector2 contextClickPos;

    private static readonly Vector2 NodeSize = new(280, 300);
    private Rect canvasRect = new Rect(0, 0, 5000, 5000);
    private StoryNode draggingOutputNode;
    private int draggingChoiceIndex = -1;

    [MenuItem("Tools/Story Graph Editor")]
    public static void Open() => GetWindow<StoryGraphEditor>("Story Graph");

    private void OnEnable() => LoadNodes();

    private void OnGUI()
    {
        DrawMiniMap();
        HandleInput(Event.current);

        // ScrollView 시작
        scrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), scrollPos, canvasRect);

        DrawConnections();
        BeginWindows();
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            Rect rect = new Rect(node.editorPosition, NodeSize);
            GUI.color = node == selectedNode ? Color.yellow : new Color(0.95f, 0.95f, 1f);
            rect = GUI.Window(i, rect, id => DrawNodeWindow(id, node), node.name);
            nodeRects[node] = rect;
            // 위치 갱신
            if (node.editorPosition != rect.position)
            {
                node.editorPosition = rect.position;
                EditorUtility.SetDirty(node);
            }
            ExpandCanvasToFit(rect);
        }
        EndWindows();

        // ScrollView 끝
        GUI.EndScrollView();

        DrawPendingConnection(Event.current);

        // Save All 버튼 추가 (ScrollView 외부에 위치)
        GUILayout.BeginArea(new Rect(position.width - 110, position.height - 30, 100, 20));
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save All"))
        {
            SaveAll();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndArea();

        if (GUI.changed) Repaint();
    }

    private void DrawNodeWindow(int id, StoryNode node)
    {
        // 이전 이름을 저장해 둡니다.
        string oldName = node.name;

        // 변경사항 감지를 시작합니다.
        EditorGUI.BeginChangeCheck();

        // 노드 이름 수정 필드
        node.name = EditorGUILayout.TextField("Node", node.name);

        // 이름 변경이 감지되면 파일 이름을 변경합니다.
        if (oldName != node.name)
        {
            string assetPath = AssetDatabase.GetAssetPath(node);
            string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assetPath), node.name + ".asset");
            AssetDatabase.RenameAsset(assetPath, node.name);

            // 이름 변경 후, 에디터에 변경 사항을 알립니다.
            EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets(); // 변경 사항을 즉시 저장합니다.
        }

        // 다이얼로그 텍스트 수정
        node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, GUILayout.Height(40));

        if (node.choices == null) node.choices = new();
        while (node.choices.Count < 3) node.choices.Add(new Choice());

        for (int i = 0; i < 3; i++)
        {
            EditorGUILayout.BeginHorizontal();
            node.choices[i].choiceText = EditorGUILayout.TextField($"Choice {i + 1}", node.choices[i].choiceText);

            // 다음 노드 연결 필드
            node.choices[i].nextNode = (StoryNode)EditorGUILayout.ObjectField(node.choices[i].nextNode, typeof(StoryNode), false);
            EditorGUILayout.EndHorizontal();

            node.choices[i].triggersCombat = EditorGUILayout.Toggle("Combat", node.choices[i].triggersCombat);

            if (node.choices[i].triggersCombat)
            {
                node.choices[i].combatEnemyID = EditorGUILayout.TextField("Enemy ID", node.choices[i].combatEnemyID);
            }
        }

        GUILayout.Space(10);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete Node"))
        {
            DeleteNode(node);
            return;
        }
        GUI.backgroundColor = Color.white;

        // 이름 변경 외 다른 필드가 변경되었는지 확인하고 저장합니다.
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(node);
        }

        GUI.DragWindow();
    }

    private void DrawConnections()
{
    Handles.BeginGUI();
    foreach (var node in nodes)
    {
        if (node.choices == null) continue;
        for (int i = 0; i < node.choices.Count && i < 3; i++)
        {
            var choice = node.choices[i];
            if (choice.nextNode != null && nodeRects.ContainsKey(node) && nodeRects.ContainsKey(choice.nextNode))
            {
                Rect from = nodeRects[node];
                Rect to = nodeRects[choice.nextNode];
                Vector3 start = new(from.xMax, from.y + 80 + i * 40);
                Vector3 end = new(to.xMin, to.center.y);
                Handles.DrawBezier(start, end, start + Vector3.right * 50, end + Vector3.left * 50, Color.white, null, 2);
            }
        }
    }
    Handles.EndGUI();
}

private void DrawPendingConnection(Event e)
{
    if (draggingOutputNode != null && draggingChoiceIndex >= 0)
    {
        Rect from = nodeRects[draggingOutputNode];
        Vector3 start = new(from.xMax, from.y + 80 + draggingChoiceIndex * 40);
        Vector3 end = e.mousePosition;
        Handles.DrawBezier(start, end, start + Vector3.right * 50, end + Vector3.left * 50, Color.gray, null, 2);
        Repaint();
    }

    if (e.type == EventType.MouseUp && draggingOutputNode != null)
    {
        foreach (var kvp in nodeRects)
        {
            if (kvp.Value.Contains(e.mousePosition) && kvp.Key != draggingOutputNode)
            {
                draggingOutputNode.choices[draggingChoiceIndex].nextNode = kvp.Key;
                EditorUtility.SetDirty(draggingOutputNode);
                break;
            }
        }
        draggingOutputNode = null;
        draggingChoiceIndex = -1;
        e.Use();
    }
}

private Vector2 dragStart;
private bool isDraggingCanvas = false;
private bool rightMouseDown = false;

private void HandleInput(Event e)
{
    if (e.type == EventType.MouseDown && e.button == 1)
    {
        dragStart = e.mousePosition;
        rightMouseDown = true;
    }
    else if (e.type == EventType.MouseDrag && rightMouseDown)
    {
        Vector2 delta = dragStart - e.mousePosition;
        if (!isDraggingCanvas && delta.magnitude > 3f)
        {
            isDraggingCanvas = true;
        }

        if (isDraggingCanvas)
        {
            scrollPos += delta;
            dragStart = e.mousePosition;
            e.Use();
        }
    }
    else if (e.type == EventType.MouseUp && e.button == 1)
    {
        if (!isDraggingCanvas)
        {
            contextClickPos = e.mousePosition + scrollPos;
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Add Story Node"), false, CreateNode);
            menu.ShowAsContext();
        }

        rightMouseDown = false;
        isDraggingCanvas = false;
        e.Use();
    }
    else if (e.type == EventType.MouseDrag && isDraggingCanvas)
    {
        Vector2 delta = dragStart - e.mousePosition;
        scrollPos += delta;
        dragStart = e.mousePosition;
        e.Use();
    }
    else if (e.type == EventType.MouseUp && isDraggingCanvas)
    {
        isDraggingCanvas = false;
        e.Use();
    }

    if (e.type == EventType.ContextClick)
    {
        contextClickPos = e.mousePosition + scrollPos;
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("Add Story Node"), false, CreateNode);
        menu.ShowAsContext();
        e.Use();
    }
}

private void ExpandCanvasToFit(Rect rect)
{
    float right = rect.xMax + 250;
    float bottom = rect.yMax + 250;
    if (right > canvasRect.width) canvasRect.width = right;
    if (bottom > canvasRect.height) canvasRect.height = bottom;
}

private void DrawMiniMap()
{
    const float minimapWidth = 200f;
    const float minimapHeight = 200f;
    Rect minimapRect = new Rect(position.width - minimapWidth - 10, 10, minimapWidth, minimapHeight);
    GUI.Box(minimapRect, GUIContent.none);

    float scaleX = minimapRect.width / canvasRect.width;
    float scaleY = minimapRect.height / canvasRect.height;
    float scale = Mathf.Min(scaleX, scaleY);

    Dictionary<StoryNode, Rect> miniRects = new();

    foreach (var kvp in nodeRects)
    {
        Rect node = kvp.Value;
        Rect miniNode = new Rect(
            minimapRect.x + node.x * scale,
            minimapRect.y + node.y * scale,
            NodeSize.x * scale,
            NodeSize.y * scale);
        EditorGUI.DrawRect(miniNode, Color.white);
        miniRects[kvp.Key] = miniNode;
    }

    Rect viewRect = new Rect(
        minimapRect.x + scrollPos.x * scale,
        minimapRect.y + scrollPos.y * scale,
        position.width * scale,
        position.height * scale);
    EditorGUI.DrawRect(viewRect, new Color(1f, 1f, 0f, 0.3f));

    if (Event.current.type == EventType.MouseDown && minimapRect.Contains(Event.current.mousePosition))
    {
        Vector2 click = Event.current.mousePosition;
        scrollPos = new Vector2(
            (click.x - minimapRect.x) / scale - position.width / 2,
            (click.y - minimapRect.y) / scale - position.height / 2);
        Event.current.Use();
    }

    }

    private void CreateNode()
{
    string name = "StoryNode";
    string folder = "Assets/Resources/Story";
    int count = 1;
    string path = $"{folder}/{name}.asset";
    while (AssetDatabase.LoadAssetAtPath<StoryNode>(path) != null)
    {
        path = $"{folder}/{name}{count++}.asset";
    }

    StoryNode node = ScriptableObject.CreateInstance<StoryNode>();
    node.name = System.IO.Path.GetFileNameWithoutExtension(path);
    node.editorPosition = contextClickPos;
    AssetDatabase.CreateAsset(node, path);
    AssetDatabase.SaveAssets();

    nodes.Add(node);
    nodeRects[node] = new Rect(node.editorPosition, NodeSize);
    ExpandCanvasToFit(nodeRects[node]);
}

private void DeleteNode(StoryNode node)
{
    foreach (var n in nodes)
    {
        if (n.choices == null) continue;
        foreach (var c in n.choices)
            if (c.nextNode == node) c.nextNode = null;
    }
    string path = AssetDatabase.GetAssetPath(node);
    nodes.Remove(node);
    nodeRects.Remove(node);
    AssetDatabase.DeleteAsset(path);
    AssetDatabase.SaveAssets();
}

private void LoadNodes()
{
    nodes.Clear();
    nodeRects.Clear();
    string[] guids = AssetDatabase.FindAssets("t:StoryNode");
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        StoryNode node = AssetDatabase.LoadAssetAtPath<StoryNode>(path);
        if (node != null)
        {
            nodes.Add(node);
            nodeRects[node] = new Rect(node.editorPosition, NodeSize);
            ExpandCanvasToFit(nodeRects[node]);
        }
    }
}

private void SaveAll()
{
    foreach (var node in nodes)
        EditorUtility.SetDirty(node);
    AssetDatabase.SaveAssets();
}
}
