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
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
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