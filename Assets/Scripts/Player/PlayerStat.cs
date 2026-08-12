using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    public static PlayerStat Instance { get; private set; }

    [SerializeField] private float _health;
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField] private float _baseAtkDmg;
    [SerializeField] private float _atkDmg;

    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public float BaseAtkDmg => _baseAtkDmg;
    public bool IsDead => _health <= 0f;

    public float AtkDmg
    {
        get => _atkDmg;
        set => _atkDmg = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _health = _maxHealth;
    }

    public bool TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return IsDead;

        _health = Mathf.Clamp(_health - amount, 0f, _maxHealth);
        return IsDead;
    }
}

