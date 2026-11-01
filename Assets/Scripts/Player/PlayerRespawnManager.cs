using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRespawnManager : MonoBehaviour
{
    [Header("引用（留空自动查找）")]
    [SerializeField] private PlayerInputReader input;
    [Tooltip("留空则自动查找同一物体上的 IMotor")]
    [SerializeField] private MonoBehaviour motorSource;
    private IMotor _motor;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerBlockAgent agent;
    [SerializeField] private Hud_StarEnergy hud;

    [Header("死亡判定")]
    [Tooltip("比最近一次站稳的高度低多少米才判死（15~25）")]
    [SerializeField] private float fallDeathDistance = 20f;
    [Tooltip("绝对地板，低于它就判死")]
    [SerializeField] private float worldFloorY = -50f;

    [Header("死亡演出")]
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private AudioClip deathClip;
    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 0.8f;

    [Header("开场演出")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private float openingHold = 2.5f;
    [SerializeField] private float narrationHold = 4.5f;
    [SerializeField] private float narration2Hold = 3.5f;
    [SerializeField] private float hintHold = 2.5f;
    [SerializeField] private float introFadeIn = 1.2f;

    private bool _busy;
    private bool _fallDeathEnabled = true;
    private bool _hasBeenGrounded;
    private float _controlEnabledAt;
    private float _lastSafeY;
    private AudioSource _audio;
    private EndingDirector _ending;

    public bool IsBusy => _busy;

    private void Awake()
    {
        if (input == null) input = GetComponent<PlayerInputReader>();
        _motor = motorSource as IMotor;
        if (_motor == null) _motor = GetComponent<IMotor>();
        if (look == null) look = GetComponent<PlayerLook>();
        if (agent == null) agent = GetComponent<PlayerBlockAgent>();
        if (hud == null) hud = GetComponent<Hud_StarEnergy>();
        _ending = FindObjectOfType<EndingDirector>();

        _audio = GetComponent<AudioSource>();

        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        _lastSafeY = transform.position.y;
        _fallDeathEnabled = Difficulty.Current == DifficultyMode.Hard;

        if (deathClip == null)
        {
            deathClip = Resources.Load<AudioClip>("Audio/SFX/death");
        }
    }

    private void Start()
    {
        _controlEnabledAt = Time.time;

        if (playIntro)
        {
            StartCoroutine(IntroRoutine());
        }
    }

    private void Update()
    {
        if (_busy)
        {
            return;
        }

        if (_ending != null && _ending.IsPlaying)
        {
            return;
        }

        if (input != null && input.ResetPressed)
        {
            StartCoroutine(DeathRoutine());
            return;
        }

        if (agent != null && agent.IsGrounded)
        {
            _lastSafeY = transform.position.y;
            _hasBeenGrounded = true;
            return;
        }

        bool fellTooFar = _fallDeathEnabled && transform.position.y < _lastSafeY - fallDeathDistance;
        bool belowFloor = _fallDeathEnabled && transform.position.y < worldFloorY;

        if (!fellTooFar && !belowFloor)
        {
            return;
        }

        if (!_hasBeenGrounded && Time.time - _controlEnabledAt < 5f)
        {
            Debug.LogError("[Respawn] 出生点下方没有落脚点，玩家一直在下落。请检查 Player 与出生台的坐标。");
            _busy = true;
            return;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        _busy = true;
        SetControlEnabled(false);

        if (hud != null) hud.SetVisible(false);

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.SetControlsVisible(false);
        }

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.SetAlpha(1f);
        }

        ShowLine("intro_opening");
        yield return new WaitForSeconds(openingHold);

        ShowLine("intro_narration");
        yield return new WaitForSeconds(narrationHold);

        ShowLine("intro_narration_2");
        yield return new WaitForSeconds(narration2Hold);

        ShowLine("intro_hint");
        yield return new WaitForSeconds(hintHold);

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.HideCenter();
        }

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.Fade(0f, introFadeIn);
        }

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.SetControlsVisible(true);
        }

        if (hud != null) hud.SetVisible(true);
        SetControlEnabled(true);
        _controlEnabledAt = Time.time;
        _hasBeenGrounded = false;
        _lastSafeY = transform.position.y;
        _busy = false;
    }

    private IEnumerator DeathRoutine()
    {
        if (_busy)
        {
            yield break;
        }

        _busy = true;
        SetControlEnabled(false);

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.SetControlsVisible(false);
        }

        if (hud != null) hud.SetVisible(false);

        if (deathClip != null && _audio != null)
        {
            _audio.PlayOneShot(deathClip, deathVolume);
        }

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.Fade(1f, fadeOutDuration);
        }

        if (UITextManager.Instance != null)
        {
            string line = $"{Loc.Text("death_fade")}\n{Loc.Text("death_return")}";
            UITextManager.Instance.ShowCenter(line, 0f, true);
        }

        yield return new WaitForSeconds(holdDuration);

        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
            Debug.LogError("[Respawn] 当前场景不在 Build Settings 里，无法重开。");
            _busy = false;
        }
    }

    private void ShowLine(string id)
    {
        if (UITextManager.Instance == null)
        {
            return;
        }

        string speaker = Loc.Speaker(id);
        string text = Loc.Text(id);
        string content = string.IsNullOrEmpty(speaker) ? text : $"{speaker}\n{text}";

        UITextManager.Instance.ShowCenter(content, 0f, true);
    }

    private void SetControlEnabled(bool enabled)
    {
        if (_motor != null) _motor.Enabled = enabled;
        if (look != null) look.enabled = enabled;
        if (agent != null) agent.enabled = enabled;
    }
}
