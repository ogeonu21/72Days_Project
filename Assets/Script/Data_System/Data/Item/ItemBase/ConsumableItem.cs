using UnityEngine;

[System.Serializable]
public class ConsumableItem : BaseItem
{
    public int quantity; //수량

    public ConsumableItem CreateRuntimeCopy(int count)
    {
        var copy = Instantiate(this);
        copy.hideFlags = HideFlags.DontSave;
        copy.quantity = count;
        return copy;
    }

    public static void ReleaseRuntimeCopy(BaseItem item)
    {
        if (!(item is ConsumableItem) || (item.hideFlags & HideFlags.DontSave) != HideFlags.DontSave) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }

    // public int weaponDamageAmount;
    // public int weaponAttackDistance;
    public override void Use(Player player)
    {
        if(quantity <= 0){ return;}
    }
}

