using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Top-of-screen boss health bar (D-086), built in code so it needs no prefab. Marks the phase
/// thresholds and turns the fill orange while the boss is recovering (the player's damage window).</summary>
public sealed class BossHealthBarUI : MonoBehaviour
{
    private BossController _boss;
    private RectTransform _fill;
    private Image _fillImage;
    private TextMeshProUGUI _label;

    private static readonly Color Normal = new Color(0.78f, 0.2f, 0.12f, 1f);
    private static readonly Color Window = new Color(1f, 0.72f, 0.2f, 1f);

    public static BossHealthBarUI Create(BossController boss)
    {
        var canvasObject = new GameObject("BossHealthBar_" + boss.name);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var bar = canvasObject.AddComponent<BossHealthBarUI>();
        bar.Build(boss);
        return bar;
    }

    private void Build(BossController boss)
    {
        _boss = boss;

        RectTransform root = NewRect("Root", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        root.sizeDelta = new Vector2(900f, 26f);
        root.anchoredPosition = new Vector2(0f, -56f);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.07f, 0.05f, 0.04f, 0.9f);
        background.raycastTarget = false;

        _fill = NewRect("Fill", root, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f));
        _fill.offsetMin = new Vector2(3f, 3f);
        _fill.offsetMax = new Vector2(-3f, -3f);
        _fillImage = _fill.gameObject.AddComponent<Image>();
        _fillImage.color = Normal;
        _fillImage.raycastTarget = false;

        // phase threshold ticks
        BossPhase[] phases = boss.Definition != null ? boss.Definition.phases : null;
        if (phases != null)
        {
            for (int i = 1; i < phases.Length; i++)
            {
                float x = phases[i].startsBelowHealth;
                RectTransform tick = NewRect("Tick" + i, root, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(0.5f, 0.5f));
                tick.sizeDelta = new Vector2(3f, 0f);
                Image tickImage = tick.gameObject.AddComponent<Image>();
                tickImage.color = new Color(1f, 1f, 1f, 0.7f);
                tickImage.raycastTarget = false;
            }
        }

        RectTransform labelRect = NewRect("Label", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f));
        labelRect.sizeDelta = new Vector2(0f, 28f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
        _label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        _label.text = boss.Definition != null ? boss.Definition.displayName : boss.name;
        _label.fontSize = 24;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(1f, 0.93f, 0.8f, 1f);
        _label.raycastTarget = false;

        boss.HealthChanged += OnHealthChanged;
        boss.Died += OnBossDied;
        OnHealthChanged(boss.Health, boss.MaxHealth);
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }

    private void OnHealthChanged(float health, float max)
    {
        if (_fill == null)
            return;

        Vector2 anchorMax = _fill.anchorMax;
        anchorMax.x = max > 0f ? Mathf.Clamp01(health / max) : 0f;
        _fill.anchorMax = anchorMax;
    }

    private void Update()
    {
        if (_boss != null && _fillImage != null)
            _fillImage.color = _boss.IsRecovering ? Window : Normal;
    }

    private void OnBossDied() => Destroy(gameObject);

    private void OnDestroy()
    {
        if (_boss != null)
        {
            _boss.HealthChanged -= OnHealthChanged;
            _boss.Died -= OnBossDied;
        }
    }
}
