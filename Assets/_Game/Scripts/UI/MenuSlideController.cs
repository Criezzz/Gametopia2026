using System.Collections;
using UnityEngine;

/// <summary>
/// Manages horizontal slide transitions between MainMenu, Settings, and Achievement panels.
/// Background stays static; only the content panels slide.
/// Settings enters from the right, Achievement enters from the left.
/// </summary>
public class MenuSlideController : MonoBehaviour
{
    public static MenuSlideController Instance { get; private set; }

    [Header("Panels")]
    [Tooltip("The main menu content panel (title, buttons). Starts at center.")]
    [SerializeField] private RectTransform _mainMenuPanel;
    [Tooltip("The settings panel. Starts off-screen right.")]
    [SerializeField] private RectTransform _settingsPanel;
    [Tooltip("The achievement panel. Starts off-screen left.")]
    [SerializeField] private RectTransform _achievementPanel;

    [Header("Animation")]
    [SerializeField] private float _slideDuration = 0.35f;
    [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _canvasRect;
    private float _slideDistance;
    private RectTransform _activePanel;
    private Coroutine _slideRoutine;

    public bool IsTransitioning => _slideRoutine != null;

    private void Awake()
    {
        Instance = this;
        _canvasRect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        _slideDistance = _canvasRect.rect.width;

        _mainMenuPanel.anchoredPosition = Vector2.zero;

        if (_settingsPanel != null)
            _settingsPanel.anchoredPosition = new Vector2(_slideDistance, 0f);

        if (_achievementPanel != null)
            _achievementPanel.anchoredPosition = new Vector2(-_slideDistance, 0f);

        SetPanelActive(_mainMenuPanel, true);
        SetPanelActive(_settingsPanel, false);
        SetPanelActive(_achievementPanel, false);

        _activePanel = _mainMenuPanel;
    }

    /// <summary>Slide from MainMenu to Settings (right to left).</summary>
    public void ShowSettings()
    {
        if (IsTransitioning || _activePanel == _settingsPanel) return;
        _slideRoutine = StartCoroutine(SlideRoutine(_mainMenuPanel, _settingsPanel, -_slideDistance));
    }

    /// <summary>Slide from MainMenu to Achievement (left to right).</summary>
    public void ShowAchievement()
    {
        if (IsTransitioning || _activePanel == _achievementPanel) return;
        _slideRoutine = StartCoroutine(SlideRoutine(_mainMenuPanel, _achievementPanel, _slideDistance));
    }

    /// <summary>Slide back to MainMenu from whichever panel is active.</summary>
    public void ShowMainMenu()
    {
        if (IsTransitioning || _activePanel == _mainMenuPanel) return;

        if (_activePanel == _settingsPanel)
            _slideRoutine = StartCoroutine(SlideRoutine(_settingsPanel, _mainMenuPanel, _slideDistance));
        else if (_activePanel == _achievementPanel)
            _slideRoutine = StartCoroutine(SlideRoutine(_achievementPanel, _mainMenuPanel, -_slideDistance));
    }

    /// <param name="outgoing">Panel sliding away.</param>
    /// <param name="incoming">Panel sliding in.</param>
    /// <param name="mainMenuDelta">How far the main-menu panel moves (sign determines direction).</param>
    private IEnumerator SlideRoutine(RectTransform outgoing, RectTransform incoming, float mainMenuDelta)
    {
        SetPanelActive(incoming, true);

        Vector2 outStart = outgoing.anchoredPosition;
        Vector2 inStart = incoming.anchoredPosition;

        // Outgoing slides by mainMenuDelta; incoming slides by the same amount to land at center
        Vector2 outEnd = outStart + new Vector2(mainMenuDelta, 0f);
        Vector2 inEnd = new Vector2(0f, inStart.y);

        float elapsed = 0f;
        while (elapsed < _slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = _slideCurve.Evaluate(Mathf.Clamp01(elapsed / _slideDuration));

            outgoing.anchoredPosition = Vector2.LerpUnclamped(outStart, outEnd, t);
            incoming.anchoredPosition = Vector2.LerpUnclamped(inStart, inEnd, t);

            yield return null;
        }

        outgoing.anchoredPosition = outEnd;
        incoming.anchoredPosition = inEnd;

        SetPanelActive(outgoing, false);

        _activePanel = incoming;
        _slideRoutine = null;
    }

    private static void SetPanelActive(RectTransform panel, bool active)
    {
        if (panel != null)
            panel.gameObject.SetActive(active);
    }
}
