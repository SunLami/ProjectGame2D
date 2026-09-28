using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Presentation-only: plays hover/click SFX via SoundFXManager.PlaySfx. Attach to any
/// Selectable (Button, Toggle, Slider handle...) that should have UI feedback. See
/// AudioSfxSystem.md for the SFX ID catalog and D-058 for the architecture decision.</summary>
public sealed class ButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public enum ClickSound
    {
        Primary,
        Secondary,
        Toggle,
        None
    }

    [SerializeField] private bool _playHoverSfx = true;
    [SerializeField] private ClickSound _clickSound = ClickSound.Primary;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_playHoverSfx)
            SoundFXManager.PlaySfx("sfx.ui.hover");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (_clickSound)
        {
            case ClickSound.Primary:
                SoundFXManager.PlaySfx("sfx.ui.click_primary");
                break;
            case ClickSound.Secondary:
                SoundFXManager.PlaySfx("sfx.ui.click_secondary");
                break;
            case ClickSound.Toggle:
                SoundFXManager.PlaySfx("sfx.ui.toggle");
                break;
        }
    }
}
