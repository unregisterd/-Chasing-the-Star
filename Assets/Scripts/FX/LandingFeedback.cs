using UnityEngine;

[DisallowMultipleComponent]
public class LandingFeedback : MonoBehaviour
{
    [Header("落地下限速度（米/秒）")]
    [SerializeField] private float minImpactSpeed = 4.5f;

    [Header("镜头震动")]
    [SerializeField] private float shakeAtMin = 0.02f;
    [SerializeField] private float shakeAtMax = 0.12f;
    [SerializeField] private float maxImpactForShake = 16f;

    [Header("重摔顿帧")]
    [SerializeField] private bool hitStopOnHeavyLanding = true;
    [SerializeField] private float heavyImpactSpeed = 9f;
    [SerializeField] private float hitStopDuration = 0.045f;
    [SerializeField] private float hitStopScale = 0.15f;

    private PlayerBlockAgent _agent;
    private ThirdPersonCameraOrbit _orbit;

    private void Awake()
    {
        _agent = GetComponent<PlayerBlockAgent>();

        if (_agent != null)
        {
            _agent.Landed += OnLanded;
        }
    }

    private void OnDestroy()
    {
        if (_agent != null)
        {
            _agent.Landed -= OnLanded;
        }
    }

    private void OnLanded(float impactSpeed)
    {
        if (impactSpeed < minImpactSpeed)
        {
            return;
        }

        if (_orbit == null)
        {
            _orbit = FindObjectOfType<ThirdPersonCameraOrbit>();
        }

        float t = Mathf.InverseLerp(minImpactSpeed, maxImpactForShake, impactSpeed);

        if (_orbit != null)
        {
            _orbit.Shake(Mathf.Lerp(shakeAtMin, shakeAtMax, t), 0.18f);
        }

        if (hitStopOnHeavyLanding && impactSpeed >= heavyImpactSpeed)
        {
            HitStop.Play(hitStopDuration, hitStopScale);
        }
    }
}
