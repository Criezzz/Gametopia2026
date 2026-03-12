using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Main Menu screen. Play goes to Map Picker, with Settings and Achievement buttons.
public class MainMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button _soloButton;
    [SerializeField] private Button _arenaButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _achievementButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("Game Modes")]
    [SerializeField] private GameModeData _soloMode;
    [SerializeField] private GameModeData _arenaMode;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private void Awake()
    {
        if (_soloButton != null) { _soloButton.onClick.RemoveAllListeners(); _soloButton.onClick.AddListener(OnSoloClicked); }
        if (_arenaButton != null) { _arenaButton.onClick.RemoveAllListeners(); _arenaButton.onClick.AddListener(OnArenaClicked); }
        if (_settingsButton != null) { _settingsButton.onClick.RemoveAllListeners(); _settingsButton.onClick.AddListener(OnSettingsClicked); }
        if (_achievementButton != null) { _achievementButton.onClick.RemoveAllListeners(); _achievementButton.onClick.AddListener(OnAchievementClicked); }
        if (_quitButton != null) { _quitButton.onClick.RemoveAllListeners(); _quitButton.onClick.AddListener(OnQuitClicked); }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        int highScore = SaveManager.Data.highScore;
        if (_highScoreText != null)
            _highScoreText.text = highScore > 0 ? $"BEST: {highScore}" : "";
    }

    public void OnSoloClicked()
    {
        if (_soloMode != null)
            GameManager.PendingGameMode = _soloMode;

        LoadScene(SceneNames.MapPicker);
        Debug.Log("Solo clicked");
    }

    public void OnArenaClicked()
    {
        if (_arenaMode != null)
            GameManager.PendingGameMode = _arenaMode;

        LoadScene(SceneNames.MapPicker);
        Debug.Log("Arena clicked");
    }

    public void OnSettingsClicked()
    {
        LoadScene(SceneNames.Settings);
    }

    public void OnAchievementClicked()
    {
        LoadScene(SceneNames.Achievement);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadScene(string sceneName)
    {
        if (_onLoadScene != null && _onLoadScene.HasListeners)
            _onLoadScene.Raise(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}