using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Presentation")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _portrait;
    [SerializeField] private TMP_Text _speakerName;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Vector2 _bodyTextWithChoicesPosition = new(98f, 45f);
    [SerializeField] private Vector2 _bodyTextWithChoicesSize = new(360f, 28f);
    [SerializeField] private float _bodyTextWithChoicesFontSize = 13f;
    [SerializeField] private Vector2 _bodyTextWithoutChoicesPosition = new(98f, -7f);
    [SerializeField] private Vector2 _bodyTextWithoutChoicesSize = new(360f, 96f);
    [SerializeField] private float _bodyTextWithoutChoicesFontSize = 15f;
    [SerializeField] private GameObject _continueIndicator;
    [SerializeField] private Transform _choiceRoot;
    [SerializeField] private Button _choiceTemplate;
    [SerializeField, Min(1f)] private float _charactersPerSecond = 42f;

    private readonly List<Button> _choiceButtons = new();
    private DialogueDefinition _definition;
    private DialogueNodeDefinition _currentNode;
    private Action<string> _completed;
    private Coroutine _typewriter;
    private bool _isRevealing;
    private DialogueHudGroup _hiddenHudGroup;
    private bool _hudWasActive;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _root.SetActive(false);
        _choiceTemplate.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        bool confirm = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        confirm |= Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        if (confirm && !IsPointerOverChoice())
            Continue();

        bool cancel = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        cancel |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (cancel)
            Close(false);
    }

    public bool Open(DialogueDefinition definition, Action<string> completed = null)
    {
        if (definition == null || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || !definition.TryGetNode(definition.InitialNodeId, out DialogueNodeDefinition initial))
            return false;

        _definition = definition;
        _completed = completed;
        HideOtherHud();
        _root.SetActive(true);
        GameStateManager.Instance.PushState(GameState.Dialogue);
        ShowNode(initial);
        return true;
    }

    public void Continue()
    {
        if (!IsOpen || _currentNode == null)
            return;
        if (_isRevealing)
        {
            FinishReveal();
            return;
        }
        if (_currentNode.Choices.Count > 0)
            return;
        if (!string.IsNullOrEmpty(_currentNode.NextNodeId)
            && _definition.TryGetNode(_currentNode.NextNodeId, out DialogueNodeDefinition next))
        {
            ShowNode(next);
            return;
        }
        Close(true);
    }

    public void Close(bool reportCompletion)
    {
        if (!IsOpen)
            return;

        string outcome = reportCompletion ? _currentNode?.OutcomeId : null;
        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = null;
        _isRevealing = false;
        ClearChoices();
        _root.SetActive(false);
        _definition = null;
        _currentNode = null;
        Action<string> completed = _completed;
        _completed = null;
        RestoreOtherHud();
        GameStateManager.Instance?.ReturnToPreviousState();
        if (reportCompletion)
            completed?.Invoke(outcome ?? string.Empty);
    }

    private void ShowNode(DialogueNodeDefinition node)
    {
        _currentNode = node;
        ClearChoices();
        _speakerName.text = node.SpeakerName;
        _portrait.sprite = node.Portrait;
        _portrait.enabled = node.Portrait != null;
        ApplyBodyLayout(node.Choices.Count > 0);
        _bodyText.text = node.Text ?? string.Empty;
        _bodyText.maxVisibleCharacters = 0;
        _continueIndicator.SetActive(false);
        _typewriter = StartCoroutine(RevealText());
    }

    private void ApplyBodyLayout(bool hasChoices)
    {
        RectTransform rect = _bodyText.rectTransform;
        rect.anchoredPosition = hasChoices ? _bodyTextWithChoicesPosition : _bodyTextWithoutChoicesPosition;
        rect.sizeDelta = hasChoices ? _bodyTextWithChoicesSize : _bodyTextWithoutChoicesSize;
        _bodyText.fontSize = hasChoices ? _bodyTextWithChoicesFontSize : _bodyTextWithoutChoicesFontSize;
    }

    private IEnumerator RevealText()
    {
        _isRevealing = true;
        _bodyText.ForceMeshUpdate();
        int count = _bodyText.textInfo.characterCount;
        float delay = 1f / _charactersPerSecond;
        for (int visible = 1; visible <= count; visible++)
        {
            _bodyText.maxVisibleCharacters = visible;
            yield return new WaitForSecondsRealtime(delay);
        }
        FinishReveal();
    }

    private void FinishReveal()
    {
        if (_typewriter != null)
            StopCoroutine(_typewriter);
        _typewriter = null;
        _isRevealing = false;
        _bodyText.maxVisibleCharacters = int.MaxValue;
        BuildChoices();
        _continueIndicator.SetActive(_currentNode.Choices.Count == 0);
    }

    private void BuildChoices()
    {
        foreach (DialogueChoiceDefinition choice in _currentNode.Choices)
        {
            Button button = Instantiate(_choiceTemplate, _choiceRoot);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>(true).text = choice.Text;
            string nextNodeId = choice.NextNodeId;
            button.onClick.AddListener(() => SelectChoice(nextNodeId));
            _choiceButtons.Add(button);
        }
    }

    private void SelectChoice(string nextNodeId)
    {
        if (_definition.TryGetNode(nextNodeId, out DialogueNodeDefinition next))
            ShowNode(next);
    }

    private void ClearChoices()
    {
        foreach (Button button in _choiceButtons)
            if (button != null)
                Destroy(button.gameObject);
        _choiceButtons.Clear();
    }

    private bool IsPointerOverChoice()
    {
        foreach (Button button in _choiceButtons)
            if (button != null && button.IsActive() && button.transform is RectTransform rect
                && RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current?.position.ReadValue() ?? Vector2.zero))
                return true;
        return false;
    }

    private void HideOtherHud()
    {
        _hiddenHudGroup = FindAnyObjectByType<DialogueHudGroup>(FindObjectsInactive.Include);
        if (_hiddenHudGroup == null)
            return;
        _hudWasActive = _hiddenHudGroup.gameObject.activeSelf;
        _hiddenHudGroup.gameObject.SetActive(false);
    }

    private void RestoreOtherHud()
    {
        if (_hiddenHudGroup == null)
            return;
        _hiddenHudGroup.gameObject.SetActive(_hudWasActive);
        _hiddenHudGroup = null;
    }

    private void OnDestroy()
    {
        RestoreOtherHud();
        if (Instance == this)
            Instance = null;
    }
}
