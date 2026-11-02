using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SecretInscription : MonoBehaviour
{
    private static int _collectedCount;
    private static int _total;

    [Header("内容")]
    [SerializeField] private string textId = "secret_1";
    [Tooltip("集齐全部后额外显示的一句")]
    [SerializeField] private string allTextId = "secret_all";

    [Header("触发")]
    [SerializeField] private float radius = 1.8f;
    [SerializeField] private float holdSeconds = 7f;

    [Header("光点")]
    [SerializeField] private bool createOrb = true;
    [SerializeField] private float orbRadius = 0.09f;
    [SerializeField] private Color orbColor = new Color(1f, 0.92f, 0.78f, 1f);

    private Transform _player;
    private Transform _orb;
    private bool _collected;

    private void Awake()
    {
        _total = Mathf.Max(_total, FindObjectsOfType<SecretInscription>(true).Length);
    }

    private void Start()
    {
        PlayerBlockAgent agent = FindObjectOfType<PlayerBlockAgent>();

        if (agent != null)
        {
            _player = agent.transform;
        }

        if (createOrb)
        {
            CreateOrb();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    private static void HandleSceneUnloaded(Scene scene)
    {
        _collectedCount = 0;
        _total = 0;
    }

    private void Update()
    {
        if (_collected || _player == null)
        {
            return;
        }

        Vector3 delta = _player.position - transform.position;

        if (delta.sqrMagnitude > radius * radius)
        {
            return;
        }

        _collected = true;
        _collectedCount++;

        if (_orb != null)
        {
            _orb.gameObject.SetActive(false);
        }

        if (UITextManager.Instance == null)
        {
            return;
        }

        UITextManager.Instance.ShowCenter(Loc.Text(textId), holdSeconds, false);

        if (_total > 1 && _collectedCount >= _total)
        {
            UITextManager.Instance.ShowText(Loc.Text(allTextId), 6f, false);
        }
    }

    private void CreateOrb()
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "SecretOrb";
        Destroy(orb.GetComponent<Collider>());

        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = Vector3.zero;
        orb.transform.localScale = Vector3.one * (orbRadius * 2f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader);
        material.SetColor("_BaseColor", new Color(orbColor.r, orbColor.g, orbColor.b, 1f));
        material.SetColor("_EmissionColor", orbColor * 3f);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        material.SetFloat("_Smoothness", 0.2f);

        Renderer renderer = orb.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        _orb = orb.transform;
    }
}
