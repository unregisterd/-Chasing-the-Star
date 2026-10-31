using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class ThirdPersonMotor : MonoBehaviour, IMotor
{
    [Header("移动")]
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runSpeed = 3.4f;
    [SerializeField] private float sprintSpeed = 4.6f;
    [SerializeField] private float acceleration = 18f;
    [Tooltip("空中加速度相对地面的比例")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.45f;
    [Tooltip("转身平滑时间（秒），越小越灵敏")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("跳跃与重力")]
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float terminalVelocity = -30f;
    [Tooltip("离开边缘后仍可起跳的宽容时间")]
    [SerializeField] private float coyoteTime = 0.12f;
    [Tooltip("落地前提前按跳的缓冲时间")]
    [SerializeField] private float jumpBuffer = 0.12f;
    [Tooltip("起跳后的冷却，防止连点")]
    [SerializeField] private float jumpCooldown = 0.2f;

    [Header("动画")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string jumpParameter = "Jump";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string fallingParameter = "Falling";
    [Tooltip("动画里跑步循环对应的地面速度（米/秒），用来消除脚滑")]
    [SerializeField] private float animationBaseSpeed = 3.4f;

    private CharacterController _controller;
    private PlayerInputReader _input;
    private Transform _cameraTransform;
    private Vector3 _planarVelocity;
    private float _verticalVelocity;
    private float _lastGroundedTime = -999f;
    private float _jumpPressTime = -999f;
    private bool _coyoteUsed;
    private float _nextJumpTime;
    private float _turnVelocity;

    public bool IsGrounded => _controller != null && _controller.isGrounded;

    public bool Enabled
    {
        get { return enabled; }
        set { enabled = value; }
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputReader>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool grounded = _controller.isGrounded;

        if (grounded)
        {
            _lastGroundedTime = Time.time;
            _coyoteUsed = false;
        }

        Vector2 moveInput = Vector2.ClampMagnitude(_input.Move, 1f);
        Vector3 wishDirection = CameraRelative(moveInput);

        float baseSpeed = _input.SprintHeld
            ? sprintSpeed
            : (moveInput.magnitude < 0.6f ? walkSpeed : runSpeed);

        float targetSpeed = wishDirection.magnitude * baseSpeed;

        if (moveInput.magnitude < 0.05f)
        {
            targetSpeed = 0f;
        }

        float accel = grounded ? acceleration : acceleration * airControl;
        Vector3 targetVelocity = wishDirection * targetSpeed;
        _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, accel * dt);

        if (_input.JumpPressed)
        {
            _jumpPressTime = Time.time;
        }

        bool withinCoyote = !_coyoteUsed && (Time.time - _lastGroundedTime) <= coyoteTime;
        bool withinBuffer = (Time.time - _jumpPressTime) <= jumpBuffer;

        if (withinCoyote && withinBuffer && Time.time >= _nextJumpTime && _verticalVelocity <= 0.5f)
        {
            Jump();
        }

        if (grounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        if (_verticalVelocity > terminalVelocity)
        {
            _verticalVelocity += gravity * dt;
        }

        _controller.Move((_planarVelocity + Vector3.up * _verticalVelocity) * dt);

        UpdateRotation(wishDirection, dt);
        UpdateAnimator(grounded);
    }

    private void Jump()
    {
        _verticalVelocity = Mathf.Sqrt(2f * Mathf.Max(0.1f, jumpHeight) * Mathf.Abs(gravity));
        _nextJumpTime = Time.time + jumpCooldown;
        _jumpPressTime = -999f;
        _coyoteUsed = true;

        if (animator != null && !string.IsNullOrEmpty(jumpParameter))
        {
            animator.SetTrigger(jumpParameter);
        }
    }

    private Vector3 CameraRelative(Vector2 moveInput)
    {
        if (_cameraTransform == null)
        {
            Camera camera = Camera.main;

            if (camera != null)
            {
                _cameraTransform = camera.transform;
            }
        }

        if (_cameraTransform == null)
        {
            return transform.right * moveInput.x + transform.forward * moveInput.y;
        }

        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = _cameraTransform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 direction = right * moveInput.x + forward * moveInput.y;
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    private void UpdateRotation(Vector3 wishDirection, float dt)
    {
        if (wishDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float targetYaw = Mathf.Atan2(wishDirection.x, wishDirection.z) * Mathf.Rad2Deg;
        float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _turnVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void UpdateAnimator(bool grounded)
    {
        if (animator == null)
        {
            return;
        }

        Vector3 planar = _planarVelocity;
        planar.y = 0f;
        float speed = planar.magnitude;
        float blend = animationBaseSpeed > 0.01f ? speed / animationBaseSpeed : speed;

        if (!string.IsNullOrEmpty(speedParameter))
        {
            animator.SetFloat(speedParameter, blend);
        }

        if (!string.IsNullOrEmpty(groundedParameter))
        {
            animator.SetBool(groundedParameter, grounded);
        }

        if (!string.IsNullOrEmpty(fallingParameter))
        {
            animator.SetBool(fallingParameter, !grounded && _verticalVelocity < -1f);
        }

        if (grounded && !string.IsNullOrEmpty(speedParameter) && animationBaseSpeed > 0.01f && speed > 0.05f)
        {
            animator.speed = Mathf.Clamp(speed / animationBaseSpeed, 0.6f, 1.6f);
        }
        else
        {
            animator.speed = 1f;
        }
    }

    public void Launch(float height, float forwardSpeed)
    {
        _verticalVelocity = Mathf.Sqrt(2f * Mathf.Max(0.05f, height) * Mathf.Abs(gravity));
        _nextJumpTime = Time.time + jumpCooldown;
        _jumpPressTime = -999f;
        _coyoteUsed = true;

        if (forwardSpeed > 0f)
        {
            Vector3 forward = transform.forward;
            _planarVelocity = new Vector3(forward.x * forwardSpeed, 0f, forward.z * forwardSpeed);
        }

        if (animator != null && !string.IsNullOrEmpty(jumpParameter))
        {
            animator.SetTrigger(jumpParameter);
        }
    }

    public void Teleport(Vector3 position)
    {
        _planarVelocity = Vector3.zero;
        _verticalVelocity = 0f;
        _jumpPressTime = -999f;
        _coyoteUsed = false;

        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;
    }
}
