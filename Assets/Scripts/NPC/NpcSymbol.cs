using TMPro;
using UnityEngine;

public class NpcSymbol : MonoBehaviour
{
    [Header("奖励")]
    [SerializeField] private AbilityType ability = AbilityType.Courage;
    [SerializeField] private int points = 2;

    [Header("文案")]
    [Tooltip("NPC 台词 id，例如 npc_light_line")]
    [SerializeField] private string dialogueId = "npc_light_line";
    [Tooltip("能力说明 id，留空则不显示")]
    [SerializeField] private string abilityDescId = "ability_courage_desc";

    [Header("触发")]
    [SerializeField] private float range = 3.5f;
    [SerializeField] private float retryInterval = 0.5f;

    [Header("表现")]
    [SerializeField] private float worldTextHeight = 2.2f;
    [SerializeField] private float worldTextWidth = 6f;
    [SerializeField] private bool createRipples = true;

    [Header("视觉浮动")]
    [Tooltip("自动给视觉子物体加轻微上下浮动")]
    [SerializeField] private bool autoFloatVisual = true;
    [SerializeField] private float bobAmplitude = 0.04f;
    [SerializeField] private float bobPeriod = 5f;
    [SerializeField] private float bobYawAmplitude = 5f;

    [SerializeField] private AbilityWallet walletOverride;
    [Header("音效")]
    [Tooltip("留空则从 Resources/Audio/SFX/star_pickup 自动加载")]
    [SerializeField] private AudioClip pickupClip;
    [Range(0f, 1f)]
    [SerializeField] private float pickupVolume = 0.8f;

    private AbilityWallet _wallet;
    private float _nextWalletLookup;
    private Transform _player;
    private TextMeshPro _worldText;
    private RippleEmitter _ripple;
    private AudioSource _audio;
    private bool _inRange;
    private bool _claimed;
    private int _remainingPoints = -1;
    private float _nextRetryTime;
    private string _promptLine = string.Empty;

    public bool HasGivenPoints { get; private set; }

    private void Awake()
    {
        _wallet = walletOverride != null ? walletOverride : FindObjectOfType<AbilityWallet>();
        PlayerBlockAgent agent = FindObjectOfType<PlayerBlockAgent>();

        if (agent != null)
        {
            _player = agent.transform;
        }

        CreateWorldText();
        SetupVisualFloat();

        _audio = GetComponent<AudioSource>();

        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0.15f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.maxDistance = 25f;
        }

        if (pickupClip == null)
        {
            pickupClip = Resources.Load<AudioClip>("Audio/SFX/star_pickup");
        }

        if (createRipples)
        {
            _ripple = gameObject.GetComponent<RippleEmitter>();

            if (_ripple == null)
            {
                _ripple = gameObject.AddComponent<RippleEmitter>();
            }

            _ripple.Configure(true, 2.4f, 2.6f, 1.2f, 4.5f, 0.5f);
        }
    }

    private void Update()
    {
        EnsureWallet();

        if (_player == null)
        {
            return;
        }

        bool inRange = Vector3.Distance(_player.position, transform.position) <= range;

        if (inRange != _inRange)
        {
            _inRange = inRange;

            if (inRange)
            {
                ShowText();
            }
            else
            {
                HideText();
            }
        }

        if (!_inRange)
        {
            return;
        }

        UpdateWorldTextFacing();

        if (!_claimed && Time.time >= _nextRetryTime)
        {
            _nextRetryTime = Time.time + retryInterval;
            TryClaim();
        }
    }

    private void EnsureWallet()
    {
        if (_wallet != null)
        {
            return;
        }

        if (Time.time < _nextWalletLookup)
        {
            return;
        }

        _nextWalletLookup = Time.time + 1f;
        _wallet = FindObjectOfType<AbilityWallet>();

        if (_wallet == null)
        {
            Debug.LogWarning("[NPC] 场景里还没有 AbilityWallet（确认 Player 上挂了 AbilityController）");
        }
    }
    private void TryClaim()
    {
        if (_wallet == null)
        {
            return;
        }

        if (_remainingPoints < 0)
        {
            _remainingPoints = points;
        }

        int free = _wallet.MaxPoints - _wallet.Points;

        if (free <= 0)
        {
            SetPrompt(Loc.Language == "en" ? "Star Energy is full." : "星光已满，先用掉一些吧");
            return;
        }

        int give = Mathf.Min(free, _remainingPoints);

        if (give > 0)
        {
            HasGivenPoints = true;
        }
        int accepted = _wallet.Add(give);
        _remainingPoints -= accepted;

        if (accepted > 0)
        {
            PlayPickupSound();
        }

        if (_remainingPoints <= 0)
        {
            _claimed = true;
        }

        SetPrompt(Loc.Language == "en" ? $"+{accepted} Star Energy" : $"+{accepted} 点星光");

        Debug.Log($"[NPC] {ability} 领取 {accepted} 点（剩余 {_remainingPoints}，当前 {_wallet.Points}/{_wallet.MaxPoints}）");
    }

    private void PlayPickupSound()
    {
        if (_audio == null || pickupClip == null)
        {
            return;
        }

        _audio.pitch = 0.96f + UnityEngine.Random.Range(0f, 0.08f);
        _audio.PlayOneShot(pickupClip, pickupVolume);
    }

    public void ResetClaim()
    {
        _claimed = false;
        _remainingPoints = points;
        HasGivenPoints = false;
    }
    private void SetPrompt(string line)
    {
        if (_promptLine == line)
        {
            return;
        }

        _promptLine = line;
        ShowText();
    }

    private void ShowText()
    {
        string speaker = Loc.Speaker(dialogueId);
        string line = Loc.Text(dialogueId);
        string desc = string.IsNullOrEmpty(abilityDescId) ? string.Empty : Loc.Text(abilityDescId);

        if (!string.IsNullOrEmpty(desc))
        {
            line += "\n" + desc;
        }

        if (!string.IsNullOrEmpty(_promptLine))
        {
            line += "\n" + _promptLine;
        }

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.ShowText(string.IsNullOrEmpty(speaker) ? line : speaker + "\n" + line, 0f, true);
        }

        if (_worldText != null)
        {
            string header = string.IsNullOrEmpty(speaker) ? string.Empty : $"<b>{speaker}</b>\n";
            _worldText.text = header + Loc.Text(dialogueId);
            _worldText.gameObject.SetActive(true);
        }
    }

    private void HideText()
    {
        _promptLine = string.Empty;
        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.HideText();
        }

        if (_worldText != null)
        {
            _worldText.gameObject.SetActive(false);
        }
    }

    private void SetupVisualFloat()
    {
        if (!autoFloatVisual)
        {
            return;
        }

        Transform visual = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child.name == "WorldText")
            {
                continue;
            }

            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                visual = child;
                break;
            }
        }

        if (visual == null)
        {
            return;
        }

        FloatBob bob = visual.GetComponent<FloatBob>();

        if (bob == null)
        {
            bob = visual.gameObject.AddComponent<FloatBob>();
        }

        bob.Configure(bobAmplitude, bobPeriod, bobYawAmplitude);
    }

    private void CreateWorldText()
    {
        GameObject textObject = new GameObject("WorldText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = Vector3.up * worldTextHeight;

        _worldText = textObject.AddComponent<TextMeshPro>();
        _worldText.alignment = TextAlignmentOptions.Center;
        _worldText.fontSize = 3f;
        _worldText.enableWordWrapping = true;
        _worldText.rectTransform.sizeDelta = new Vector2(worldTextWidth, 3f);
        _worldText.color = Color.black;
        _worldText.outlineWidth = 0.25f;
        _worldText.outlineColor = Color.white;
        _worldText.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansCN-VF-2 SDF");

        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }

        if (font != null)
        {
            _worldText.font = font;
        }

        _worldText.gameObject.SetActive(false);
    }

    private void UpdateWorldTextFacing()
    {
        Camera camera = Camera.main;

        if (_worldText != null && camera != null && _worldText.gameObject.activeSelf)
        {
            _worldText.transform.rotation = camera.transform.rotation;
        }
    }

    private void OnValidate()
    {
        points = Mathf.Max(1, points);
        range = Mathf.Max(1f, range);
    }
}
