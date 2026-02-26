using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Menu screen. Play button starts the game.
/// Shows high score.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button _playButton;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private void Awake()
    {
        if (_playButton != null)
            _playButton.onClick.AddListener(OnPlayClicked);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        // Show high score
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (_highScoreText != null)
            _highScoreText.text = highScore > 0 ? $"BEST: {highScore}" : "";
    }

    private void OnDestroy()
    {
    }

    public void OnPlayClicked()
    {
        if (_onLoadScene != null && _onLoadScene.HasListeners)
        {
            _onLoadScene.Raise("Game");
        }
        else
        {
            Debug.Log("[MainMenuUI] No scene loader listeners — loading Game directly.");
            SceneManager.LoadScene("Game");
        }
    }
}