using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Hit feedback shared by the three boss fights (BossCombatAudit.md section 4B): floating damage numbers and an element-coloured spark where the
/// boss is hit, a short hit-stop, and for the player a red/blink "I was hit" flash with a screen tint and shake. Pure presentation: it never
/// changes damage, health or any save data. Art: Resources/VFX/Skills/Combat (Pixellab, D-081).
/// </summary>
public static class CombatFeedback
{
    public enum Element { Earth, Water, Wind }

    public static Element ElementOf(string bossId)
    {
        if (!string.IsNullOrEmpty(bossId))
        {
            if (bossId.Contains("water"))
                return Element.Water;
            if (bossId.Contains("wind"))
                return Element.Wind;
        }

        return Element.Earth;
    }

    public static string SparkName(Element element) => "Combat/HitSpark_" + element;
    public static string AuraName(Element element) => "Combat/CastAura_" + element;

    /// <summary>Boss took `damage` (after its own multipliers); `weak` = it was inside a vulnerable window.</summary>
    public static void BossHit(Vector2 bossPosition, Element element, float damage, bool weak)
    {
        Vector2 point = bossPosition + Random.insideUnitCircle * 1.4f;
        BossVfx.Spawn(SparkName(element), point, weak ? 1.5f : 1.1f, 20f, false, 0.6f, 12);
        DamageNumber.Spawn(point + Vector2.up * 0.8f, Mathf.RoundToInt(damage).ToString(),
            weak ? new Color(1f, 0.82f, 0.25f) : Color.white, weak ? 1.25f : 1f);
        SkillScreenFX.HitStopAndSlowMo(weak ? 0.055f : 0.035f, 1f, 0.01f);
    }

    /// <summary>The player was hurt by a boss: element spark on them.</summary>
    public static void PlayerHitSpark(Vector2 playerPosition, Element element)
    {
        BossVfx.Spawn(SparkName(element), playerPosition + Vector2.up * 0.4f, 0.9f, 20f, false, 0.6f, 12);
    }

    /// <summary>The player took damage from anything: blink + red edge + a nudge of shake scaled by the hit.</summary>
    public static void PlayerHurt(Player player, float damage, float invulnerableSeconds)
    {
        if (player == null)
            return;

        HurtBlink blink = player.GetComponent<HurtBlink>();
        if (blink == null)
            blink = player.gameObject.AddComponent<HurtBlink>();
        blink.Begin(invulnerableSeconds);

        float strength = Mathf.Clamp01(damage / 30f);
        SkillScreenFX.Flash(new Color(0.85f, 0.08f, 0.06f), 0.16f + 0.2f * strength, 0.28f + 0.2f * strength);
        SkillScreenFX.Shake(0.07f + 0.12f * strength, 0.16f + 0.1f * strength);
        if (strength > 0.45f)
            SkillScreenFX.HitStopAndSlowMo(0.05f, 1f, 0.01f);
    }

    /// <summary>Red flash, then blinking while the post-hit invulnerability lasts.</summary>
    private sealed class HurtBlink : MonoBehaviour
    {
        private SpriteRenderer[] _renderers;
        private Color[] _original;
        private float _end;
        private float _flashEnd;
        private Coroutine _routine;

        public void Begin(float seconds)
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_original == null || _original.Length != _renderers.Length)
            {
                _original = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                    _original[i] = _renderers[i].color;
            }

            _flashEnd = Time.time + 0.12f;
            _end = Time.time + Mathf.Max(0.12f, seconds);
            if (_routine == null)
                _routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (Time.time < _end)
            {
                bool flash = Time.time < _flashEnd;
                bool hidden = !flash && Mathf.FloorToInt(Time.time * 18f) % 2 == 0;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null)
                        continue;
                    Color c = flash ? Color.Lerp(_original[i], new Color(1f, 0.25f, 0.22f), 0.85f) : _original[i];
                    c.a = hidden ? _original[i].a * 0.35f : _original[i].a;
                    _renderers[i].color = c;
                }

                yield return null;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].color = _original[i];
            }

            _routine = null;
        }

        private void OnDisable()
        {
            if (_renderers == null)
                return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _original != null && i < _original.Length)
                    _renderers[i].color = _original[i];
            }

            _routine = null;
        }
    }
}

/// <summary>A number that pops up above the boss when it is hit, drifts upward and fades. World-space TextMeshPro, no canvas.</summary>
public sealed class DamageNumber : MonoBehaviour
{
    private TextMeshPro _text;
    private float _age;
    private float _scale;
    private Vector2 _drift;
    private const float Life = 0.85f;

    public static DamageNumber Spawn(Vector2 position, string value, Color color, float scale)
    {
        var go = new GameObject("DamageNumber");
        go.transform.position = position;
        var number = go.AddComponent<DamageNumber>();
        number._text = go.AddComponent<TextMeshPro>();
        number._text.text = value;
        number._text.fontSize = 7f;
        number._text.alignment = TextAlignmentOptions.Center;
        number._text.color = color;
        number._text.fontStyle = FontStyles.Bold;
        number._text.sortingOrder = 130;
        number._text.outlineWidth = 0.25f;
        number._text.outlineColor = new Color32(20, 12, 8, 255);
        number._scale = scale;
        number._drift = new Vector2(Random.Range(-0.6f, 0.6f), 2.2f);
        return number;
    }

    private void Update()
    {
        _age += Time.unscaledDeltaTime;
        float k = _age / Life;
        if (k >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(_drift * Time.unscaledDeltaTime);
        _drift.y = Mathf.Max(0.2f, _drift.y - 4f * Time.unscaledDeltaTime);
        float pop = k < 0.15f ? Mathf.Lerp(0.4f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f));
        transform.localScale = Vector3.one * (_scale * pop);
        Color c = _text.color;
        c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
        _text.color = c;
    }
}

/// <summary>Spins the art ring of a circular telegraph (the ring child only, never the fill).</summary>
public sealed class TelegraphSpin : MonoBehaviour
{
    private Transform _ring;

    private void Start() => _ring = transform.Find("ArtRing");

    private void Update()
    {
        if (_ring != null)
            _ring.Rotate(0f, 0f, 22f * Time.deltaTime);
    }
}

/// <summary>Fades a sprite out over its last `fade` seconds and destroys it after `life` seconds.</summary>
public sealed class FadeAndDestroy : MonoBehaviour
{
    private float _life;
    private float _fade;
    private float _age;
    private SpriteRenderer _renderer;
    private float _alpha = 1f;

    public void Begin(float life, float fade)
    {
        _life = life;
        _fade = Mathf.Max(0.01f, fade);
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _alpha = _renderer.color.a;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_renderer != null && _age > _life - _fade)
        {
            Color c = _renderer.color;
            c.a = _alpha * Mathf.Clamp01((_life - _age) / _fade);
            _renderer.color = c;
        }

        if (_age >= _life)
            Destroy(gameObject);
    }
}
