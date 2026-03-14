using System.Collections;
using UnityEngine;
using TMPro;

/// Displays tutorial instruction text with optional typewriter effect.
public class TutorialUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _textBoxPanel;
    [SerializeField] private TextMeshProUGUI _instructionText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Typewriter")]
    [SerializeField] private float _charsPerSecond = 40f;

    private Coroutine _typewriterCoroutine;

    private void Awake()
    {
        if (_canvasGroup == null && _textBoxPanel != null)
            _canvasGroup = _textBoxPanel.GetComponent<CanvasGroup>();

        HideMessage();
    }

    public void ShowMessage(string text)
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (_textBoxPanel != null) _textBoxPanel.SetActive(true);
        SetVisible(true);

        if (_instructionText != null)
            _instructionText.text = text;
    }

    /// Shows message with a typewriter reveal. Invokes onComplete when done.
    public void ShowMessageTypewriter(string text, System.Action onComplete = null)
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (_textBoxPanel != null) _textBoxPanel.SetActive(true);
        SetVisible(true);

        _typewriterCoroutine = StartCoroutine(TypewriterRoutine(text, onComplete));
    }

    public void HideMessage()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        SetVisible(false);
        if (_textBoxPanel != null) _textBoxPanel.SetActive(false);
    }

    private IEnumerator TypewriterRoutine(string fullText, System.Action onComplete)
    {
        if (_instructionText == null) yield break;

        _instructionText.text = "";
        for (int i = 0; i < fullText.Length; i++)
        {
            _instructionText.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(1f / _charsPerSecond);
        }

        _typewriterCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
