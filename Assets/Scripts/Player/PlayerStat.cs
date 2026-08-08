using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    public static PlayerStat Instance;
    [SerializeField] private float _health;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _baseAtkDmg;
    [SerializeField] private float _atkDmg;
    public float Health { get { return _health; } set { _health = value; } }
    public float MaxHealth { get { return _maxHealth; } }
    public float BaseAtkDmg { get { return _baseAtkDmg; } }
    public float AtkDmg { get { return _atkDmg; } set { _atkDmg = value; } }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _health = _maxHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
