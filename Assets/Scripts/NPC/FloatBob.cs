using UnityEngine;

[DisallowMultipleComponent]
public class FloatBob : MonoBehaviour
{
    [Header("上下浮动")]
    [Tooltip("上下浮动的幅度（米）")]
    [SerializeField] private float amplitude = 0.03f;
    [Tooltip("一次完整上下循环的秒数")]
    [SerializeField] private float period = 4f;
    [Tooltip("按世界坐标错开相位，避免所有 NPC 同步起伏")]
    [SerializeField] private bool offsetByPosition = true;

    [Header("轻微摆头（可选）")]
    [SerializeField] private bool swayYaw = true;
    [Tooltip("左右摆动的角度")]
    [SerializeField] private float yawAmplitude = 5f;
    [Tooltip("摆头循环秒数")]
    [SerializeField] private float yawPeriod = 9f;

    private Vector3 _origin;
    private Quaternion _originRotation = Quaternion.identity;
    private float _phase;
    private float _yawPhase;

    private void Awake()
    {
        _origin = transform.localPosition;
        _originRotation = transform.localRotation;

        if (offsetByPosition)
        {
            Vector3 world = transform.position;
            _phase = (world.x * 0.7f + world.z * 1.3f) % (Mathf.PI * 2f);
            _yawPhase = (world.x * 1.1f + world.z * 0.5f) % (Mathf.PI * 2f);
        }
        else
        {
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _yawPhase = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        float bob = amplitude * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, period) + _phase);
        transform.localPosition = _origin + new Vector3(0f, bob, 0f);

        if (!swayYaw)
        {
            return;
        }

        float yaw = yawAmplitude * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.5f, yawPeriod) + _yawPhase);
        transform.localRotation = _originRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    private void OnDisable()
    {
        transform.localPosition = _origin;
        transform.localRotation = _originRotation;
    }
}
