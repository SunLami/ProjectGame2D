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

    // Guarantees at most one bubble is ever open at a time. Normally the previous line's
    // Close() (fired by the player's confirm input) always runs before the next clip's Show(),
    // but a confirm press landing the same frame a new clip activates can still let Show() beat
    // that Close() to the punch, leaving two bubbles open at once. Forcing the previous one shut
    // before opening a new one closes that race outright.
    private static ActorSpeechBubble s_currentlyOpen;

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
        if (_continueIndicator != null)
            _continueIndicator.SetActive(false);

        GameStateManager.Instance?.PushState(GameState.Dialogue);

        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = StartCoroutine(RevealText(text ?? string.Empty));
        Punch();
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
