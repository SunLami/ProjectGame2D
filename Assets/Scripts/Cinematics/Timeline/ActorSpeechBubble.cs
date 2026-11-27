using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// A world-space speech bubble that floats above this actor's head. Opened by
/// ActorDialogueMixerBehaviour when its Timeline clip becomes active; closing it (on player
/// confirm input) resumes the PlayableDirector that paused for it.
///
/// This component's own GameObject must stay active at all times -- put it on the actor itself,
/// not on the toggled bubble visuals (_root). Show() is called synchronously from inside a
/// PlayableGraph callback (ActorDialogueMixerBehaviour.ProcessFrame); activating _root and
/// starting a coroutine on it in that same call was found to silently fail
/// ("Coroutine couldn't be started because the game object is inactive") because the graph
/// callback runs outside Unity's normal frame in which SetActive's effect on
/// activeInHierarchy has fully propagated. Keeping this script on an always-active GameObject
/// sidesteps that race entirely.
/// </summary>
public sealed class ActorSpeechBubble : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private RectTransform _background;
    [SerializeField] private TMP_Text _speakerText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private GameObject _continueIndicator;
    [SerializeField, Min(1f)] private float _charactersPerSecond = 42f;
    [SerializeField, Range(0.5f, 1f)] private float _punchScale = 0.9f;
    [SerializeField, Min(0.01f)] private float _punchDuration = 0.14f;

    [Header("Fit to text")]
    [Tooltip("Resize _root's height to the wrapped body text on every line. _root's pivot should sit at the tail tip (bottom) so the tail stays on the actor's head while the bubble grows upward. Width stays fixed and acts as the maximum line width.")]
    [SerializeField] private bool _fitHeightToText = true;
    [Tooltip("Smallest bubble height, in _root's local units. Keep it >= the background sprite's top + bottom slice borders so the 9-slice never overlaps.")]
    [SerializeField, Min(0f)] private float _minHeight;

    // Guarantees at most one bubble is ever open at a time. Normally the previous line's
    // Close() (fired by the player's confirm input) always runs before the next clip's Show(),
    // but a confirm press landing the same frame a new clip activates can still let Show() beat
    // that Close() to the punch, leaving two bubbles open at once. Forcing the previous one shut
    // before opening a new one closes that race outright.
    private static ActorSpeechBubble s_currentlyOpen;

    /// <summary>Force-closes whichever bubble is currently open, if any, without waiting for
    /// player input -- used by a scene-skip control so a mid-line skip doesn't leave a stale
    /// bubble on screen or the director paused. Runs the exact same resume-and-pop-state path
    /// as a normal player-confirmed close.</summary>
    public static void CloseCurrentIfOpen() => s_currentlyOpen?.Close();

    private PlayableDirector _director;
    private Coroutine _typewriter;
    private Coroutine _punch;
    private bool _isRevealing;
    private bool _isOpen;

    private void Awake()
    {
        // _root's position is authored directly in the Editor (drag it to sit above this actor's
        // head, tail included) and used as-is here -- Show() no longer applies any offset on top
        // of it. It used to auto-shift by a tail-offset constant depending on flip state, but
        // that meant whatever was actually saved already included that shift (since tuning it
        // meant looking at _root while it was active, i.e. already shifted), and Show() would
        // then shift it again on top, landing noticeably off from what was tuned in the Editor.
        if (_root != null)
            _root.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        bool confirm = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        confirm |= Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (confirm)
            Advance();
    }

    public void Show(string speakerName, string text, PlayableDirector director, bool flipHorizontal)
    {
        if (s_currentlyOpen != null && s_currentlyOpen != this)
            s_currentlyOpen.Close();
        s_currentlyOpen = this;

        _director = director;
        _isOpen = true;

        if (_root != null)
            _root.SetActive(true);
        // Only the background art mirrors -- the tail is the only asymmetric part of the sprite,
        // so this flips which side it points off to without also mirroring (and un-reading) the
        // speaker/body text, which stay in their own unflipped RectTransforms.
        if (_background != null)
        {
            Vector3 scale = _background.localScale;
            scale.x = flipHorizontal ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _background.localScale = scale;
        }
        if (_speakerText != null)
            _speakerText.text = speakerName;
        // Set the full text up front as a fallback: this component's own GameObject must stay
        // active at all times (see class remarks) specifically so StartCoroutine below never
        // races a same-frame SetActive from a PlayableGraph callback, but showing the line's
        // text should never depend on that coroutine actually starting.
        if (_bodyText != null)
            _bodyText.text = text ?? string.Empty;
        FitHeightToText(text ?? string.Empty);
        if (_continueIndicator != null)
            _continueIndicator.SetActive(false);

        GameStateManager.Instance?.PushState(GameState.Dialogue);

        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = StartCoroutine(RevealText(text ?? string.Empty));
        Punch();
    }

    /// <summary>
    /// Grows or shrinks the bubble vertically so the wrapped body text never clips. The body
    /// RectTransform stretches vertically inside _root with fixed offsets, so everything that is
    /// not body text (padding, speaker row, tail) is simply _root's height minus the body's height;
    /// that "chrome" stays constant and only the body share changes. The background is a 9-sliced
    /// sprite, so only its straight middle band stretches and the corners and tail keep their art.
    /// </summary>
    private void FitHeightToText(string text)
    {
        if (!_fitHeightToText || _bodyText == null || _root == null)
            return;
        if (!(_root.transform is RectTransform bubble))
            return;

        RectTransform body = _bodyText.rectTransform;
        float chrome = bubble.rect.height - body.rect.height;
        float needed = _bodyText.GetPreferredValues(text, body.rect.width, 0f).y;
        float height = Mathf.Max(_minHeight, chrome + needed);
        bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private void Advance()
    {
        if (_isRevealing)
        {
            FinishReveal();
            return;
        }
        Close();
    }

    private void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        if (s_currentlyOpen == this)
            s_currentlyOpen = null;
        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = null;
        _isRevealing = false;
        if (_root != null)
            _root.SetActive(false);

        GameStateManager.Instance?.ReturnToPreviousState();

        PlayableDirector director = _director;
        _director = null;
        director?.Play();
    }

    private IEnumerator RevealText(string text)
    {
        _isRevealing = true;
        _bodyText.text = text;
        _bodyText.ForceMeshUpdate();
        int count = _bodyText.textInfo.characterCount;
        _bodyText.maxVisibleCharacters = 0;
        float delay = 1f / _charactersPerSecond;
        for (int visible = 1; visible <= count; visible++)
        {
            _bodyText.maxVisibleCharacters = visible;
            yield return new WaitForSecondsRealtime(delay);
        }
        FinishReveal();
    }

    private void FinishReveal()
    {
        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = null;
        _isRevealing = false;
        if (_bodyText != null)
            _bodyText.maxVisibleCharacters = int.MaxValue;
        if (_continueIndicator != null)
            _continueIndicator.SetActive(true);
    }

    private void Punch()
    {
        if (_root == null)
            return;
        if (_punch != null)
            StopCoroutine(_punch);
        _punch = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _punchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _punchDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float scale = Mathf.Lerp(_punchScale, 1f, eased);
            _root.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        _root.transform.localScale = Vector3.one;
        _punch = null;
    }
}
