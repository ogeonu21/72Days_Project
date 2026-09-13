using UnityEngine;

public class SpawnPointRegister : MonoBehaviour
{

    private void Awake()
    {
        if (CharacterManager.Instance != null)
        {
                CharacterManager.Instance.RegisterPlayerParent(this.transform);
                CharacterManager.Instance.RegisterEnemyParent(this.transform);
        }
        else
        {
            Debug.LogError("CharacterManager가 존재하지 않습니다!");
        }
    }
}
