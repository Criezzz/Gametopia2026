using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays "P1" or "P2" above the player in Arena mode.
/// Attach to a child of the Player (with Canvas World Space + TextMeshProUGUI).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class PlayerTagUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _labelText;
    [SerializeField] private float _heightOffset = 1.2f;

    private PlayerController _playerController;
    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    private void Awake()
    {
        _playerController = GetComponentInParent<PlayerController>();
        _canvas = GetComponent<Canvas>();
        _raycaster = GetComponent<GraphicRaycaster>();

        // This tag is display-only and should never consume UI pointer events.
        if (_raycaster != null)
            _raycaster.enabled = false;
        if (_labelText != null)
            _labelText.raycastTarget = false;
    }

    private void Start()
    {
        bool isArena = GameManager.IsArenaMode;

        if (!isArena)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_playerController != null && _labelText != null)
        {
            int idx = _playerController.PlayerIndex;
            _labelText.text = idx == 0 ? "P1" : "P2";
        }

        transform.localPosition = new Vector3(0f, _heightOffset, 0f);
    }
}
