using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBlockAgent : MonoBehaviour
{
    [Tooltip("留空则自动查找同一物体上的 IMotor")]
    [SerializeField] private MonoBehaviour motorSource;
    private IMotor _motor;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("从脚底向下探测方块的距离")]
    [SerializeField] private float groundProbeDistance = 0.35f;
    [Tooltip("是否启用就近回退（调试用）；正式流程由 PlayerRespawnManager 接管")]
    [SerializeField] private bool autoFallRespawn;
    [Tooltip("掉到低于最后站立方块多少米就回退（推荐 3~5）")]
    [SerializeField] private float fallDepth = 3.5f;
    [Tooltip("绝对兜底高度（找不到最后方块时使用）")]
    [SerializeField] private float fallRespawnY = -12f;

    [Header("简单模式回退")]
    [Tooltip("简单模式下低于方块顶面多少米才回退")]
    [SerializeField] private float easyFallDepth = 5f;
    [Tooltip("向下探测多远还有方块；探到就不算掉出关卡（保护故意往下跳的路线）")]
    [SerializeField] private float fallProbeDistance = 25f;
    [Tooltip("回退时的淡出/淡入时长")]
    [SerializeField] private float respawnFadeDuration = 0.15f;
    [Tooltip("留空则自动查找 AbilityWallet")]
    [SerializeField] private AbilityWallet wallet;

    [Header("落地音效")]
    [Tooltip("留空则从 Resources/Audio/step_land 自动加载")]
    [SerializeField] private AudioClip landingClip;
    [Range(0f, 1f)]
    [SerializeField] private float landingVolume = 0.7f;

    [Header("走路脚步")]
    [Tooltip("留空则复用落地音效")]
    [SerializeField] private AudioClip stepClip;
    [Tooltip("每走多少米响一次")]
    [SerializeField] private float stepDistance = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float stepVolume = 0.35f;

    private CharacterController _controller;
    private Block _standingBlock;
    private Block _lastStandingBlock;
    private float _lastStandingTop;
    private Vector3 _lastSafePosition;
    private AudioSource _audio;
    private float _stepAccumulator;
    private float _lastLandingTime = -10f;
    private bool _wasGrounded = true;
    private bool _respawning;
    private int _safePoints;
    private float _effectiveFallDepth = 3.5f;
    private bool _walletBound;

    public Block StandingBlock => _standingBlock;

    public bool IsGrounded => _controller != null && _controller.isGrounded;

    public event Action<Block> StandingBlockChanged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        _motor = motorSource as IMotor;
        if (_motor == null)
        {
            _motor = GetComponent<IMotor>();
        }

        _lastSafePosition = transform.position;

        _audio = GetComponent<AudioSource>();

        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        if (landingClip == null)
        {
            landingClip = Resources.Load<AudioClip>("Audio/step_land");
        }

        if (stepClip == null)
        {
            stepClip = landingClip;
        }

        autoFallRespawn = Difficulty.IsEasy;
        _effectiveFallDepth = Difficulty.IsEasy ? Mathf.Max(fallDepth, easyFallDepth) : fallDepth;

        Debug.Log("[Agent] 难度：" + (Difficulty.IsEasy ? "简单（掉落后回退）" : "困难（掉落后重开）")
                  + "，回退深度 " + _effectiveFallDepth.ToString("0.0") + "m");
    }

    public void SetAutoFallRespawn(bool value)
    {
        autoFallRespawn = value;
    }

    private void Update()
    {
        ResolveWallet();

        if (_respawning)
        {
            return;
        }

        if (_motor != null && !_motor.Enabled)
        {
            return;
        }

        if (ShouldRespawn())
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        if (!_controller.isGrounded)
        {
            _wasGrounded = false;
            return;
        }

        if (!_wasGrounded)
        {
            _wasGrounded = true;
            _lastLandingTime = Time.time;
            _stepAccumulator = stepDistance * 0.5f;
            PlayLandingFeedback();
            SnapshotPoints();
        }

        UpdateFootsteps();

        Block under = ProbeBlock();

        if (under == null)
        {
            return;
        }

        if (under != _standingBlock)
        {
            _standingBlock = under;
            StandingBlockChanged?.Invoke(under);
        }

        _lastStandingBlock = under;
        _lastStandingTop = under.GetTopHeight();

        _lastSafePosition = transform.position;
    }

    public Block ProbeBlock()
    {
        float feetY = GetFeetY();
        Vector3 origin = new Vector3(transform.position.x, feetY + 0.08f, transform.position.z);
        float distance = 0.08f + groundProbeDistance;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Block>();
        }

        return null;
    }

    public float GetFeetY()
    {
        if (_controller != null && _controller.height > 0f)
        {
            return transform.TransformPoint(_controller.center).y - _controller.height * 0.5f;
        }

        return transform.position.y;
    }

    private bool ShouldRespawn()
    {
        if (!autoFallRespawn)
        {
            return transform.position.y < fallRespawnY;
        }

        if (_lastStandingBlock != null && _lastStandingBlock.gameObject.activeInHierarchy)
        {
            bool tooLow = transform.position.y < _lastStandingTop - _effectiveFallDepth;

            if (!tooLow)
            {
                return false;
            }

            return !HasBlockBelow();
        }

        return transform.position.y < fallRespawnY;
    }

    private bool HasBlockBelow()
    {
        Vector3 origin = new Vector3(transform.position.x, GetFeetY() + 0.1f, transform.position.z);

        return Physics.Raycast(origin, Vector3.down, fallProbeDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void ResolveWallet()
    {
        if (wallet == null)
        {
            wallet = GetComponent<AbilityWallet>();
        }

        if (wallet == null)
        {
            wallet = FindObjectOfType<AbilityWallet>();
        }

        if (wallet == null || _walletBound)
        {
            return;
        }

        wallet.Changed += HandleWalletChanged;
        _walletBound = true;

        if (wallet.Points > _safePoints)
        {
            _safePoints = wallet.Points;
        }
    }

    private void HandleWalletChanged(int points, int maxPoints)
    {
        if (points > _safePoints)
        {
            _safePoints = points;
        }
    }

    private void SnapshotPoints()
    {
        ResolveWallet();

        if (wallet != null && wallet.Points > _safePoints)
        {
            _safePoints = wallet.Points;
        }
    }

    private void OnDestroy()
    {
        if (wallet != null && _walletBound)
        {
            wallet.Changed -= HandleWalletChanged;
        }
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        if (_respawning)
        {
            yield break;
        }

        _respawning = true;

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.Fade(1f, respawnFadeDuration);
        }

        RespawnOnLastBlock();

        yield return null;

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.Fade(0f, respawnFadeDuration);
        }

        _respawning = false;
    }

    private void RespawnOnLastBlock()
    {
        Block block = _lastStandingBlock;

        if (block == null)
        {
            RespawnAt(_lastSafePosition);
            Debug.Log("[Agent] 掉出地图，回到上一个安全点");
            return;
        }

        Collider collider = block.GetComponentInChildren<Collider>();
        Vector3 center = collider != null ? collider.bounds.center : block.transform.position;
        float top = block.GetTopHeight();
        float feetOffset = transform.position.y - GetFeetY();
        Vector3 target = new Vector3(center.x, top + 0.06f + feetOffset, center.z);

        RespawnAt(target);
        _wasGrounded = true;

        if (wallet != null)
        {
            wallet.SetPoints(_safePoints);
        }

        Debug.Log($"[Agent] 掉落，回到方块 {block.name}（顶面 {top:F1}m），星光恢复到 {_safePoints}");
    }

    public void RespawnAt(Vector3 position)
    {
        if (_motor != null)
        {
            _motor.Teleport(position);
        }
        else
        {
            transform.position = position;
        }

        _standingBlock = null;
        _lastSafePosition = position;
    }


    private void PlayLandingFeedback()
    {
        Block block = ProbeBlock();

        if (block != null)
        {
            block.TryAdvance();

            BlockFeedback feedback = block.GetComponent<BlockFeedback>();

            if (feedback == null)
            {
                feedback = block.gameObject.AddComponent<BlockFeedback>();
            }

            feedback.Play();
        }

        PlayLandingSound();
    }

    private void UpdateFootsteps()
    {
        if (_audio == null || stepClip == null || Time.time - _lastLandingTime < 0.15f)
        {
            return;
        }

        Vector3 velocity = _controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (speed < 0.3f)
        {
            return;
        }

        _stepAccumulator += speed * Time.deltaTime;

        if (_stepAccumulator < stepDistance)
        {
            return;
        }

        _stepAccumulator = 0f;
        _audio.pitch = 0.92f + UnityEngine.Random.Range(0f, 0.16f);
        _audio.PlayOneShot(stepClip, stepVolume);
    }

    private void PlayLandingSound()
    {
        if (_audio == null || landingClip == null)
        {
            return;
        }

        _audio.pitch = 0.95f + UnityEngine.Random.Range(0f, 0.1f);
        _audio.PlayOneShot(landingClip, landingVolume);
    }

    public void NotifyExternalMove()
    {
        _lastSafePosition = transform.position;
    }

    private void OnValidate()
    {
        CharacterController controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            return;
        }

        float expectedCenterY = controller.height * 0.5f;
        bool offCenter = Mathf.Abs(controller.center.x) > 0.02f
                         || Mathf.Abs(controller.center.z) > 0.02f
                         || Mathf.Abs(controller.center.y - expectedCenterY) > 0.12f;

        if (offCenter)
        {
            Debug.LogWarning(
                $"[PlayerBlockAgent] CharacterController 的 center 与胶囊高度不匹配（期望 y≈{expectedCenterY:0.##}，当前 {controller.center}），"
                + "脚底探测与落地判定可能偏。ThirdPerson 预制体自带 0.93 属于正常范围。",
                this);
        }
    }
}
