using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Summon ritual of a boss arena (D-091): the guardian statues in the corners charge their eyes, then
/// fire laser beams at the summoning statue, which blows up. Fully procedural (no art assets besides the
/// statues): jittering line renderers + radial glow sprites. Presentation only; the arena flow stays in
/// <see cref="BossArenaController"/>.
/// </summary>
public static class SummonRitualFX
{
    private static readonly Color DefaultCore = new Color(0.9f, 1f, 0.85f, 1f);
    private static readonly Color DefaultOuter = new Color(0.3f, 1f, 0.45f, 0.8f);
    private static Color Core = DefaultCore;
    private static Color Outer = DefaultOuter;
    private const int BeamPoints = 14;

    private static Sprite _glow;
    private static Material _lineMaterial;

    private sealed class Beam
    {
        public Transform Emitter;
        public SpriteRenderer Eye;
        public LineRenderer Outer;
        public LineRenderer Core;
    }

    /// <summary>Runs the ritual. `onImpact` fires the moment the beams hit and the statue should explode;
    /// the coroutine then plays the explosion burst for about half a second and ends.</summary>
    public static IEnumerator Play(Transform[] emitters, Vector3 target, float chargeSeconds, float beamSeconds, Action onImpact,
        Color? beamColor = null)
    {
        // one ritual runs at a time; the colour is per arena (green for Earth, aqua for Water)
        Outer = beamColor ?? DefaultOuter;
        Core = beamColor.HasValue ? Color.Lerp(beamColor.Value, Color.white, 0.75f) : DefaultCore;

        var root = new GameObject("SummonRitualFX");
        var beams = new List<Beam>();
        if (emitters != null)
        {
            foreach (Transform emitter in emitters)
            {
                if (emitter != null)
                    beams.Add(CreateBeam(root.transform, emitter));
            }
        }

        SpriteRenderer targetGlow = CreateGlow("TargetGlow", root.transform, target, Outer, 22);
        targetGlow.transform.localScale = Vector3.zero;

        // 1) charge: the eyes flare up and flicker
        for (float t = 0f; t < chargeSeconds; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / chargeSeconds);
            float flicker = 0.85f + 0.15f * Mathf.Sin(t * 45f);
            foreach (Beam beam in beams)
            {
                beam.Eye.transform.position = beam.Emitter.position;
                beam.Eye.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.95f, k * k) * flicker;
            }

            if (Mathf.Repeat(t, 0.25f) < Time.deltaTime)
                SkillScreenFX.Shake(0.03f + 0.05f * k, 0.25f);
            yield return null;
        }

        // 2) beams reach the statue and hold
        foreach (Beam beam in beams)
        {
            beam.Outer.enabled = true;
            beam.Core.enabled = true;
        }

        float nextJitter = 0f;
        var jitter = new float[BeamPoints];
        for (float e = 0f; e < beamSeconds; e += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(e / beamSeconds);
            float extend = Mathf.Clamp01(e / 0.3f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(e * 38f);
            if (e >= nextJitter)
            {
                nextJitter = e + 0.05f;
                for (int i = 0; i < jitter.Length; i++)
                    jitter[i] = UnityEngine.Random.Range(-1f, 1f);
            }

            foreach (Beam beam in beams)
            {
                Vector3 start = beam.Emitter.position;
                Vector3 direction = (target - start);
                Vector3 normal = new Vector3(-direction.y, direction.x, 0f).normalized;
                beam.Eye.transform.position = start;
                beam.Eye.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(e * 30f));
                for (int i = 0; i < BeamPoints; i++)
                {
                    float f = (float)i / (BeamPoints - 1);
                    float envelope = Mathf.Sin(f * Mathf.PI) * 0.12f * (0.4f + progress);
                    Vector3 point = start + direction * (f * extend) + normal * (jitter[i] * envelope);
                    beam.Outer.SetPosition(i, point);
                    beam.Core.SetPosition(i, point);
                }

                beam.Outer.widthMultiplier = (0.2f + 0.2f * progress) * pulse;
                beam.Core.widthMultiplier = (0.07f + 0.07f * progress) * pulse;
            }

            targetGlow.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 4f, progress * progress) * pulse * Mathf.Clamp01(e / 0.3f);
            if (Mathf.Repeat(e, 0.2f) < Time.deltaTime)
                SkillScreenFX.Shake(0.08f + 0.2f * progress, 0.25f);
            yield return null;
        }

        // 3) impact: beams cut, the statue blows up
        foreach (Beam beam in beams)
        {
            beam.Outer.enabled = false;
            beam.Core.enabled = false;
        }

        SkillScreenFX.Flash(new Color(0.8f, 1f, 0.8f), 0.55f, 0.6f);
        SkillScreenFX.Shake(0.35f, 0.7f);
        onImpact?.Invoke();

        var sparks = new List<Transform>();
        var velocities = new List<Vector3>();
        for (int i = 0; i < 28; i++)
        {
            SpriteRenderer spark = CreateGlow("Spark", root.transform, target, i % 2 == 0 ? Core : Outer, 23);
            float scale = UnityEngine.Random.Range(0.15f, 0.45f);
            spark.transform.localScale = Vector3.one * scale;
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            sparks.Add(spark.transform);
            velocities.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f, 0f) * UnityEngine.Random.Range(3f, 9f));
        }

        const float burstSeconds = 0.55f;
        for (float t = 0f; t < burstSeconds; t += Time.deltaTime)
        {
            float k = t / burstSeconds;
            targetGlow.transform.localScale = Vector3.one * Mathf.Lerp(4f, 7f, k);
            Color glowColor = targetGlow.color;
            glowColor.a = 0.8f * (1f - k);
            targetGlow.color = glowColor;
            for (int i = 0; i < sparks.Count; i++)
            {
                sparks[i].position += velocities[i] * Time.deltaTime;
                velocities[i] *= 1f - 2.5f * Time.deltaTime;
                sparks[i].localScale *= 1f - 1.6f * Time.deltaTime;
            }

            foreach (Beam beam in beams)
            {
                Color eyeColor = beam.Eye.color;
                eyeColor.a = 1f - k;
                beam.Eye.color = eyeColor;
            }

            yield return null;
        }

        UnityEngine.Object.Destroy(root);
    }

    private static Beam CreateBeam(Transform parent, Transform emitter)
    {
        var beam = new Beam { Emitter = emitter };
        beam.Eye = CreateGlow("EyeGlow", parent, emitter.position, Outer, 24);
        beam.Eye.transform.localScale = Vector3.one * 0.1f;
        beam.Outer = CreateLine("BeamOuter", parent, Outer, 20);
        beam.Core = CreateLine("BeamCore", parent, Core, 21);
        beam.Outer.enabled = false;
        beam.Core.enabled = false;
        return beam;
    }

    private static LineRenderer CreateLine(string name, Transform parent, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        if (_lineMaterial == null)
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial = _lineMaterial;
        line.positionCount = BeamPoints;
        line.useWorldSpace = true;
        line.startColor = color;
        line.endColor = color;
        line.sortingLayerName = "Default";
        line.sortingOrder = order;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        return line;
    }

    private static SpriteRenderer CreateGlow(string name, Transform parent, Vector3 position, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GlowSprite();
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite GlowSprite()
    {
        if (_glow != null)
            return _glow;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }

        texture.Apply();
        _glow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _glow;
    }
}
