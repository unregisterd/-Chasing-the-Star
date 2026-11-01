using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EscapeMenuController : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private CanvasGroup skillGroup;

    [Header("遮罩呼吸")]
    [Tooltip("留空会自动找 Menu Root/Dim")]
    [SerializeField] private Image dimImage;
    [Tooltip("最暗时的 alpha（0~255）")]
    [Range(0f, 255f)] [SerializeField] private float dimAlphaMin = 25f;
    [Tooltip("最亮时的 alpha（0~255）")]
    [Range(0f, 255f)] [SerializeField] private float dimAlphaMax = 150f;
    [Tooltip("一次明暗循环的秒数")]
    [SerializeField] private float dimBreathPeriod = 5f;

    [Header("右侧导航")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Image[] navDiamonds = new Image[4];

    [Header("技能图标")]
    [SerializeField] private Button[] skillButtons = new Button[5];
    [SerializeField] private Image[] skillDiamonds = new Image[5];

    [Header("文案")]
    [SerializeField] private string[] skillKeys =
    {
        "ability_courage_desc",
        "ability_wisdom_desc",
        "ability_patience_desc",
        "ability_hope_desc",
        "skill_shift_desc",
        "skill_R_desc"
    };

    [SerializeField] private string[] navKeys =
    {
        "esc_resume",
        "esc_skills",
        "esc_menu",
        "esc_quit",
    };

    [SerializeField] private string[] skillLabelKeys =
    {
        "esc_skill_courage",
        "esc_skill_wisdom",
        "esc_skill_patience",
        "esc_skill_hope",
        "esc_skill_shift",
        "esc_skill_R",
    };

    [SerializeField] private string defaultDetail = "把鼠标移到技能上，看看它能为骑士做什么。";

    [Header("表现")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private float fadeDuration = 0.22f;
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.63f, 1f);
    [SerializeField] private float pausedMusicVolume = 0.4f;
    [SerializeField] private string mainMenuScene = "MainMenu";

    private readonly List<Button> _navButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> _navLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> _skillLabels = new List<TextMeshProUGUI>();
    private readonly bool[] _navHoverTarget = new bool[4];
    private bool[] _skillHoverTarget = new bool[0];
    private readonly float[] _navHover = new float[4];
    private float[] _skillHover = new float[0];

    private bool _open;
    private bool _skillsExpanded;
    private bool _transitioning;
    private float _transitionEnd;
    private float _alpha;
    private float _skillsAlpha;
    private int _currentSkill = -1;

    private PlayerInputReader _input;
    private AbilityController _abilities;
    private Hud_StarEnergy _hud;
    private PlayerRespawnManager _respawn;
    private EndingDirector _ending;

    public bool IsOpen => _open;

    private void Awake()
    {
        _skillHover = new float[Mathf.Max(1, skillButtons.Length)];
        _skillHoverTarget = new bool[Mathf.Max(1, skillButtons.Length)];

        _navButtons.Add(resumeButton);
        _navButtons.Add(skillsButton);
        _navButtons.Add(mainMenuButton);
        _navButtons.Add(quitButton);

        for (int i = 0; i < _navButtons.Count; i++)
        {
            Button button = _navButtons[i];

            if (button == null)
            {
                _navLabels.Add(null);
                continue;
            }

            TextMeshProUGUI navLabel = button.GetComponentInChildren<TextMeshProUGUI>();

            if (navLabel != null)
            {
                navLabel.raycastTarget = false;
            }

            _navLabels.Add(navLabel);
            AddHover(button, i, true);

            switch (i)
            {
                case 0: button.onClick.AddListener(Close); break;
                case 1: button.onClick.AddListener(ToggleSkills); break;
                case 2: button.onClick.AddListener(ReturnToMainMenu); break;
                case 3: button.onClick.AddListener(QuitGame); break;
            }
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];

            if (button == null)
            {
                _skillLabels.Add(null);
                continue;
            }

            int index = i;
            TextMeshProUGUI skillLabel = button.GetComponentInChildren<TextMeshProUGUI>();

            if (skillLabel != null)
            {
                skillLabel.raycastTarget = false;
            }

            _skillLabels.Add(skillLabel);
            AddHover(button, i, false);
            button.onClick.AddListener(() => ShowSkill(index));
        }

        if (skillGroup != null)
        {
            skillGroup.alpha = 0f;
            skillGroup.blocksRaycasts = false;
        }

        if (menuGroup != null)
        {
            menuGroup.alpha = 0f;
            menuGroup.blocksRaycasts = false;
            menuGroup.interactable = false;
        }

        if (detailText != null)
        {
            detailText.text = defaultDetail;
        }

        _input = FindObjectOfType<PlayerInputReader>(true);
        _abilities = FindObjectOfType<AbilityController>(true);
        _hud = FindObjectOfType<Hud_StarEnergy>(true);
        _respawn = FindObjectOfType<PlayerRespawnManager>(true);
        _ending = FindObjectOfType<EndingDirector>(true);

        Loc.LanguageChanged += RefreshTexts;
        RefreshTexts();
    }

    private void Start()
    {
        if (!lockCursorOnStart)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        Loc.LanguageChanged -= RefreshTexts;
    }

    private void RefreshTexts()
    {
        for (int i = 0; i < _navLabels.Count && i < navKeys.Length; i++)
        {
            if (_navLabels[i] != null)
            {
                _navLabels[i].text = Loc.Text(navKeys[i]);
            }
        }

        for (int i = 0; i < _skillLabels.Count && i < skillLabelKeys.Length; i++)
        {
            if (_skillLabels[i] != null)
            {
                _skillLabels[i].text = Loc.Text(skillLabelKeys[i]);
            }
        }

        defaultDetail = Loc.Text("esc_default");

        if (_currentSkill >= 0)
        {
            ShowSkill(_currentSkill);
        }
        else
        {
            ResetDetail();
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (_transitioning && Time.unscaledTime >= _transitionEnd)
        {
            _transitioning = false;
        }

        if (!_transitioning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        UpdateHover(dt);
        UpdateFade(dt);
        UpdateDimBreath();
    }

    private void UpdateHover(float dt)
    {
        for (int i = 0; i < 4; i++)
        {
            float navTarget = _open && _navHoverTarget[i] ? 1f : 0f;
            _navHover[i] = Mathf.MoveTowards(_navHover[i], navTarget, dt * 8f);

            if (i < _navLabels.Count && _navLabels[i] != null)
            {
                _navLabels[i].color = Color.Lerp(idleColor, hoverColor, _navHover[i]);
            }

            if (i < navDiamonds.Length && navDiamonds[i] != null)
            {
                Color color = navDiamonds[i].color;
                color.a = _navHover[i];
                navDiamonds[i].color = color;
            }
        }

        int skillCount = Mathf.Min(_skillHover.Length, skillButtons.Length);

        for (int i = 0; i < skillCount; i++)
        {
            float skillTarget = _open && _skillsExpanded && _skillHoverTarget[i] ? 1f : 0f;
            _skillHover[i] = Mathf.MoveTowards(_skillHover[i], skillTarget, dt * 8f);

            if (i < _skillLabels.Count && _skillLabels[i] != null)
            {
                _skillLabels[i].color = Color.Lerp(new Color(1f, 1f, 1f, 0.62f), hoverColor, _skillHover[i]);
            }

            if (i < skillDiamonds.Length && skillDiamonds[i] != null)
            {
                Color color = skillDiamonds[i].color;
                color.a = _skillHover[i];
                skillDiamonds[i].color = color;
            }
        }

        float skillsTarget = _open && _skillsExpanded ? 1f : 0f;
        _skillsAlpha = Mathf.MoveTowards(_skillsAlpha, skillsTarget, dt * 8f);

        if (skillGroup != null)
        {
            skillGroup.alpha = _skillsAlpha;
            skillGroup.blocksRaycasts = _skillsAlpha > 0.5f;
        }
    }

    private void UpdateDimBreath()
    {
        if (dimImage == null)
        {
            Transform found = transform.Find("Menu Root/Dim");

            if (found != null)
            {
                dimImage = found.GetComponent<Image>();
            }

            if (dimImage == null)
            {
                return;
            }
        }

        float period = Mathf.Max(0.5f, dimBreathPeriod);
        float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(dimAlphaMin, dimAlphaMax, t) / 255f;

        Color color = dimImage.color;
        color.a = alpha;
        dimImage.color = color;
    }

    private void UpdateFade(float dt)
    {
        _alpha = Mathf.MoveTowards(_alpha, _open ? 1f : 0f, dt / Mathf.Max(0.01f, fadeDuration));

        if (menuGroup != null)
        {
            menuGroup.alpha = _alpha;
            menuGroup.blocksRaycasts = _alpha > 0.5f;
            menuGroup.interactable = _alpha > 0.5f;
        }
    }

    private void AddHover(Button button, int index, bool isNav)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnHoverEnter(index, isNav));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => OnHoverExit(index, isNav));
        trigger.triggers.Add(exit);
    }

    private void OnHoverEnter(int index, bool isNav)
    {
        if (index < 0 || index >= 5)
        {
            return;
        }

        if (isNav)
        {
            _navHoverTarget[index] = true;
        }
        else
        {
            _skillHoverTarget[index] = true;
        }
    }

    private void OnHoverExit(int index, bool isNav)
    {
        if (index < 0 || index >= 5)
        {
            return;
        }

        if (isNav)
        {
            _navHoverTarget[index] = false;
        }
        else
        {
            _skillHoverTarget[index] = false;
        }
    }

    private void ToggleSkills()
    {
        if (_skillsExpanded)
        {
            CollapseSkills();
        }
        else
        {
            _skillsExpanded = true;
        }
    }

    private void CollapseSkills()
    {
        _skillsExpanded = false;

        for (int i = 0; i < _skillHoverTarget.Length; i++)
        {
            _skillHoverTarget[i] = false;
        }

        ResetDetail();
    }

    private void ShowSkill(int index)
    {
        if (detailText == null)
        {
            return;
        }

        if (index < 0 || index >= skillKeys.Length)
        {
            ResetDetail();
            return;
        }

        _currentSkill = index;
        string text = Loc.Text(skillKeys[index]);
        detailText.text = string.IsNullOrEmpty(text) ? defaultDetail : text;
    }

    private void ResetDetail()
    {
        _currentSkill = -1;
        if (detailText != null)
        {
            detailText.text = defaultDetail;
        }
    }

    private void Open()
    {
        if (_respawn != null && _respawn.IsBusy)
        {
            return;
        }

        if (_ending != null && _ending.IsPlaying)
        {
            return;
        }

        if (EndingSequence.IsPlaying)
        {
            return;
        }

        _open = true;
        _transitioning = true;
        _transitionEnd = Time.unscaledTime + fadeDuration;

        TimeController.SetPaused(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_input != null) _input.enabled = false;
        if (_abilities != null) _abilities.enabled = false;
        if (_hud != null) _hud.SetVisible(false);

        AudioListener.volume = pausedMusicVolume;
        ResetDetail();
    }

    private void Close()
    {
        if (!_open)
        {
            return;
        }

        _open = false;
        _skillsExpanded = false;
        _transitioning = true;
        _transitionEnd = Time.unscaledTime + fadeDuration;

        TimeController.SetPaused(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_input != null) _input.enabled = true;
        if (_abilities != null) _abilities.enabled = true;
        if (_hud != null) _hud.SetVisible(true);

        AudioListener.volume = 1f;
        ResetDetail();
    }

    private void ReturnToMainMenu()
    {
        TimeController.SetPaused(false);
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void QuitGame()
    {
        TimeController.SetPaused(false);
        AudioListener.volume = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        if (!_open)
        {
            return;
        }

        _open = false;
        TimeController.SetPaused(false);
        AudioListener.volume = 1f;
    }
}
