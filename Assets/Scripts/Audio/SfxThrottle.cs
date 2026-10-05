using System.Collections.Generic;

/// <summary>
/// Pure bookkeeping for per-ID SFX limits (no Unity audio types, EditMode-testable): a minimum interval between two
/// starts of the same ID and a cap on simultaneously audible instances of it. A rain of meteors or feathers would
/// otherwise stack dozens of identical one-shots into noise. Time is passed in so tests control it.
/// </summary>
public sealed class SfxThrottle
{
    private readonly Dictionary<string, float> _lastStart = new Dictionary<string, float>();
    private readonly Dictionary<string, List<float>> _endTimes = new Dictionary<string, List<float>>();

    /// <summary>Registers a start if allowed. `minInterval` &lt;= 0 and `maxVoices` &lt;= 0 mean "no limit".</summary>
    public bool TryAcquire(string id, float now, float minInterval, int maxVoices, float clipSeconds)
    {
        if (minInterval > 0f && _lastStart.TryGetValue(id, out float last) && now - last < minInterval)
            return false;

        if (maxVoices > 0)
        {
            if (!_endTimes.TryGetValue(id, out List<float> ends))
            {
                ends = new List<float>(maxVoices);
                _endTimes[id] = ends;
            }

            ends.RemoveAll(end => end <= now);
            if (ends.Count >= maxVoices)
                return false;

            ends.Add(now + clipSeconds);
        }

        _lastStart[id] = now;
        return true;
    }

    public int ActiveVoices(string id, float now)
    {
        if (!_endTimes.TryGetValue(id, out List<float> ends))
            return 0;

        ends.RemoveAll(end => end <= now);
        return ends.Count;
    }

    public void Clear()
    {
        _lastStart.Clear();
        _endTimes.Clear();
    }
}
