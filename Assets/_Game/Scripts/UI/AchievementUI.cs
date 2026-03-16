using UnityEngine;
using UnityEngine.EventSystems;
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
        public ToolData toolData;
        public GameObject lockedIcon;
        public GameObject unlockedIcon;
        public Image iconImage;
        public TextMeshProUGUI labelText;
    }

    [System.Serializable]
    public class MapMilestoneSlot
    {
        public MapData mapData;
        public GameObject lockedIcon;
        public GameObject unlockedIcon;
        public Image iconImage;
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

    [Header("Tooltip")]
    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private TextMeshProUGUI _tooltipText;

    [Header("Navigation")]
    [SerializeField] private Button _backButton;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private static readonly Color LockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    private void Start()
    {
        var data = SaveManager.Data;

        RefreshToolMilestones(data);
        RefreshMapMilestones(data);
        RefreshStats();
        SetupTooltipTriggers();

        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);

        if (_tooltipPanel != null)
            _tooltipPanel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Tool milestones
    // ------------------------------------------------------------------

    private void RefreshToolMilestones(SaveData data)
    {
        if (_toolSlots == null) return;

        foreach (var slot in _toolSlots)
        {
            if (slot.toolData == null) continue;

            bool unlocked = data.totalToolPickups >= slot.toolData.unlockPickupCount;
            if (slot.lockedIcon != null) slot.lockedIcon.SetActive(!unlocked);
            if (slot.unlockedIcon != null)
            {
                slot.unlockedIcon.SetActive(unlocked);
                ToggleGlow(slot.unlockedIcon.transform.parent, unlocked);
            }

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = slot.toolData.toolIcon;
                slot.iconImage.color = unlocked ? Color.white : LockedTint;
                slot.iconImage.enabled = slot.toolData.toolIcon != null;
            }

            if (slot.labelText != null)
            {
                string status = unlocked ? "UNLOCKED" : $"Pickups {slot.toolData.unlockPickupCount}";
                slot.labelText.text = $"{slot.toolData.toolName}\n{status}";
            }
        }
    }

    // ------------------------------------------------------------------
    // Map milestones
    // ------------------------------------------------------------------

    private void RefreshMapMilestones(SaveData data)
    {
        if (_mapSlots == null) return;

        foreach (var slot in _mapSlots)
        {
            if (slot.mapData == null) continue;

            bool unlocked = slot.mapData.IsUnlocked(data);
            if (slot.lockedIcon != null) slot.lockedIcon.SetActive(!unlocked);
            if (slot.unlockedIcon != null)
            {
                slot.unlockedIcon.SetActive(unlocked);
                ToggleGlow(slot.unlockedIcon.transform.parent, unlocked);
            }

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = slot.mapData.previewSprite;
                slot.iconImage.color = unlocked ? Color.white : LockedTint;
                slot.iconImage.enabled = slot.mapData.previewSprite != null;
            }

            if (slot.labelText != null)
            {
                string status = unlocked ? "UNLOCKED" : slot.mapData.GetUnlockDescription();
                slot.labelText.text = $"{slot.mapData.mapName}\n{status}";
            }
        }
    }

    // ------------------------------------------------------------------
    // Stats
    // ------------------------------------------------------------------

    private void RefreshStats()
    {
        var data = SaveManager.Data;
        if (_highScoreText != null) _highScoreText.text = $"BEST: {data.highScore}";
        if (_totalPickupsText != null) _totalPickupsText.text = $"PICKUPS: {data.totalToolPickups}";
        if (_totalKillsText != null) _totalKillsText.text = $"KILLS: {data.totalEnemiesKilled}";
        if (_totalGamesText != null) _totalGamesText.text = $"GAMES: {data.totalGamesPlayed}";
    }

    // ------------------------------------------------------------------
    // Tooltip
    // ------------------------------------------------------------------

    private void SetupTooltipTriggers()
    {
        if (_tooltipPanel == null || _tooltipText == null) return;

        if (_toolSlots != null)
        {
            foreach (var slot in _toolSlots)
            {
                if (slot.toolData == null || slot.lockedIcon == null) continue;
                var card = slot.lockedIcon.transform.parent;
                if (card == null) continue;
                AddTooltipTrigger(card.gameObject, () => ShowToolTooltip(slot));
            }
        }

        if (_mapSlots != null)
        {
            foreach (var slot in _mapSlots)
            {
                if (slot.mapData == null || slot.lockedIcon == null) continue;
                var card = slot.lockedIcon.transform.parent;
                if (card == null) continue;
                AddTooltipTrigger(card.gameObject, () => ShowMapTooltip(slot));
            }
        }
    }

    private void AddTooltipTrigger(GameObject target, System.Action onEnter)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        var img = target.GetComponent<Image>();
        if (img == null)
        {
            img = target.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
        }
        img.raycastTarget = true;

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exitEntry);
    }

    private void ShowToolTooltip(ToolMilestoneSlot slot)
    {
        if (_tooltipPanel == null || _tooltipText == null || slot.toolData == null) return;

        var t = slot.toolData;
        bool unlocked = SaveManager.Data.totalToolPickups >= t.unlockPickupCount;
        string unlockLine = unlocked ? "<color=#66FF66>UNLOCKED</color>" : $"<color=#FFAA33>Unlock: Pickups {t.unlockPickupCount}</color>";

        string desc = string.IsNullOrEmpty(t.description) ? "" : $"\n{t.description}";
        string stats = $"Type: {t.toolType}  |  DMG: {t.damage}  |  CD: {t.cooldown:F1}s";

        _tooltipText.text = $"<b>{t.toolName}</b>\n{unlockLine}{desc}\n<size=80%>{stats}</size>";
        _tooltipPanel.SetActive(true);
    }

    private void ShowMapTooltip(MapMilestoneSlot slot)
    {
        if (_tooltipPanel == null || _tooltipText == null || slot.mapData == null) return;

        var m = slot.mapData;
        bool unlocked = m.IsUnlocked(SaveManager.Data);
        string unlockLine = unlocked
            ? "<color=#66FF66>UNLOCKED</color>"
            : $"<color=#FFAA33>Unlock: {m.GetUnlockDescription()}</color>";

        _tooltipText.text = $"<b>{m.mapName}</b>\n{unlockLine}";
        _tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (_tooltipPanel != null)
            _tooltipPanel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void ToggleGlow(Transform card, bool on)
    {
        if (card == null) return;
        var glow = card.Find("Glow");
        if (glow != null) glow.gameObject.SetActive(on);
    }

    private void OnBackClicked()
    {
        HideTooltip();
        if (MenuSlideController.Instance != null)
            MenuSlideController.Instance.ShowMainMenu();
        else if (_onLoadScene != null && _onLoadScene.HasListeners)
            _onLoadScene.Raise(SceneNames.MainMenu);
        else
            SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
