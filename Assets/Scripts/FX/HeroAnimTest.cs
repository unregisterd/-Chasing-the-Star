using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HeroAnimTest : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float slowMotionScale = 0.25f;

    private float _speed = 1f;
    private bool _slowed;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void Start()
    {
        SetSpeed(0f);
        Debug.Log("[HeroTest] 1=站立 2=跑步 3=跳跃 4=慢动作 5=切换 0.5 倍速");
    }

    private void Update()
    {
        if (Keyboard.current == null || animator == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetSpeed(0f);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SetSpeed(1f);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            animator.SetTrigger("Jump");
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            _slowed = !_slowed;
            animator.speed = _slowed ? slowMotionScale : 1f;
            Debug.Log("[HeroTest] 慢动作 " + (_slowed ? "开" : "关"));
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            _speed = Mathf.Approximately(_speed, 0.5f) ? 1f : 0.5f;
            SetSpeed(_speed);
        }
    }

    private void SetSpeed(float value)
    {
        _speed = value;
        animator.SetFloat("Speed", value);
        Debug.Log("[HeroTest] Speed = " + value.ToString("0.00") + "（0 站立 / 1 跑 / 0.5 走）");
    }
}
