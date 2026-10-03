using System.Collections;
using UnityEngine;

/// <summary>Reuses the existing `ResourceNodeWhiteFlash` swap-material technique
/// (ResourceNodeInteractable.cs) for a brief white flash on an enemy sprite when a skill lands a hit.
/// Added at runtime by SkillProjectile on first hit — not authored on enemy prefabs.</summary>
public class EnemyHitFlash : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Material _originalMaterial;
    private Coroutine _routine;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _renderer != null ? _renderer.sharedMaterial : null;
    }

    public void PlayFlash()
    {
        if (_renderer == null)
            return;

        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Material flashMaterial = Resources.Load<Material>("World/ResourceNodeWhiteFlash");
        if (flashMaterial != null)
            _renderer.sharedMaterial = flashMaterial;

        yield return new WaitForSecondsRealtime(0.12f);

        _renderer.sharedMaterial = _originalMaterial;
        _routine = null;
    }
}
