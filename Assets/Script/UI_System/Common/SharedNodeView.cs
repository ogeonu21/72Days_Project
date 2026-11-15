using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>본문 출력과 공통 선택지의 수명·배치를 소유한다. 진행 규칙은 모드에 남긴다.</summary>
public sealed class SharedNodeView : MonoBehaviour
{
    public TMP_Text dialogueText;
    public ScrollRect dialogueScroll;
    public RectTransform choiceArea;
    public Button choiceTemplate;
    public ScrollRect choiceScroll;
    public RectTransform choiceContent;
    private UIController mode;
    private MonoBehaviour choiceOwner;
    private Coroutine typing;
    private int textVersion, choiceVersion;
    private bool typingActive;
    private RectTransform attackArea;
    private Vector2 textMin, textMax, textOffsetMin, textOffsetMax;
    private Vector2 choicesMin, choicesMax, choicesOffsetMin, choicesOffsetMax;
    private TextOverflowModes originalOverflow;
    private bool capturedLayout;
    private Vector2 contentMin, contentMax, contentOffsetMin, contentOffsetMax, contentPivot;
    private RectTransform DialogueRect => dialogueScroll != null ? dialogueScroll.transform as RectTransform : dialogueText.rectTransform;
    private readonly Vector3[] corners = new Vector3[4];
    private bool layoutDirty = true;
    private string lastText;
    private Vector2 lastParentSize, lastTextParentSize;
    private float lastPreferred, lastAttackBottom;
    private bool lastChoicesVisible;

    private void OnEnable() => GameEvent.NodeTextUpdate += HandleTextUpdate;
    private void OnDisable()
    {
        GameEvent.NodeTextUpdate -= HandleTextUpdate;
        CancelText();
        ClearChoices();
        RestoreLayout();
        mode = null;
        attackArea = null;
    }

    public void Enter(UIController controller)
    {
        CancelText();
        ClearChoices();
        RestoreLayout();
        mode = controller;
        var combat = controller as CombatUIController;
        attackArea = combat != null && combat.attackButtons != null ? combat.attackButtons.transform as RectTransform : null;
        CaptureLayout();
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);
        layoutDirty = true;
        UpdateLayout();
    }

    public void Exit(UIController controller)
    {
        if (mode != controller) return;
        CancelText();
        ClearChoices();
        RestoreLayout();
        mode = null;
        attackArea = null;
    }

    public IEnumerator PresentText(UIController controller, string text, float speed = .05f)
    {
        if (mode != controller || dialogueText == null) yield break;
        CancelText();
        int version = textVersion;
        typingActive = true;
        typing = StartCoroutine(TypeText(text, speed, version));
        while (version == textVersion && typingActive) yield return null;
    }

    private IEnumerator HandleTextUpdate(string text)
    {
        if (mode != null) yield return PresentText(mode, text);
    }

    private IEnumerator TypeText(string text, float speed, int version)
    {
        yield return TypewriterEffect.TypeTextCoroutine(dialogueText, text ?? string.Empty, speed);
        if (version == textVersion) { typingActive = false; typing = null; }
    }

    private void CancelText()
    {
        textVersion++;
        if (typing != null) StopCoroutine(typing);
        typing = null;
        typingActive = false;
    }

    public bool OpenChoices(MonoBehaviour owner, RectTransform combatAttackArea = null)
    {
        var controller = owner != null ? owner.GetComponentInParent<UIController>() : null;
        if (owner is UIController direct) controller = direct;
        if (controller != mode || choiceArea == null || choiceContent == null || choiceTemplate == null ||
            choiceTemplate.GetComponentInChildren<TMP_Text>(true) == null) return false;
        ClearChoices();
        choiceOwner = owner;
        if (combatAttackArea != null) attackArea = combatAttackArea;
        CaptureLayout();
        choiceArea.gameObject.SetActive(true);
        layoutDirty = true;
        UpdateLayout();
        return true;
    }

    public Transform NewContent(MonoBehaviour owner)
    {
        if (choiceOwner != owner) return null;
        ClearContent();
        choiceArea.gameObject.SetActive(true);
        if (choiceScroll != null)
        {
            choiceScroll.StopMovement();
            choiceScroll.verticalNormalizedPosition = 1;
        }
        return choiceContent;
    }

    public Button AddChoice(MonoBehaviour owner, string label, Action action, bool available = true)
    {
        int version = choiceVersion;
        return ChoiceUIRouter.CreateButton(choiceTemplate, dialogueText != null ? dialogueText.font : null,
            choiceContent, label, () =>
            {
                if (choiceOwner == owner && version == choiceVersion && owner != null && owner.isActiveAndEnabled)
                    action?.Invoke();
            }, available);
    }

    public void HideChoices(MonoBehaviour owner)
    {
        if (choiceOwner == owner) ClearChoices();
    }

    private void ClearChoices()
    {
        ClearContent();
        if (choiceArea != null) choiceArea.gameObject.SetActive(false);
        choiceOwner = null;
        layoutDirty = true;
        UpdateLayout();
    }

    private void ClearContent()
    {
        choiceVersion++;
        if (choiceContent == null) return;
        for (int i = choiceContent.childCount - 1; i >= 0; i--)
        {
            var child = choiceContent.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
    }

    private void CaptureLayout()
    {
        if (capturedLayout || dialogueText == null || choiceArea == null) return;
        var rect = DialogueRect;
        textMin = rect.anchorMin; textMax = rect.anchorMax;
        textOffsetMin = rect.offsetMin; textOffsetMax = rect.offsetMax;
        choicesMin = choiceArea.anchorMin; choicesMax = choiceArea.anchorMax;
        choicesOffsetMin = choiceArea.offsetMin; choicesOffsetMax = choiceArea.offsetMax;
        originalOverflow = dialogueText.overflowMode;
        if (dialogueScroll != null)
        {
            var content = dialogueText.rectTransform;
            contentMin = content.anchorMin; contentMax = content.anchorMax;
            contentOffsetMin = content.offsetMin; contentOffsetMax = content.offsetMax; contentPivot = content.pivot;
        }
        capturedLayout = true;
        layoutDirty = true;
    }

    private void RestoreLayout()
    {
        if (!capturedLayout) return;
        if (dialogueText != null)
        {
            var rect = DialogueRect;
            rect.anchorMin = textMin; rect.anchorMax = textMax;
            rect.offsetMin = textOffsetMin; rect.offsetMax = textOffsetMax;
            dialogueText.overflowMode = originalOverflow;
        }
        if (choiceArea != null)
        {
            choiceArea.anchorMin = choicesMin; choiceArea.anchorMax = choicesMax;
            choiceArea.offsetMin = choicesOffsetMin; choiceArea.offsetMax = choicesOffsetMax;
        }
        RestoreDialogueContent();
        capturedLayout = false;
        layoutDirty = true;
    }

    private void LateUpdate()
    {
        if (mode != null) UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (!capturedLayout || dialogueText == null || choiceArea == null) return;
        var parent = choiceArea.parent as RectTransform;
        var textRect = DialogueRect;
        var textParent = textRect.parent as RectTransform;
        if (parent == null || textParent == null) return;
        bool showingChoices = choiceOwner != null && choiceArea.gameObject.activeSelf;
        float preferred = showingChoices ? LayoutUtility.GetPreferredHeight(choiceContent) : 0;
        float attackBottom = float.PositiveInfinity;
        if (attackArea != null)
        {
            attackArea.GetWorldCorners(corners);
            attackBottom = parent.InverseTransformPoint(corners[0]).y - 24;
        }
        if (!layoutDirty && lastText == dialogueText.text && lastParentSize == parent.rect.size &&
            lastTextParentSize == textParent.rect.size && lastChoicesVisible == showingChoices &&
            Mathf.Approximately(lastPreferred, preferred) &&
            (lastAttackBottom == attackBottom || Mathf.Approximately(lastAttackBottom, attackBottom))) return;
        layoutDirty = false;
        lastText = dialogueText.text; lastParentSize = parent.rect.size; lastTextParentSize = textParent.rect.size;
        lastChoicesVisible = showingChoices; lastPreferred = preferred; lastAttackBottom = attackBottom;
        // 원래 영역을 기준으로 계산해 이전 프레임의 축소가 누적되지 않게 한다.
        textRect.anchorMin = textMin; textRect.anchorMax = textMax;
        textRect.offsetMin = textOffsetMin; textRect.offsetMax = textOffsetMax;
        choiceArea.anchorMin = choicesMin; choiceArea.anchorMax = choicesMax;
        choiceArea.offsetMin = choicesOffsetMin; choiceArea.offsetMax = choicesOffsetMax;
        dialogueText.overflowMode = originalOverflow;
        RestoreDialogueContent();
        if (attackArea == null && !showingChoices) return;
        textRect.GetWorldCorners(corners);
        float originalBottom = parent.InverseTransformPoint(corners[0]).y;
        float topLimit = parent.InverseTransformPoint(corners[1]).y;
        if (attackArea != null) topLimit = Mathf.Min(topLimit, attackBottom);
        float bottom = originalBottom;
        if (showingChoices)
        {
            choiceArea.GetWorldCorners(corners);
            float choiceBottom = parent.InverseTransformPoint(corners[0]).y;
            float maximum = Mathf.Max(0, parent.InverseTransformPoint(corners[1]).y - choiceBottom);
            float width = Mathf.Max(1, textRect.rect.width - dialogueText.margin.x - dialogueText.margin.z);
            float bodyHeight = dialogueText.GetPreferredValues(dialogueText.text, width, Mathf.Infinity).y + 8;
            float available = Mathf.Max(0, topLimit - choiceBottom - 116);
            // 본문을 읽을 높이를 먼저 확보하고 긴 목록은 공통 Scroll로 제한한다.
            float spare = Mathf.Max(124, topLimit - choiceBottom - bodyHeight - 16);
            float height = Mathf.Min(maximum, available, spare, Mathf.Max(124, preferred + 24));
            float choiceTop = choiceBottom + height;
            choiceArea.anchorMin = new Vector2(choicesMin.x, 0);
            choiceArea.anchorMax = new Vector2(choicesMax.x, 0);
            choiceArea.offsetMin = new Vector2(choicesOffsetMin.x, choiceBottom - parent.rect.yMin);
            choiceArea.offsetMax = new Vector2(choicesOffsetMax.x, choiceTop - parent.rect.yMin);
            bottom = Mathf.Max(originalBottom, choiceTop + 16);
        }
        bottom = Mathf.Min(bottom, topLimit);
        float localBottom = textParent.InverseTransformPoint(parent.TransformPoint(new Vector3(0, bottom, 0))).y - textParent.rect.yMin;
        float localTop = textParent.InverseTransformPoint(parent.TransformPoint(new Vector3(0, topLimit, 0))).y - textParent.rect.yMin;
        textRect.anchorMin = new Vector2(textMin.x, 0);
        textRect.anchorMax = new Vector2(textMax.x, 0);
        textRect.offsetMin = new Vector2(textOffsetMin.x, localBottom);
        textRect.offsetMax = new Vector2(textOffsetMax.x, Mathf.Max(localBottom, localTop));
        dialogueText.overflowMode = TextOverflowModes.Masking;
        UpdateDialogueScroll(showingChoices);
    }

    private void RestoreDialogueContent()
    {
        if (dialogueScroll == null || dialogueText == null) return;
        dialogueScroll.StopMovement();
        dialogueScroll.enabled = false;
        var content = dialogueText.rectTransform;
        content.pivot = contentPivot;
        content.anchorMin = contentMin; content.anchorMax = contentMax;
        content.offsetMin = contentOffsetMin; content.offsetMax = contentOffsetMax;
        var image = dialogueScroll.GetComponent<UnityEngine.UI.Image>();
        if (image != null) image.raycastTarget = false;
    }

    private void UpdateDialogueScroll(bool showingChoices)
    {
        if (dialogueScroll == null || dialogueText == null || !showingChoices) return;
        var viewport = DialogueRect;
        var content = dialogueText.rectTransform;
        float width = Mathf.Max(1, content.rect.width - dialogueText.margin.x - dialogueText.margin.z);
        float height = dialogueText.GetPreferredValues(dialogueText.text, width, Mathf.Infinity).y + 8;
        if (height <= viewport.rect.height) return;
        content.pivot = new Vector2(.5f, 1);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(contentOffsetMin.x, -height);
        content.offsetMax = new Vector2(contentOffsetMax.x, 0);
        dialogueScroll.horizontal = false;
        dialogueScroll.vertical = true;
        dialogueScroll.movementType = ScrollRect.MovementType.Clamped;
        dialogueScroll.content = content;
        dialogueScroll.viewport = viewport;
        dialogueScroll.enabled = true;
        dialogueScroll.verticalNormalizedPosition = 1;
        var image = dialogueScroll.GetComponent<UnityEngine.UI.Image>();
        if (image != null) image.raycastTarget = true;
    }
}
