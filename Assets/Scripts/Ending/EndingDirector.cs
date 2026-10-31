using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EndingDirector : MonoBehaviour
{
    [Header("引用（留空自动查找）")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform playerRoot;
    [Tooltip("留空则自动查找场景里的 IMotor（第一人称或第三人称）")]
    [SerializeField] private MonoBehaviour motorSource;
    private IMotor _motor;
    [SerializeField] private PlayerLook look;

    [Header("天空切换")]
    [Tooltip("坠落进度到多少时切换成星空（0~1）")]
    [Range(0.1f, 1f)]
    [SerializeField] private float starSkyProgress = 0.85f;
    [SerializeField] private Material endingSkybox;
    [SerializeField] private Color endingFogColor = new Color(0.02f, 0.02f, 0.05f, 1f);
    [SerializeField] private float endingFogEnd = 400f;

    [Header("坠落")]
    [Tooltip("坠落距离（米），越大塔越像一个点")]
    [SerializeField] private float fallDistance = 160f;
    [SerializeField] private float fallDuration = 9f;
    [Tooltip("坠落时是否回头看出发点（看到塔缩小成一个点）")]
    [SerializeField] private bool lookBackAtTower = true;
    [Tooltip("坠落时的视场角收窄（视觉上更聚焦）")]
    [SerializeField] private float fallFov = 45f;

    [Header("收尾")]
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float finalFov = 65f;

    [Header("调试")]
    [Tooltip("默认 F8 播放 / F7 还原")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private Key playKey = Key.F8;
    [SerializeField] private Key restoreKey = Key.F7;

    private Transform _cameraOriginalParent;
    private Vector3 _cameraOriginalLocalPosition;
    private Quaternion _cameraOriginalLocalRotation;
    private float _cameraOriginalFov;
    private Material _originalSkybox;
    private Color _originalFogColor;
    private float _originalFogEnd;
    private bool _originalFogEnabled;
    private Coroutine _routine;

    public bool IsPlaying => _routine != null;

    private void Awake()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        _motor = motorSource as IMotor;

        if (_motor == null)
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMotor candidate)
                {
                    _motor = candidate;
                    motorSource = behaviours[i];
                    break;
                }
            }
        }

        if (look == null)
        {
            look = FindObjectOfType<PlayerLook>();
        }

        if (playerRoot == null && motorSource != null)
        {
            playerRoot = motorSource.transform;
        }
    }

    private void Update()
    {
        if (!enableDebugKeys || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[playKey].wasPressedThisFrame)
        {
            Play();
        }

        if (Keyboard.current[restoreKey].wasPressedThisFrame)
        {
            Restore();
        }
    }

    public void Play()
    {
        if (viewCamera == null || _routine != null)
        {
            return;
        }

        _routine = StartCoroutine(Run());
    }

    public void Restore()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (viewCamera == null)
        {
            return;
        }

        viewCamera.transform.SetParent(_cameraOriginalParent, false);
        viewCamera.transform.localPosition = _cameraOriginalLocalPosition;
        viewCamera.transform.localRotation = _cameraOriginalLocalRotation;
        viewCamera.fieldOfView = _cameraOriginalFov;

        if (_motor != null)
        {
            _motor.Enabled = true;
        }

        if (look != null)
        {
            look.enabled = true;
        }

        RenderSettings.skybox = _originalSkybox;
        RenderSettings.fogColor = _originalFogColor;
        RenderSettings.fogEndDistance = _originalFogEnd;
        RenderSettings.fog = _originalFogEnabled;
        DynamicGI.UpdateEnvironment();

        Debug.Log("[Ending] 已还原相机与天空");
    }

    private IEnumerator Run()
    {
        _cameraOriginalParent = viewCamera.transform.parent;
        _cameraOriginalLocalPosition = viewCamera.transform.localPosition;
        _cameraOriginalLocalRotation = viewCamera.transform.localRotation;
        _cameraOriginalFov = viewCamera.fieldOfView;
        _originalSkybox = RenderSettings.skybox;
        _originalFogColor = RenderSettings.fogColor;
        _originalFogEnd = RenderSettings.fogEndDistance;
        _originalFogEnabled = RenderSettings.fog;

        if (_motor != null)
        {
            _motor.Enabled = false;
        }

        if (look != null)
        {
            look.enabled = false;
        }

        viewCamera.transform.SetParent(null, true);

        Vector3 startPosition = viewCamera.transform.position;
        Vector3 lookTarget = startPosition + viewCamera.transform.forward * 6f;
        Quaternion startRotation = viewCamera.transform.rotation;
        float startFov = viewCamera.fieldOfView;

        RenderSettings.fog = false;

        bool skyboxSwitched = false;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fallDuration);
            float eased = progress * progress * (3f - 2f * progress);

            Vector3 position = startPosition + Vector3.down * (fallDistance * eased);
            viewCamera.transform.position = position;

            if (lookBackAtTower)
            {
                Vector3 direction = (lookTarget - position).normalized;
                Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
                Quaternion targetRotation = Quaternion.LookRotation(direction, up);
                viewCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(eased * 2f));
            }

            viewCamera.fieldOfView = Mathf.Lerp(startFov, fallFov, eased);

            if (!skyboxSwitched && progress >= starSkyProgress)
            {
                skyboxSwitched = true;
                ApplyStarSky();
                Debug.Log("[Ending] 塔已缩成一个点，切换星空");
            }

            yield return null;
        }

        if (!skyboxSwitched)
        {
            ApplyStarSky();
        }

        float holdStart = Time.time;
        Vector3 holdStartPosition = viewCamera.transform.position;
        Vector3 holdEndPosition = holdStartPosition + Vector3.up * 12f;
        float holdStartFov = viewCamera.fieldOfView;

        while (Time.time - holdStart < holdDuration)
        {
            float progress = Mathf.Clamp01((Time.time - holdStart) / holdDuration);
            float eased = progress * progress * (3f - 2f * progress);

            viewCamera.transform.position = Vector3.Lerp(holdStartPosition, holdEndPosition, eased);
            viewCamera.fieldOfView = Mathf.Lerp(holdStartFov, finalFov, eased);

            yield return null;
        }

        _routine = null;
        Debug.Log("[Ending] 占位结局播放完成。按 F7 还原。");
    }

    private void ApplyStarSky()
    {
        if (endingSkybox != null)
        {
            RenderSettings.skybox = endingSkybox;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = endingFogColor;
        RenderSettings.fogEndDistance = endingFogEnd;
        DynamicGI.UpdateEnvironment();
    }
}
