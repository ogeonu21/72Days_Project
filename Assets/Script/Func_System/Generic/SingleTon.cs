using UnityEngine;

public class SingleTon<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    Debug.LogError($"[SingleTon] 필수 매니저 '{typeof(T).Name}'를 찾을 수 없습니다. " +
                                   "시작 씬 또는 해당 기능 씬에 명시적으로 배치해 주세요.");
                }
            }
            return instance;
        }
    }

    // 이 변수를 이용해 글로벌/씬 매니저를 구분합니다.
    [SerializeField]
    protected bool isGlobal = false;

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this as T;

        // isGlobal 변수가 true일 때만 DontDestroyOnLoad를 호출합니다.
        if (isGlobal)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

}
