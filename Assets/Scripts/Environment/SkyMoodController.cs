using UnityEngine;

public class SkyMoodController : MonoBehaviour
{
    public static SkyMoodController Instance { get; private set; }

    [Header("氛围颜色")]
    [SerializeField] private Color duskTop = new Color(0.42f, 0.34f, 0.58f);
    [SerializeField] private Color duskHorizon = new Color(1.00f, 0.79f, 0.64f);
    [SerializeField] private Color duskGround = new Color(0.22f, 0.18f, 0.26f);
    [SerializeField] private Color duskFog = new Color(0.96f, 0.80f, 0.72f);
    [SerializeField] private Color duskAmbient = new Color(0.62f, 0.55f, 0.62f);

    [SerializeField] private Color nightTop = new Color(0.08f, 0.10f, 0.28f);
    [SerializeField] private Color nightHorizon = new Color(0.12f, 0.62f, 0.78f);
    [SerializeField] private Color nightGround = new Color(0.03f, 0.04f, 0.10f);
    [SerializeField] private Color nightFog = new Color(0.07f, 0.10f, 0.24f);
    [SerializeField] private Color nightAmbient = new Color(0.18f, 0.22f, 0.38f);

    [Header("涟漪颜色")]
    [SerializeField] private Color duskRipple = new Color(1f, 0.86f, 0.8f);
    [SerializeField] private Color nightRipple = new Color(0.55f, 0.95f, 1f);

    [Header("背景板")]
    [SerializeField] private Texture2D duskBackdrop;
    [SerializeField] private Texture2D nightBackdrop;
    [SerializeField] private float backdropDistance = 60f;
    [SerializeField] private float backdropPadding = 1.12f;

    [Header("音乐")]
    [SerializeField] private AudioClip duskBgm;
    [SerializeField] private AudioClip nightBgm;
    [SerializeField] private float bgmVolume = 0.55f;
    [Tooltip("音乐交叉淡入时长（秒），与视觉过渡解耦，避免两首歌长时间叠加")]
    [SerializeField] private float bgmCrossfade = 2.5f;

    [Header("切换")]
    [SerializeField] private float blendDuration = 8f;
    [SerializeField] private string nightAnchorName = "Tower_Extension";
    [Tooltip("找不到 Tower_Extension 时用作切换高度的兜底值")]
    [SerializeField] private float nightHeightFallback = 45f;
    [SerializeField] private float fogEndDusk = 60f;
    [SerializeField] private float fogEndNight = 95f;

    public float Blend { get; private set; }

    public float BackdropRoll { get; set; }
    public float Target { get; set; }

    public Color RippleColor => Color.Lerp(duskRipple, nightRipple, Blend);

    private Material _skyMaterial;
    private Material _backdropMaterial;
    private Transform _backdropQuad;
    private Camera _camera;
    private PlayerBlockAgent _player;

    private AudioSource _duskSource;
    private AudioSource _nightSource;

    private float _nightHeight;
    private bool _hasNightAnchor;
    private float _lastAppliedBlend = -1f;
    private float _bgmBlend;

    private void Awake()
    {
        Instance = this;

        _skyMaterial = new Material(Shader.Find("Sky/Gradient"));
        RenderSettings.skybox = _skyMaterial;

        if (duskBackdrop == null) duskBackdrop = Resources.Load<Texture2D>("Sky/Backdrop_Dusk");
        if (nightBackdrop == null) nightBackdrop = Resources.Load<Texture2D>("Sky/Backdrop_Night");
        if (duskBgm == null) duskBgm = Resources.Load<AudioClip>("Audio/BGM/dusk");
        if (nightBgm == null) nightBgm = Resources.Load<AudioClip>("Audio/BGM/night");

        CreateBackdrop();
        CreateBgm();

        ResolveNightHeight();
        Apply(true);
    }

    private void Update()
    {
        if (_player == null)
        {
            PlayerBlockAgent agent = FindObjectOfType<PlayerBlockAgent>();
            if (agent != null) _player = agent;
        }

        if (_hasNightAnchor && _player != null && _player.transform.position.y >= _nightHeight)
        {
            if (Target < 1f)
            {
                Debug.Log("[Mood] 到达夜色高度，开始黄昏→深夜过渡");
            }

            Target = 1f;
        }

        UpdateBgm(Time.deltaTime);

        if (Mathf.Approximately(Blend, Target))
        {
            return;
        }

        float speed = blendDuration > 0.01f ? 1f / blendDuration : 1000f;
        Blend = Mathf.MoveTowards(Blend, Target, speed * Time.deltaTime);
        Apply(false);
    }

    private void UpdateBgm(float dt)
    {
        if (_duskSource == null && _nightSource == null)
        {
            return;
        }

        float speed = bgmCrossfade > 0.01f ? 1f / bgmCrossfade : 1000f;
        _bgmBlend = Mathf.MoveTowards(_bgmBlend, Target, speed * dt);

        if (_duskSource != null) _duskSource.volume = bgmVolume * (1f - _bgmBlend);
        if (_nightSource != null) _nightSource.volume = bgmVolume * _bgmBlend;
    }

    public void SetTarget(float target)
    {
        Target = Mathf.Clamp01(target);
    }

    private void ResolveNightHeight()
    {
        GameObject anchor = GameObject.Find(nightAnchorName);

        if (anchor == null)
        {
            _nightHeight = nightHeightFallback;
            _hasNightAnchor = true;
            Debug.LogWarning($"[Mood] 场景里没有 {nightAnchorName}，改用兜底高度 {nightHeightFallback}m 触发夜色。");
            return;
        }

        Block[] blocks = anchor.GetComponentsInChildren<Block>(true);

        if (blocks.Length == 0)
        {
            _nightHeight = anchor.transform.position.y;
            _hasNightAnchor = true;
            return;
        }

        float lowest = float.MaxValue;

        foreach (Block block in blocks)
        {
            lowest = Mathf.Min(lowest, block.GetTopHeight());
        }

        _nightHeight = lowest - 1.5f;
        _hasNightAnchor = true;
    }

    private void Apply(bool force)
    {
        if (!force && Mathf.Approximately(_lastAppliedBlend, Blend))
        {
            return;
        }

        _lastAppliedBlend = Blend;

        if (_skyMaterial != null)
        {
            _skyMaterial.SetColor("_TopColor", Color.Lerp(duskTop, nightTop, Blend));
            _skyMaterial.SetColor("_HorizonColor", Color.Lerp(duskHorizon, nightHorizon, Blend));
            _skyMaterial.SetColor("_GroundColor", Color.Lerp(duskGround, nightGround, Blend));
        }

        RenderSettings.fogColor = Color.Lerp(duskFog, nightFog, Blend);
        RenderSettings.fogEndDistance = Mathf.Lerp(fogEndDusk, fogEndNight, Blend);
        RenderSettings.ambientLight = Color.Lerp(duskAmbient, nightAmbient, Blend);

        if (_backdropMaterial != null)
        {
            _backdropMaterial.SetFloat("_Blend", Blend);
        }

        RenderSettings.skybox = _skyMaterial;
        DynamicGI.UpdateEnvironment();
    }

    private void LateUpdate()
    {
        if (_backdropQuad == null)
        {
            return;
        }

        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        float height = 2f * backdropDistance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * backdropPadding;
        float width = height * _camera.aspect * backdropPadding;

        _backdropQuad.position = _camera.transform.position + _camera.transform.forward * backdropDistance;
        _backdropQuad.rotation = _camera.transform.rotation * Quaternion.Euler(0f, 0f, BackdropRoll);
        _backdropQuad.localScale = new Vector3(width, height, 1f);
    }

    private void CreateBackdrop()
    {
        Shader shader = Shader.Find("Sky/Backdrop");

        if (shader == null)
        {
            Debug.LogWarning("[Mood] 找不到 Sky/Backdrop shader");
            return;
        }

        _backdropMaterial = new Material(shader);

        if (duskBackdrop != null) _backdropMaterial.SetTexture("_MainTex", duskBackdrop);
        if (nightBackdrop != null) _backdropMaterial.SetTexture("_SecondTex", nightBackdrop);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Backdrop";
        Destroy(quad.GetComponent<Collider>());

        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = _backdropMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _backdropQuad = quad.transform;
    }

    private void CreateBgm()
    {
        _duskSource = gameObject.AddComponent<AudioSource>();
        _duskSource.clip = duskBgm;
        _duskSource.loop = true;
        _duskSource.playOnAwake = false;
        _duskSource.volume = bgmVolume;

        _nightSource = gameObject.AddComponent<AudioSource>();
        _nightSource.clip = nightBgm;
        _nightSource.loop = true;
        _nightSource.playOnAwake = false;
        _nightSource.volume = 0f;

        if (duskBgm != null) _duskSource.Play();
        if (nightBgm != null) _nightSource.Play();
    }
}
