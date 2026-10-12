using UnityEngine;

/// <summary>생성한 상태효과 스프라이트를 양쪽 캐릭터가 공유한다.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CharacterEffectIcon : UnityEngine.UI.Image
{
    private static readonly string[] ResourcePaths =
    {
        "UI/StatusEffects/attack-down",
        "UI/StatusEffects/dodge-down",
        "UI/StatusEffects/bleeding"
    };
    private static readonly Sprite[] SharedSprites = new Sprite[3];
    private CharacterEffect effect;

    public CharacterEffect Effect
    {
        get => effect;
        set
        {
            effect = value;
            int index = (int)value;
            if (index < 0 || index >= ResourcePaths.Length)
            {
                sprite = null;
                enabled = false;
                return;
            }
            if (SharedSprites[index] == null)
                SharedSprites[index] = Resources.Load<Sprite>(ResourcePaths[index]);
            sprite = SharedSprites[index];
            enabled = sprite != null;
            if (sprite == null) Debug.LogError("[CharacterEffectIcon] 상태효과 스프라이트 누락: " + ResourcePaths[index], this);
            color = Color.white;
            preserveAspect = true;
            raycastTarget = false;
        }
    }
}
