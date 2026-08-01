using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody2D _rb;
    Animator _animator;

    Vector2 _moveInput;

    [SerializeField] float _moveSpeed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }
    private void Start()
    {
    }

    private void Update()
    {
        _rb.linearVelocity = _moveInput * _moveSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _animator.SetBool("isMoving", true);

        if (context.canceled)
        {
            _animator.SetBool("isMoving", false);
        }
        _animator.SetFloat("LastInputX", _moveInput.x);
        _animator.SetFloat("LastInputY", _moveInput.y);

        _moveInput = context.ReadValue<Vector2>();
        _animator.SetFloat("InputX", _moveInput.x);
        _animator.SetFloat("InputY", _moveInput.y);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _animator.SetBool("isRunning", true);
            _moveSpeed *= 2;
        }
        else if (context.canceled)
        {
            _animator.SetBool("isRunning", false);
            _moveSpeed /= 2;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _animator.SetTrigger("Attack");
        }
    }
}
