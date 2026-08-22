using System;
using UnityEngine;

/// <summary>
/// Phase 1 readiness source: reports ready once every explicitly assigned scene
/// dependency exists. This confirms scene runtime dependencies are bound; it does
/// not perform save/world restoration.
/// </summary>
public sealed class SceneDependencyReadinessSource : MonoBehaviour, IGameplayReadinessSource
{
    [SerializeField] private string _sourceId = "SceneDependency";
    [SerializeField] private UnityEngine.Object[] _requiredDependencies;

    public string SourceId => _sourceId;
    public bool IsReady { get; private set; }

    public event Action ReadyChanged;

    private void Start()
    {
        Evaluate();
    }

    private void Evaluate()
    {
        bool ready = true;
        if (_requiredDependencies != null)
        {
            foreach (UnityEngine.Object dependency in _requiredDependencies)
            {
                if (dependency == null)
                {
                    ready = false;
                    break;
                }
            }
        }

        if (ready == IsReady)
            return;

        IsReady = ready;
        ReadyChanged?.Invoke();
    }
}
