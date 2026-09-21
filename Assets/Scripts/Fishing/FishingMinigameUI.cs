using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FishingMinigameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _waitingPanel;
    [SerializeField] private GameObject _bitePrompt;
    [SerializeField] private GameObject _minigamePanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text _waitingText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _resultText;

    [Header("Mini-game")]
    [SerializeField] private RectTransform _movementTrack;
    [SerializeField] private RectTransform _fishIcon;
    [SerializeField] private RectTransform _catchZone;
    [SerializeField] private Image _fishImage;
    [SerializeField] private Slider _progressSlider;

    public void ShowWaiting()
    {
        gameObject.SetActive(true);
        SetOnly(_waitingPanel);
        if (_waitingText != null) _waitingText.text = "Waiting for a bite...";
    }

    public void ShowBitePrompt()
    {
        gameObject.SetActive(true);
        SetOnly(_bitePrompt);
    }

    public void ShowMinigame(FishDefinitionSO fish, float timeLimit)
    {
        gameObject.SetActive(true);
        SetOnly(_minigamePanel);
        if (_fishImage != null)
        {
            _fishImage.sprite = fish != null ? fish.icon : null;
            _fishImage.enabled = _fishImage.sprite != null;
        }
        SetProgress(0f);
        SetTime(timeLimit);
    }

    public void ShowResult(string message)
    {
        gameObject.SetActive(true);
        SetOnly(_resultPanel);
        if (_resultText != null) _resultText.text = message;
    }

    public void HideAll()
    {
        SetOnly(null);
        gameObject.SetActive(false);
    }

    public void SetProgress(float normalized)
    {
        if (_progressSlider != null) _progressSlider.value = Mathf.Clamp01(normalized);
    }

    public void SetTime(float seconds)
    {
        // TMP's numeric overload avoids allocating a new string every minigame frame.
        if (_timerText != null)
            _timerText.SetText("{0:0}s", Mathf.CeilToInt(Mathf.Max(0f, seconds)));
    }

    public void SetFishPosition(float normalized) => SetTrackPosition(_fishIcon, normalized);

    public void SetCatchZonePosition(float normalized, float normalizedSize)
    {
        SetTrackPosition(_catchZone, normalized);
        if (_catchZone != null && _movementTrack != null)
        {
            Vector2 size = _catchZone.sizeDelta;
            size.y = _movementTrack.rect.height * Mathf.Clamp01(normalizedSize);
            _catchZone.sizeDelta = size;
        }
    }

    private void SetTrackPosition(RectTransform target, float normalized)
    {
        if (target == null || _movementTrack == null)
            return;

        float targetHalfHeight = target.rect.height * 0.5f;
        float halfRange = Mathf.Max(0f, (_movementTrack.rect.height * 0.5f) - targetHalfHeight);
        Vector2 position = target.anchoredPosition;
        position.y = Mathf.Lerp(-halfRange, halfRange, Mathf.Clamp01(normalized));
        target.anchoredPosition = position;
    }

    private void SetOnly(GameObject visible)
    {
        if (_waitingPanel != null) _waitingPanel.SetActive(_waitingPanel == visible);
        if (_bitePrompt != null) _bitePrompt.SetActive(_bitePrompt == visible);
        if (_minigamePanel != null) _minigamePanel.SetActive(_minigamePanel == visible);
        if (_resultPanel != null) _resultPanel.SetActive(_resultPanel == visible);
    }
}
