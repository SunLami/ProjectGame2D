using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Presentation-only: plays click/submit/toggle SFX via SoundFXManager.PlaySfx. Attach to
/// any Selectable (Button, Toggle...) that should have UI feedback. See AudioSfxSystem.md for the
/// SFX ID catalog and D-058 for the architecture decision. Hover/selection SFX was removed by
/// owner decision (2026-09-29): rapid hover/selection across dense lists (Crafting, Inventory,
/// Shop, Quest) produced constant noise, so this now only fires on an actual click/submit/toggle
/// commitment, never on pointer-enter or keyboard/gamepad selection change.</summary>
public sealed class ButtonSfx : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    public enum ClickSound
    {
        Primary,
        Secondary,
        Toggle,
        None
    }

    [SerializeField] private ClickSound _clickSound = ClickSound.Primary;

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }

    private void PlayClick()
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
