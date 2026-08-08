using UnityEngine;

public class PlayerCollider : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private float _knockbackForce = 7f;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            _player.TakeDamage(10f, knockbackDirection, _knockbackForce);
            Debug.Log("Player Health: " + PlayerStat.Instance.Health);
        }
    }
}
