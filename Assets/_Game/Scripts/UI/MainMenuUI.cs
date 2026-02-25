using UnityEngine;
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

    private void Start()
    {
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
        _onLoadScene?.Raise("Game");
    }
}