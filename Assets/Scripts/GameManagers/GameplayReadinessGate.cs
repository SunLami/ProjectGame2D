using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ProjectGame2D.Tests.PlayMode")]

/// <summary>
/// Owns the Loading -> Playing readiness barrier for a gameplay scene. Placed on the
/// scene's _SceneContext. Waits for every registered IGameplayReadinessSource to report
/// ready, then completes the restore exactly once via SceneFlowService.
///
/// Phase 2/3 plug in additional IGameplayReadinessSource implementations (save restore,
/// world restore, quest restore, inventory restore, scene-bound registration, camera/UI
/// binding) without changing this class.
///
/// Editor direct-play (GameBootstrapMode.DevelopmentGameplay) already reaches
/// GameState.Playing before this component runs, so it takes no action in that path.
/// </summary>
public sealed class GameplayReadinessGate : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] _readinessSources;
    [SerializeField] private float _timeoutSeconds = 10f;

    private readonly List<IGameplayReadinessSource> _sources = new();
    private bool _configuredForTests;
    private bool _completed;
    private Coroutine _waitRoutine;

    internal void ConfigureForTests(IReadOnlyList<IGameplayReadinessSource> sources, float timeoutSeconds)
    {
        _configuredForTests = true;
        _sources.Clear();
        if (sources != null)
            _sources.AddRange(sources);
        _timeoutSeconds = timeoutSeconds;
    }

    private void Start()
    {
        if (!_configuredForTests)
            CollectSourcesFromInspector();

        if (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Loading
            || GameSessionManager.Instance == null
            || !GameSessionManager.Instance.HasActiveSession)
        {
            return;
        }

        BeginWaiting();
    }

    private void CollectSourcesFromInspector()
    {
        if (_readinessSources == null)
            return;

        foreach (MonoBehaviour behaviour in _readinessSources)
        {
            if (behaviour is IGameplayReadinessSource source)
                _sources.Add(source);
        }
    }

    private void BeginWaiting()
    {
        if (_sources.Count == 0)
        {
            Fail("No readiness sources configured on GameplayReadinessGate.");
            return;
        }

        foreach (IGameplayReadinessSource source in _sources)
            source.ReadyChanged += HandleReadyChanged;

        EvaluateReadiness();

        if (!_completed)
            _waitRoutine = StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _timeoutSeconds)
        {
            if (_completed)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_completed)
            yield break;

        List<string> notReady = new();
        foreach (IGameplayReadinessSource source in _sources)
        {
            if (!source.IsReady)
                notReady.Add(source.SourceId);
        }

        Fail($"Timed out after {_timeoutSeconds:0.#}s waiting on: {string.Join(", ", notReady)}");
    }

    private void HandleReadyChanged()
    {
        EvaluateReadiness();
    }

    private void EvaluateReadiness()
    {
        if (_completed)
            return;

        foreach (IGameplayReadinessSource source in _sources)
        {
            if (!source.IsReady)
                return;
        }

        Complete();
    }

    private void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        StopWaiting();

        if (SceneFlowService.Instance != null)
            SceneFlowService.Instance.CompleteGameplayRestore();
    }

    private void Fail(string reason)
    {
        if (_completed)
            return;

        _completed = true;
        StopWaiting();

        if (SceneFlowService.Instance != null)
            SceneFlowService.Instance.FailGameplayRestore(reason);
        else
            Debug.LogError($"GameplayReadinessGate failure with no SceneFlowService: {reason}", this);
    }

    private void StopWaiting()
    {
        foreach (IGameplayReadinessSource source in _sources)
            source.ReadyChanged -= HandleReadyChanged;

        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }
    }

    private void OnDestroy()
    {
        StopWaiting();
    }
}
