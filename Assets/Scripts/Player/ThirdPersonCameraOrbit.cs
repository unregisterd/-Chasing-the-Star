using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class ThirdPersonCameraOrbit : MonoBehaviour
{
    [Header("输入")]
    [Tooltip("留空则自动查找场景里的 PlayerInputReader")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("轨道")]
    [SerializeField] private float distance = 3.4f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private float startYaw;
    [SerializeField] private float startPitch = 12f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.45f, 0f);

    private CinemachineVirtualCamera _virtualCamera;
    private CinemachineTransposer _transposer;
    private CinemachineComposer _composer;
    private float _yaw;
    private float _shakeStrength;
    private float _shakeDecay;
    private Vector3 _shakeOffset;
    private float _pitch;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _transposer = _virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        _composer = _virtualCamera.GetCinemachineComponent<CinemachineComposer>();

        _yaw = startYaw;
        _pitch = startPitch;

        Apply();
    }

    public void Shake(float strength, float duration = 0.18f)
    {
        float safeDuration = Mathf.Max(0.05f, duration);
        _shakeStrength = Mathf.Max(_shakeStrength, strength);
        _shakeDecay = _shakeStrength / safeDuration;
    }

    private void Start()
    {
        if (lockCursorOnStart && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (input == null)
        {
            input = FindObjectOfType<PlayerInputReader>();
        }

        if (input != null && Time.timeScale > 0f)
        {
            Vector2 look = input.Look * sensitivity;
            _yaw += look.x;
            _pitch += invertY ? look.y : -look.y;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        UpdateShake();
        Apply();
    }

    private void UpdateShake()
    {
        if (_shakeStrength <= 0f)
        {
            _shakeOffset = Vector3.zero;
            return;
        }

        _shakeStrength = Mathf.Max(0f, _shakeStrength - _shakeDecay * Time.unscaledDeltaTime);
        _shakeOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)) * _shakeStrength;
    }

    private void Apply()
    {
        if (_transposer != null)
        {
            _transposer.m_FollowOffset = Quaternion.Euler(_pitch, _yaw, 0f) * new Vector3(0f, 0f, -distance) + _shakeOffset;
        }

        if (_composer != null)
        {
            _composer.m_TrackedObjectOffset = lookOffset;
        }
    }

    public void SetYaw(float yaw)
    {
        _yaw = yaw;
        Apply();
    }
}
