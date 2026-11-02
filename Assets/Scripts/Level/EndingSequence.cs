using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class EndingSequence : MonoBehaviour
{
    private enum Phase
    {
        Idle,
        Narration,
        Hint,
        Transition,
    }

    [Header("触发")]
    [Tooltip("玩家进入这个半径就算到达终点")]
    [SerializeField] private float arrivalRadius = 1.8f;
    [SerializeField] private Transform playerOverride;

    [Header("文本")]
    [SerializeField] private string narrationId = "ending_narration";
    [SerializeField] private string hintId = "ending_jump_hint";
    [Tooltip("长旁白停留多久后换成「再跳一步」")]
    [SerializeField] private float narrationHold = 6f;

    [Header("坠落与转场")]
    [Tooltip("比如 1.5 米：一开始下落就立刻扣住死亡重生")]
    [SerializeField] private float fallLatchDistance = 1.5f;
    [Tooltip("下落超过这个距离就切场景（必须小于死亡判定的 20m）")]
    [SerializeField] private float voidDropDistance = 4f;
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private float blackHold = 0.3f;
    [SerializeField] private string nextScene = "MainEnding";

    private Phase _phase;
    private Transform _player;
    private PlayerRespawnManager _respawn;
    private PlayerBlockAgent _agent;
    private Hud_StarEnergy _hud;
    private float _phaseTime;
    private float _topY;
    private bool _fallLatched;

    public static bool IsPlaying { get; private set; }

    private static EndingSequence _active;

    private void Awake()
    {
        if (_active != null && _active != this)
        {
            if (transform.position.y <= _active.transform.position.y)
            {
                Debug.LogWarning("[Ending] 场景里有多个 EndingSequence，已禁用较低的这个：" + name, this);
                enabled = false;
                return;
            }

            Debug.LogWarning("[Ending] 发现更高的 EndingSequence，禁用较低的那个：" + _active.name, this);
            _active.enabled = false;
        }

        _active = this;
        IsPlaying = false;
        _phase = Phase.Idle;

        if (narrationId == "ending_last_words")
        {
            narrationId = "ending_narration";
        }

        if (Loc.Get(narrationId) == null)
        {
            Debug.LogWarning("[Ending] 找不到文本 " + narrationId + "，改用 ending_narration");
            narrationId = "ending_narration";
        }

        if (Loc.Get(hintId) == null)
        {
            Debug.LogWarning("[Ending] 找不到文本 " + hintId + "，改用 ending_jump_hint");
            hintId = "ending_jump_hint";
        }

        Block block = GetComponent<Block>();
        _topY = block != null ? block.GetTopHeight() : transform.position.y;

        _respawn = FindObjectOfType<PlayerRespawnManager>();
        _agent = FindObjectOfType<PlayerBlockAgent>();

        if (FindObjectOfType<EndingAtmosphere>() == null)
        {
            gameObject.AddComponent<EndingAtmosphere>();
        }
        _hud = FindObjectOfType<Hud_StarEnergy>();

        if (playerOverride != null)
        {
            _player = playerOverride;
            return;
        }

        PlayerBlockAgent agent = FindObjectOfType<PlayerBlockAgent>();

        if (agent != null)
        {
            _player = agent.transform;
        }
    }

    private void OnDestroy()
    {
        if (_active == this)
        {
            _active = null;
        }

        IsPlaying = false;
    }

    private void Update()
    {
        if (_player == null || _phase == Phase.Transition)
        {
            return;
        }

        switch (_phase)
        {
            case Phase.Idle:
                if (WithinArrival())
                {
                    StartNarration();
                }
                break;

            case Phase.Narration:
                _phaseTime += Time.deltaTime;

                if (TryBeginFall())
                {
                    break;
                }

                if (_phaseTime >= narrationHold)
                {
                    ShowHint();
                }
                break;

            case Phase.Hint:
                TryBeginFall();
                break;
        }
    }

    private bool WithinArrival()
    {
        Vector3 delta = _player.position - transform.position;
        delta.y *= 0.5f;
        return delta.sqrMagnitude <= arrivalRadius * arrivalRadius;
    }

    private void StartNarration()
    {
        _phase = Phase.Narration;
        _phaseTime = 0f;

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.ShowCenter(WithSpeaker(narrationId), 0f, true);
        }
    }

    private static string WithSpeaker(string id)
    {
        string line = Loc.Text(id);
        string speaker = Loc.Speaker(id);
        return string.IsNullOrEmpty(speaker) ? line : speaker + "\\n" + line;
    }

    private void ShowHint()
    {
        _phase = Phase.Hint;

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.ShowText(Loc.Text(hintId), 0f, true);
        }
    }

    private bool TryBeginFall()
    {
        if (_player.position.y > _topY - fallLatchDistance)
        {
            return false;
        }

        if (!_fallLatched)
        {
            _fallLatched = true;
            IsPlaying = true;

            if (_respawn != null)
            {
                _respawn.enabled = false;
            }

            if (_agent != null)
            {
                _agent.SetAutoFallRespawn(false);
            }

            if (_hud != null)
            {
                _hud.SetVisible(false);
            }

            if (UITextManager.Instance != null)
            {
                UITextManager.Instance.SetControlsVisible(false);
                UITextManager.Instance.ShowText(string.Empty, 0f, false);
                UITextManager.Instance.HideCenter();
            }
        }

        if (_player.position.y > _topY - voidDropDistance)
        {
            return true;
        }

        _phase = Phase.Transition;
        StartCoroutine(RunTransition());
        return true;
    }

    private IEnumerator RunTransition()
    {
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.Fade(1f, fadeDuration);
        }

        if (blackHold > 0f)
        {
            yield return new WaitForSeconds(blackHold);
        }

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.SetAlpha(1f);
        }

        SceneManager.LoadScene(nextScene);
    }

    private void OnValidate()
    {
        if (voidDropDistance <= fallLatchDistance)
        {
            voidDropDistance = fallLatchDistance + 1f;
        }
    }
}
