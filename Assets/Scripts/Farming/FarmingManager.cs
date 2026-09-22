using System.Collections.Generic;
using UnityEngine;

public sealed class FarmingManager : MonoBehaviour
{
    [SerializeField] private FarmingCatalog _catalog;
    [SerializeField] private FarmPlot[] _plots;
    private readonly Dictionary<string, FarmPlot> _byId = new();

    public static FarmingManager Instance { get; private set; }
    public FarmingCatalog Catalog => _catalog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one FarmingManager may exist in a gameplay scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
        RebuildRegistry();
    }

    public FarmingSaveData ToSaveData()
    {
        var data = new FarmingSaveData();
        foreach (FarmPlot plot in _byId.Values)
            if (plot != null && plot.HasCrop) data.plots.Add(plot.CaptureState());
        return data;
    }

    public void RestoreState(FarmingSaveData data, List<string> missingPlotIds = null, List<string> missingCropIds = null)
    {
        foreach (FarmPlot plot in _byId.Values) plot.RestoreEmpty();
        if (data?.plots == null) return;

        foreach (FarmPlotSaveData state in data.plots)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.plotId)) continue;
            if (!_byId.TryGetValue(state.plotId, out FarmPlot plot))
            {
                missingPlotIds?.Add(state.plotId);
                continue;
            }
            if (_catalog == null || !_catalog.TryResolve(state.cropId, out CropDefinition crop))
            {
                missingCropIds?.Add(state.cropId);
                continue;
            }
            plot.RestoreCrop(crop, state.plantedAtUtcTicks);
        }
    }

    internal void ConfigureForTests(FarmingCatalog catalog, params FarmPlot[] plots)
    {
        _catalog = catalog;
        _plots = plots;
        RebuildRegistry();
    }

    private void RebuildRegistry()
    {
        _byId.Clear();
        if (_plots == null) return;
        foreach (FarmPlot plot in _plots)
        {
            if (plot == null || string.IsNullOrWhiteSpace(plot.PlotId)) continue;
            if (_byId.ContainsKey(plot.PlotId))
            {
                Debug.LogError($"Duplicate farm plot id '{plot.PlotId}'.", plot);
                continue;
            }
            _byId.Add(plot.PlotId, plot);
            plot.Bind(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
