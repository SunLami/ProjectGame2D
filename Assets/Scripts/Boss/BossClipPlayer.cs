using System;
using UnityEngine;

/// <summary>Clips the boss body can play (D-092, BossAnimationPlan.md).</summary>
public enum BossClipId { Idle, Move, Slam, Raise, Stomp, Fist, Resonance, Recovery, Snap, Burrow, Emerge, Spit, Whirl, Volley, TakeOff, Dive, Flap, Sweep, Slash, Circle, Death }

/// <summary>
/// Plays the full-frame boss animations on the body renderer. Without an action the boss loops Idle (or Move
/// while drifting). A skill starts an action clip; clips with a hold frame run their wind-up up to that frame
/// (stretched to `windupSeconds` when given so it lines up with the telegraph), wait there, and play the rest
/// when the skill calls <see cref="Continue"/> (the strike). Finished actions return to the locomotion loop.
/// Presentation only: gameplay timing stays in BossController/BossSkills.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossClipPlayer : MonoBehaviour
{
    [Serializable]
    public sealed class Clip
    {
        public BossClipId id;
        public Sprite[] frames;
        [Min(1f)] public float fps = 12f;
        public bool loop;
        [Tooltip("Frame the clip waits on until Continue() is called; -1 = plays straight through.")]
        public int holdFrame = -1;
    }

    private enum Mode { Locomotion, Winding, Holding, Releasing }

    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Clip[] _clips;

    private Clip _current;
    private Mode _mode = Mode.Locomotion;
    private float _frame;
    private float _windupFps;
    private bool _continueQueued;
    private bool _moving;

    public bool IsActionPlaying => _mode != Mode.Locomotion;
    public bool IsHolding => _mode == Mode.Holding;
    public bool HasClips => _clips != null && _clips.Length > 0;

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponentInChildren<SpriteRenderer>();
        StartLocomotion();
    }

    /// <summary>Idle vs Move loop while no action clip is playing.</summary>
    public void SetMoving(bool moving)
    {
        if (_moving == moving)
            return;

        _moving = moving;
        if (_mode == Mode.Locomotion)
            StartLocomotion();
    }

    /// <summary>Starts an action clip. `windupSeconds` &gt; 0 stretches the part before the hold frame to that length.</summary>
    public void Play(BossClipId id, float windupSeconds = 0f)
    {
        Clip clip = Find(id);
        if (clip == null || clip.frames == null || clip.frames.Length == 0)
            return;

        _current = clip;
        _frame = 0f;
        _continueQueued = false;
        bool hasHold = clip.holdFrame >= 0 && clip.holdFrame < clip.frames.Length;
        _windupFps = hasHold && windupSeconds > 0.05f ? (clip.holdFrame + 1) / windupSeconds : clip.fps;
        _mode = hasHold ? Mode.Winding : Mode.Releasing;
        Show(0);
    }

    /// <summary>Releases a hold (or skips it if the wind-up has not reached it yet).</summary>
    public void Continue()
    {
        if (_mode == Mode.Holding)
        {
            _mode = Mode.Releasing;
            _frame = _current.holdFrame + 1;
            Show(Mathf.Min((int)_frame, _current.frames.Length - 1));
        }
        else if (_mode == Mode.Winding)
        {
            _continueQueued = true;
        }
    }

    /// <summary>Drops the running action and goes back to the locomotion loop.</summary>
    public void Stop()
    {
        _mode = Mode.Locomotion;
        StartLocomotion();
    }

    private void Update()
    {
        if (_current == null || _renderer == null)
            return;

        switch (_mode)
        {
            case Mode.Locomotion:
                Advance(_current.fps, true);
                break;
            case Mode.Winding:
                _frame += _windupFps * Time.deltaTime;
                if (_frame >= _current.holdFrame)
                {
                    _frame = _current.holdFrame;
                    Show(_current.holdFrame);
                    if (_continueQueued)
                    {
                        _continueQueued = false;
                        _mode = Mode.Releasing;
                        _frame = _current.holdFrame + 1;
                    }
                    else
                    {
                        _mode = Mode.Holding;
                    }
                }
                else
                {
                    Show((int)_frame);
                }

                break;
            case Mode.Holding:
                break;
            case Mode.Releasing:
                _frame += _current.fps * Time.deltaTime;
                if ((int)_frame >= _current.frames.Length)
                {
                    if (_current.loop)
                        _frame = 0f;
                    else
                    {
                        _mode = Mode.Locomotion;
                        StartLocomotion();
                        break;
                    }
                }

                Show((int)_frame);
                break;
        }
    }

    private void Advance(float fps, bool loop)
    {
        _frame += fps * Time.deltaTime;
        if ((int)_frame >= _current.frames.Length)
            _frame = loop ? _frame % _current.frames.Length : _current.frames.Length - 1;
        Show((int)_frame);
    }

    private void StartLocomotion()
    {
        Clip clip = Find(_moving ? BossClipId.Move : BossClipId.Idle) ?? Find(BossClipId.Idle);
        if (clip == null || clip.frames == null || clip.frames.Length == 0)
            return;

        _current = clip;
        _frame = 0f;
        Show(0);
    }

    private Clip Find(BossClipId id)
    {
        if (_clips == null)
            return null;

        foreach (Clip clip in _clips)
        {
            if (clip != null && clip.id == id)
                return clip;
        }

        return null;
    }

    private void Show(int index)
    {
        if (_renderer == null || _current == null || _current.frames == null || _current.frames.Length == 0)
            return;

        Sprite sprite = _current.frames[Mathf.Clamp(index, 0, _current.frames.Length - 1)];
        if (sprite != null)
            _renderer.sprite = sprite;
    }
}
