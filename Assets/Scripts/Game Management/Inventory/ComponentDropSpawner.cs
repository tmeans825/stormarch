using UnityEngine;
using UnityEngine.SceneManagement;

public class ComponentDropSpawner : MonoBehaviour
{
    public static ComponentDropSpawner instance;

    [SerializeField] private GameObject componentDropPrefab;

    private GameObject currentInstance;
    private bool subscribed = false;

    void Awake()
    {
        if (instance == null && this != instance) instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    // Subscribe in Start, not OnEnable: OnEnable can run before GameEvents.current
    // is assigned depending on scene execution order. By Start, all Awakes have run.
    void Start()
    {
        if (GameEvents.current != null && !subscribed)
        {
            GameEvents.current.onPreviousSceneDataLoaded += spawnIfNeeded;
            subscribed = true;
        }
    }

    void OnDestroy()
    {
        if (subscribed && GameEvents.current != null)
            GameEvents.current.onPreviousSceneDataLoaded -= spawnIfNeeded;
    }

    private void spawnIfNeeded()
    {
        if (currentInstance != null) return;

        var pc = PlayerController.instance;
        if (pc == null) return;
        if (pc.deathDropComponentCount <= 0) return;
        if (pc.deathDropScene != SceneManager.GetActiveScene().name) return;

        // No parent → goes into the active scene → dies with the scene on next load.
        // Do NOT parent to ObjectPooler (DontDestroyOnLoad) — drop must scope to scene.
        currentInstance = Instantiate(componentDropPrefab, pc.deathDropPos, Quaternion.identity);
    }
}
