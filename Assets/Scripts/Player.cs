using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    Rigidbody2D _rb;
    Animator _animator;
    Camera _mainCamera;

    Vector2 _moveInput;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private bool _isMoving;
    [SerializeField] private bool _isAttacking;
    [SerializeField] private bool _isRunning;

    public float MoveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }
    public bool IsMoving { get { return _isMoving; } set { _isMoving = value; } }
    public bool IsAttacking { get { return _isAttacking; } set { _isAttacking = value; } }
    public bool IsRunning { get { return _isRunning; } set { _isRunning = value; } }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
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
        _moveInput = context.ReadValue<Vector2>();

        _animator.SetFloat("InputX", _moveInput.x);
        _animator.SetFloat("InputY", _moveInput.y);

        if (_moveInput != Vector2.zero)
        {
            _isMoving = true;
            _animator.SetBool("isMoving", _isMoving);

            if (!_isAttacking)
            {
                _animator.SetFloat("LastInputX", _moveInput.x);
                _animator.SetFloat("LastInputY", _moveInput.y);
            }
        }

        if (context.canceled || _moveInput == Vector2.zero)
        {
            _isMoving = false;
            _animator.SetBool("isMoving", _isMoving);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _isRunning = true;
            _animator.SetBool("isRunning", _isRunning);
            _moveSpeed *= 2;
        }
        else if (context.canceled)
        {
            _isRunning = false;
            _animator.SetBool("isRunning", _isRunning);
            _moveSpeed /= 2;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && !_isAttacking)
        {
            if (_isRunning)
            {
                _animator.SetFloat("LastInputX", _moveInput.x);
                _animator.SetFloat("LastInputY", _moveInput.y);
            }
            else
                UpdateDirectionToMouse();

            _animator.SetTrigger("Attack");
        }
    }

    private void UpdateDirectionToMouse()
    {

        Vector2 mouseScreenPosition = Pointer.current.position.ReadValue();
        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 attackDirection = (mouseWorldPosition - transform.position).normalized;

        _animator.SetFloat("LastInputX", attackDirection.x);
        _animator.SetFloat("LastInputY", attackDirection.y);
    }
}
