using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Achievement screen. Reads unlock state from SaveManager + ScriptableObject thresholds.
/// Wire each MilestoneSlot in the Inspector: assign the SO + locked/unlocked GameObjects.
public class AchievementUI : MonoBehaviour
{
    [System.Serializable]
    public class ToolMilestoneSlot
    {
        [Tooltip("Drag the ToolData SO here. Unlock score is read at runtime.")]
        public ToolData toolData;
        public GameObject lockedIcon;
        public GameObject unlockedIcon;
        [Tooltip("Optional — auto-filled with tool name + unlock score.")]
        public TextMeshProUGUI labelText;
    }

    [System.Serializable]
    public class MapMilestoneSlot
    {
        [Tooltip("Drag the MapData SO here. Unlock score is read at runtime.")]
        public MapData mapData;
        public GameObject lockedIcon;
        public GameObject unlockedIcon;
        [Tooltip("Optional — auto-filled with map name + unlock score.")]
        public TextMeshProUGUI labelText;
    }

    [Header("Tool Milestones")]
    [SerializeField] private ToolMilestoneSlot[] _toolSlots;

    [Header("Map Milestones")]
    [SerializeField] private MapMilestoneSlot[] _mapSlots;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _totalPickupsText;
    [SerializeField] private TextMeshProUGUI _totalKillsText;
    [SerializeField] private TextMeshProUGUI _totalGamesText;

    [Header("Navigation")]
    [SerializeField] private Button _backButton;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private void Start()
    {
        int highScore = SaveManager.Data.highScore;

        RefreshToolMilestones(highScore);
        RefreshMapMilestones(highScore);
        RefreshStats();

        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);
    }

    private void RefreshToolMilestones(int highScore)
    {
        if (_toolSlots == null) return;

        foreach (var slot in _toolSlots)
        {
            if (slot.toolData == null) continue;

            bool unlocked = highScore >= slot.toolData.unlockScore;
            if (slot.lockedIcon != null) slot.lockedIcon.SetActive(!unlocked);
            if (slot.unlockedIcon != null) slot.unlockedIcon.SetActive(unlocked);

            if (slot.labelText != null)
            {
                string status = unlocked ? "UNLOCKED" : $"Score {slot.toolData.unlockScore}";
                slot.labelText.text = $"{slot.toolData.toolName} — {status}";
            }
        }
    }

    private void RefreshMapMilestones(int highScore)
    {
        if (_mapSlots == null) return;

        foreach (var slot in _mapSlots)
        {
            if (slot.mapData == null) continue;

            bool unlocked = slot.mapData.IsUnlocked(highScore);
            if (slot.lockedIcon != null) slot.lockedIcon.SetActive(!unlocked);
            if (slot.unlockedIcon != null) slot.unlockedIcon.SetActive(unlocked);

            if (slot.labelText != null)
            {
                string status = unlocked ? "UNLOCKED" : $"Score {slot.mapData.unlockScore}";
                slot.labelText.text = $"{slot.mapData.mapName} — {status}";
            }
        }
    }

    private void RefreshStats()
    {
        var data = SaveManager.Data;
        if (_highScoreText != null) _highScoreText.text = $"BEST: {data.highScore}";
        if (_totalPickupsText != null) _totalPickupsText.text = $"PICKUPS: {data.totalToolPickups}";
        if (_totalKillsText != null) _totalKillsText.text = $"KILLS: {data.totalEnemiesKilled}";
        if (_totalGamesText != null) _totalGamesText.text = $"GAMES: {data.totalGamesPlayed}";
    }

    private void OnBackClicked()
    {
        if (_onLoadScene != null && _onLoadScene.HasListeners)
            _onLoadScene.Raise(SceneNames.MainMenu);
        else
            SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
