using UnityEngine;

/// <summary>Scene-level ambience: one or two looping beds (through the SFX channel, so the Settings SFX volume applies) plus
/// the fallback footstep surface used when the tile under the player carries no clip. Put one per scene (or arena).</summary>
public sealed class SceneAmbience : MonoBehaviour
{
    [SerializeField] private string _bedId = SfxIds.AmbForest;
    [SerializeField] private string _secondBedId = "";
    [SerializeField, Range(0f, 1f)] private float _bedLevel = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _secondBedLevel = 0.4f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 2f;
    [Tooltip("Footstep SFX id used when the walked tile has no audio clip (empty = keep silent there).")]
    [SerializeField] private string _fallbackFootstepId = "";

    private SfxLoopHandle _bed;
    private SfxLoopHandle _second;
    private string _previousFootstepId;

    public string BedId => _bedId;
    public SfxLoopHandle Bed => _bed;

    public void Configure(string bedId, string secondBedId, float bedLevel, float secondBedLevel, string fallbackFootstepId)
    {
        _bedId = bedId;
        _secondBedId = secondBedId;
        _bedLevel = bedLevel;
        _secondBedLevel = secondBedLevel;
        _fallbackFootstepId = fallbackFootstepId;
    }

    private SfxSceneProfile _profile;

    private void OnEnable()
    {
        // The scene's SfxSceneProfile asset (Resources/Audio/SfxScenes) overrides the values serialized here.
        _profile = SfxSceneProfile.Find(gameObject.scene.name);
        if (_profile != null)
        {
            _bedId = _profile.bedId;
            _secondBedId = _profile.secondBedId;
            _bedLevel = _profile.bedLevel;
            _secondBedLevel = _profile.secondBedLevel;
            _fadeInSeconds = _profile.fadeInSeconds;
            _fallbackFootstepId = _profile.fallbackFootstepId;
            SfxSceneMix.Current = _profile;
        }

        if (!string.IsNullOrEmpty(_bedId))
            _bed = SoundFXManager.StartLoop(_bedId, _bedLevel, 1f, _fadeInSeconds);
        if (!string.IsNullOrEmpty(_secondBedId))
            _second = SoundFXManager.StartLoop(_secondBedId, _secondBedLevel, 1f, _fadeInSeconds);
        _previousFootstepId = SoundFXManager.FallbackFootstepId;
        if (!string.IsNullOrEmpty(_fallbackFootstepId))
            SoundFXManager.FallbackFootstepId = _fallbackFootstepId;
    }

    private void OnDisable()
    {
        if (_profile != null && SfxSceneMix.Current == _profile)
            SfxSceneMix.Current = null;
        _bed?.Stop(1f);
        _second?.Stop(1f);
        _bed = _second = null;
        if (!string.IsNullOrEmpty(_fallbackFootstepId) && SoundFXManager.FallbackFootstepId == _fallbackFootstepId)
            SoundFXManager.FallbackFootstepId = _previousFootstepId;
    }
}
