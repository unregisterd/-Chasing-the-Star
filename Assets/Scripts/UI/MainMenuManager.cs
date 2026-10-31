using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonGroup;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("前景小骑士")]
    [SerializeField] private RectTransform knightRect;
    [SerializeField] private CanvasGroup glowGroup;
    [SerializeField] private RectTransform glowRect;

    [Header("音频")]
    [SerializeField] private string bgmResource = "Audio/BGM/menu";
    [SerializeField] private string hoverResource = "Audio/UI/hover";
    [SerializeField] private string clickResource = "Audio/UI/click";
    [SerializeField] private float bgmVolume = 0.5f;
    [SerializeField] private float sfxVolume = 0.7f;

    [Header("转场")]
    [SerializeField] private string gameSceneName = "Main";
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("黄昏天空")]
    [SerializeField] private Color skyTop = new Color(0.42f, 0.34f, 0.58f);
    [SerializeField] private Color skyHorizon = new Color(1.00f, 0.79f, 0.64f);
    [SerializeField] private Color skyGround = new Color(0.22f, 0.18f, 0.26f);
    [SerializeField] private Color ambientColor = new Color(0.62f, 0.55f, 0.62f);

    [Header("本地化")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text subtitleLabel;
    [SerializeField] private TMP_Text startLabel;
    [SerializeField] private TMP_Text quitLabel;
    [SerializeField] private Button languageToggleButton;
    [SerializeField] private RawImage languageToggleIcon;
    [SerializeField] private Texture2D languageEnIcon;
    [SerializeField] private Texture2D languageZhIcon;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private AudioClip _clickClip;
    private bool _leaving;

    private void Awake()
    {
        SetupSky();
        SetupAudio();
    }

    private void Start()
    {
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (buttonGroup != null) buttonGroup.alpha = 0f;
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;

        BindButtons();
        PlayMenuIntro();
        PlayKnightBreathing();

        Loc.LanguageChanged += RefreshLocalized;
        RefreshLocalized();

        if (_bgmSource != null && _bgmSource.clip != null)
        {
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            _bgmSource.DOFade(bgmVolume, 2.5f).SetEase(Ease.OutQuad);
        }
    }

    private void SetupSky()
    {
        Shader shader = Shader.Find("Sky/Gradient");

        if (shader != null)
        {
            Material sky = new Material(shader);
            sky.SetColor("_TopColor", skyTop);
            sky.SetColor("_HorizonColor", skyHorizon);
            sky.SetColor("_GroundColor", skyGround);
            sky.SetFloat("_Exponent", 1.3f);
            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.fog = false;
    }

    private void SetupAudio()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.clip = Resources.Load<AudioClip>(bgmResource);
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
        _sfxSource.volume = sfxVolume;

        _clickClip = Resources.Load<AudioClip>(clickResource);
    }

    private void PlayMenuIntro()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOFade(0f, 2f).SetEase(Ease.OutQuad);
        }

        if (titleGroup != null)
        {
            titleGroup.alpha = 0f;
            titleGroup.DOFade(1f, 2.5f).SetDelay(0.6f).SetEase(Ease.OutQuad);
        }

        if (buttonGroup != null)
        {
            buttonGroup.alpha = 0f;
            buttonGroup.DOFade(1f, 2f).SetDelay(1.6f).SetEase(Ease.OutQuad);
        }
    }

    private void PlayKnightBreathing()
    {
        if (glowGroup != null)
        {
            glowGroup.alpha = 0.35f;
            glowGroup.DOFade(0.8f, 2.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        if (glowRect != null)
        {
            glowRect.DOScale(1.08f, 2.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        if (knightRect != null)
        {
            float baseY = knightRect.anchoredPosition.y;
            knightRect.DOAnchorPosY(baseY + 5f, 3.4f)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(Ease.InOutSine);
        }
    }

    private void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            AddHover(startButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            AddHover(quitButton);
        }

        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.AddListener(ToggleLanguage);
            AddHover(languageToggleButton);
        }
    }

    private void ToggleLanguage()
    {
        Loc.SetLanguage(Loc.Language == "en" ? "zh" : "en");
    }

    private void RefreshLocalized()
    {
        if (titleLabel != null) titleLabel.text = Loc.Text("menu_title");
        if (subtitleLabel != null) subtitleLabel.text = Loc.Language == "en" ? "逐星" : "CHASING THE STAR";
        if (startLabel != null) startLabel.text = Loc.Text("menu_start");
        if (quitLabel != null) quitLabel.text = Loc.Text("menu_quit");

        if (languageToggleIcon != null)
        {
            Texture2D icon = Loc.Language == "en" ? languageEnIcon : languageZhIcon;

            if (icon != null)
            {
                languageToggleIcon.texture = icon;
            }
        }
    }

    private void OnDestroy()
    {
        Loc.LanguageChanged -= RefreshLocalized;
    }

    private void AddHover(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnHover(button));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => OnHoverExit(button));
        trigger.triggers.Add(exit);
    }

    private void OnHover(Button button)
    {
        if (_leaving)
        {
            return;
        }

        PlayHoverSound();
        button.transform.DOKill();
        button.transform.DOScale(1.07f, 0.16f).SetEase(Ease.OutQuad);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();

        if (label != null)
        {
            label.DOKill();
            label.DOFade(1f, 0.16f);
        }
    }

    private void OnHoverExit(Button button)
    {
        button.transform.DOKill();
        button.transform.DOScale(1f, 0.16f).SetEase(Ease.OutQuad);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();

        if (label != null)
        {
            label.DOKill();
            label.DOFade(0.72f, 0.16f);
        }
    }

    private void PlayHoverSound()
    {
        if (_sfxSource == null)
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(hoverResource);

        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    private void PlayClickSound()
    {
        if (_sfxSource == null || _clickClip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(_clickClip, sfxVolume);
    }

    public void OnStartButtonClicked()
    {
        if (_leaving)
        {
            return;
        }

        _leaving = true;
        PlayClickSound();

        if (buttonGroup != null)
        {
            buttonGroup.interactable = false;
            buttonGroup.transform.DOScale(0.96f, 0.12f).SetEase(Ease.OutQuad);
            buttonGroup.DOFade(0f, 0.8f).SetDelay(0.2f);
        }

        if (titleGroup != null)
        {
            titleGroup.DOFade(0f, 0.8f);
        }

        if (_bgmSource != null && _bgmSource.clip != null)
        {
            _bgmSource.DOFade(0f, fadeOutDuration * 0.8f);
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOFade(1f, fadeOutDuration)
                            .SetDelay(0.3f)
                            .SetEase(Ease.InQuad)
                            .OnComplete(LoadGameScene);
        }
        else
        {
            LoadGameScene();
        }
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitButtonClicked()
    {
        if (_leaving)
        {
            return;
        }

        _leaving = true;
        PlayClickSound();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOFade(1f, 0.8f).SetEase(Ease.InQuad).OnComplete(Quit);
        }
        else
        {
            Quit();
        }
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
