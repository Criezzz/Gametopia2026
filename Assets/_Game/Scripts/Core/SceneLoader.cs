using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Async scene loader. Listens to a StringEventChannel for scene transitions.
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onSceneTransition;

    [Header("Settings")]
    [SerializeField] private float _minimumLoadTime = 0.5f;

    private bool _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (_onSceneTransition != null)
            _onSceneTransition.Register(LoadScene);
    }

    private void OnDisable()
    {
        if (_onSceneTransition != null)
            _onSceneTransition.Unregister(LoadScene);
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        _isLoading = true;
        Debug.Log($"[SceneLoader] Loading scene: {sceneName}");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found in Build Settings.");
            _isLoading = false;
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        float elapsedTime = 0f;

        while (!asyncLoad.isDone)
        {
            elapsedTime += Time.unscaledDeltaTime;

            // Wait until loading is at 90% (Unity holds at 0.9 until allowSceneActivation)
            if (asyncLoad.progress >= 0.9f && elapsedTime >= _minimumLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        _isLoading = false;
        Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
    }

    public void LoadSceneByIndex(int buildIndex)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (!string.IsNullOrEmpty(scenePath))
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[SceneLoader] No scene found at build index: {buildIndex}");
        }
    }
}