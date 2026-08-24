using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Proximity capability entry points for the Town Elder's shop and recipes.</summary>
public sealed class TownElderCommerceInteractionUI : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private string _stationTag;
    [SerializeField] private GameObject _promptRoot;
    [SerializeField] private TMP_Text _promptText;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _craftingButton;

    private readonly HashSet<Collider2D> _playerColliders = new();
    private PlayerInput _playerInput;

    private void OnEnable()
    {
        _shopButton.onClick.AddListener(OpenShop);
        _craftingButton.onClick.AddListener(OpenCrafting);
        Refresh();
    }

    private void OnDisable()
    {
        _shopButton.onClick.RemoveListener(OpenShop);
        _craftingButton.onClick.RemoveListener(OpenCrafting);
        _playerColliders.Clear();
        _playerInput = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInput input = other.GetComponentInParent<PlayerInput>();
        if (input == null)
            return;
        _playerColliders.Add(other);
        _playerInput = input;
        Refresh();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other))
            return;
        if (_playerColliders.Count == 0)
            _playerInput = null;
        Refresh();
    }

    public void OpenShop()
    {
        if (_playerInput != null)
            ShopCraftingUI.Instance?.OpenShop(_npcId, _playerInput);
    }

    public void OpenCrafting()
    {
        if (_playerInput != null)
            ShopCraftingUI.Instance?.OpenCrafting(_npcId, _stationTag, _playerInput);
    }

    private void Refresh()
    {
        bool visible = _playerColliders.Count > 0;
        _promptRoot.SetActive(visible);
        if (visible)
            _promptText.text = "TOWN ELDER SERVICES";
    }
}
