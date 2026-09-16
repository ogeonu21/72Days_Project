using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class NodeScreenRouter
{
    private readonly List<UIController> uiControllers;
    public NodeScreenRouter(List<UIController> controllers) { uiControllers = controllers; }
    public void Show(Node node, Enemy enemy)
    {
        if (node == null)
        {
            Debug.LogWarning("노드 오류 발생");
            return;
        }






        DeactivateAllUI();

        // 노드 타입에 따라 특정 UI 활성화
        switch (node.nodeType)
        {
            case NodeType.MainStoryNode:
            case NodeType.StoryNode:
                ActivateUI<StoryUIController>(node);
                SetEnemy(enemy, false);
                break;
            case NodeType.CombatNode:
                ActivateUI<CombatUIController>(node);
                SetEnemy(enemy, true);
                break;
            case NodeType.EventNode:
                ActivateUI<EventUIController>(node);
                SetEnemy(enemy, false);
                break;
            case NodeType.EndingNode:
                ActivateUI<EndingUIController>(node);
                SetEnemy(enemy, false);
                break;
            default:
                Debug.LogWarning($"알 수 없는 노드 타입입니다: {node.nodeType}");
                break;
        }
    }

    private void DeactivateAllUI()
    {
        foreach (var controller in uiControllers)
        {
            if (controller != null)
            {
                controller.gameObject.SetActive(false);
            }
        }
    }

    private void ActivateUI<T>(Node node) where T : MonoBehaviour, IUpdatableUI
    {
        var targetUI = uiControllers.FirstOrDefault(ui => ui is T);
        if (targetUI != null)
        {
            targetUI.gameObject.SetActive(true);
            (targetUI as IUpdatableUI)?.UpdateUI(node);
        }
        else
        {
            Debug.LogError($"{typeof(T).Name} UI를 찾을 수 없습니다. Inspector를 확인하세요.");
        }
    }

    private static void SetEnemy(Enemy enemy, bool active)
    {
        if (enemy != null) enemy.gameObject.SetActive(active);
    }
}
