using UnityEngine;

[DisallowMultipleComponent]
public class EndingAtmosphere : MonoBehaviour
{
    [Header("风声")]
    [SerializeField] private string windResource = "Audio/Ambient/wind";
    [SerializeField] private float approachRange = 22f;
    [SerializeField] private float maxApproachVolume = 0.35f;
    [SerializeField] private float fallingVolume = 0.85f;
    [SerializeField] private float fadeSpeed = 0.8f;

    [Header("星空")]
    [SerializeField] private float idleRollSpeed = 0f;
    [SerializeField] private float fallingRollSpeed = 1.6f;

    private AudioSource _wind;
    private PlayerBlockAgent _player;
    private SkyMoodController _mood;
    private float _targetVolume;
    private float _rollSpeed;

    private void Awake()
    {
        AudioClip clip = Resources.Load<AudioClip>(windResource);

        if (clip != null)
        {
            _wind = gameObject.AddComponent<AudioSource>();
            _wind.clip = clip;
            _wind.loop = true;
            _wind.playOnAwake = false;
            _wind.spatialBlend = 0f;
            _wind.volume = 0f;
            _wind.Play();
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindObjectOfType<PlayerBlockAgent>();
        }

        if (_mood == null)
        {
            _mood = SkyMoodController.Instance;
        }

        float targetVolume = 0f;
        float targetRoll = idleRollSpeed;

        if (EndingSequence.IsPlaying)
        {
            targetVolume = fallingVolume;
            targetRoll = fallingRollSpeed;
        }
        else if (_player != null)
        {
            float distance = Vector3.Distance(_player.transform.position, transform.position);
            float t = Mathf.Clamp01(1f - distance / Mathf.Max(1f, approachRange));
            targetVolume = maxApproachVolume * t;
        }

        if (_wind != null)
        {
            _wind.volume = Mathf.MoveTowards(_wind.volume, targetVolume, fadeSpeed * Time.unscaledDeltaTime);
        }

        if (_mood != null)
        {
            _rollSpeed = Mathf.MoveTowards(_rollSpeed, targetRoll, 1.5f * Time.unscaledDeltaTime);
            _mood.BackdropRoll += _rollSpeed * Time.unscaledDeltaTime;
        }
    }
}
