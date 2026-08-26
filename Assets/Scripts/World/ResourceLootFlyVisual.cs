using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceLootFlyVisual
{
    public static IEnumerator Play(Vector3 origin, Transform player, IReadOnlyList<InventoryItemGrant> grants)
    {
        var visuals = new List<Transform>();
        for (int i = 0; i < grants.Count; i++)
        {
            GameObject visual = new($"LootVisual_{grants[i].Item.itemId}");
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = grants[i].Item.icon;
            renderer.sortingOrder = 200;
            renderer.color = renderer.sprite != null ? Color.white : FallbackColor(i);
            visual.transform.localScale = renderer.sprite != null ? Vector3.one * 1.2f : Vector3.one * 0.36f;
            visual.transform.position = origin;
            visuals.Add(visual.transform);
        }

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.3f);
            for (int i = 0; i < visuals.Count; i++)
            {
                float angle = visuals.Count == 1 ? 0f : Mathf.Lerp(-55f, 55f, i / (float)(visuals.Count - 1));
                Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.down * (0.45f + 0.08f * i);
                visuals[i].position = Vector3.Lerp(origin, origin + offset, t);
            }
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.12f);
        var starts = new Vector3[visuals.Count];
        for (int i = 0; i < visuals.Count; i++) starts[i] = visuals[i].position;

        elapsed = 0f;
        while (elapsed < 0.45f && player != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.45f));
            for (int i = 0; i < visuals.Count; i++) visuals[i].position = Vector3.Lerp(starts[i], player.position, t);
            yield return null;
        }

        foreach (Transform visual in visuals)
            if (visual != null) Object.Destroy(visual.gameObject);
    }

    private static Color FallbackColor(int index) => (index % 3) switch
    {
        0 => new Color(0.78f, 0.38f, 0.18f),
        1 => new Color(0.55f, 0.33f, 0.16f),
        _ => new Color(0.25f, 0.75f, 0.3f)
    };
}
