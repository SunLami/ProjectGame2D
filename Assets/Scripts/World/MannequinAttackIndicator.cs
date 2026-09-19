using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial 5's "attack this" marker: a bouncing down-arrow above a training mannequin while
/// quest.trainer_killquest is still Active and this specific dummy is alive. Hides while this
/// dummy is dead/respawning (nothing to attack right now) and disappears everywhere once the
/// quest's objective is done (ReadyToTurnIn/Completed) -- purely a visual nudge; the quest's own
/// progress (QuestManager) is the actual source of truth, this never gates anything.
/// </summary>
public sealed class MannequinAttackIndicator : MonoBehaviour
{
    private const string KillQuestId = "quest.trainer_killquest";

    [SerializeField] private MannequinHurtbox _owner;
    [SerializeField] private GameObject _arrowRoot;
    [SerializeField] private Image _arrowImage;

    [SerializeField] private float _bobAmplitude = 0.15f;
    [SerializeField] private float _bobSpeed = 2.5f;

    private QuestManager _questManager;
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        if (_owner == null)
            _owner = GetComponentInParent<MannequinHurtbox>();
        if (_arrowImage != null && _arrowImage.sprite == null)
            _arrowImage.sprite = ProceduralArrowSprite.GetUpArrow();
        if (_arrowRoot != null)
        {
            _baseLocalPosition = _arrowRoot.transform.localPosition;
            // Placeholder sprite points up by default -- rotate 180 so it points down at the dummy.
            _arrowRoot.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
        }
    }

    private void OnEnable()
    {
        _questManager = QuestManager.Instance;
        if (_questManager != null)
        {
            _questManager.QuestProgressChanged += HandleQuestChanged;
            _questManager.QuestCompleted += HandleQuestChanged;
        }
        if (_owner != null)
        {
            _owner.HealthChanged += HandleHealthChanged;
            _owner.Respawned += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (_questManager != null)
        {
            _questManager.QuestProgressChanged -= HandleQuestChanged;
            _questManager.QuestCompleted -= HandleQuestChanged;
        }
        if (_owner != null)
        {
            _owner.HealthChanged -= HandleHealthChanged;
            _owner.Respawned -= Refresh;
        }
    }

    private void Update()
    {
        if (_arrowRoot == null || !_arrowRoot.activeSelf)
            return;

        float offset = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
        _arrowRoot.transform.localPosition = _baseLocalPosition + new Vector3(0f, offset, 0f);
    }

    private void HandleHealthChanged(float current, float max) => Refresh();

    private void HandleQuestChanged(string questId)
    {
        if (questId == KillQuestId)
            Refresh();
    }

    private void Refresh()
    {
        if (_arrowRoot == null)
            return;

        bool questActive = _questManager != null && _questManager.GetStatus(KillQuestId) == QuestStatus.Active;
        bool alive = _owner != null && !_owner.IsDead;
        _arrowRoot.SetActive(questActive && alive);
    }
}
