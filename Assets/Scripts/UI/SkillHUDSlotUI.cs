using UnityEngine;
using UnityEngine.UI;

// One diamond slot inside SkillHUD: either a Q/E/R/T loadout slot (icon + radial cooldown overlay +
// key label, dimmed when empty) or the Shield slot (fixed icon, no cooldown, highlighted while
// Player.IsBlocking instead). SkillHUDController owns deciding which state applies each frame.
public sealed class SkillHUDSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private Image _activeHighlight;

    public void Configure(Image icon, Image cooldownOverlay, Image activeHighlight)
    {
        _icon = icon;
        _cooldownOverlay = cooldownOverlay;
        _activeHighlight = activeHighlight;
    }

    /// <summary>Null icon = empty/unequipped slot: hidden (an Image with no sprite otherwise draws
    /// as a solid-colour quad, not nothing), no cooldown shown.</summary>
    public void SetIcon(Sprite icon)
    {
        if (_icon == null)
            return;

        _icon.sprite = icon;
        _icon.enabled = icon != null;
        if (icon == null && _cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = 0f;
            _cooldownOverlay.enabled = false;
        }
    }

    /// <summary>0 = ready (overlay hidden), 1 = just cast (overlay fully covers the icon).</summary>
    public void SetCooldownFraction01(float fraction)
    {
        if (_cooldownOverlay == null)
            return;

        float value = float.IsNaN(fraction) ? 0f : Mathf.Clamp01(fraction);
        _cooldownOverlay.fillAmount = value;
        _cooldownOverlay.enabled = value > 0f && _icon != null && _icon.sprite != null;
    }

    public void SetActiveHighlight(bool active)
    {
        if (_activeHighlight != null)
            _activeHighlight.enabled = active;
    }
}
