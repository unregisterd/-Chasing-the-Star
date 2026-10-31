using UnityEngine;

[DisallowMultipleComponent]
public class EndingHintTrigger : MonoBehaviour
{
    [Tooltip("Localization/texts.json 里的文本 id")]
    [SerializeField] private string textId = "ending_jump_hint";
    [Tooltip("玩家进入这个半径就触发（米）")]
    [SerializeField] private float radius = 1.8f;
    [Tooltip("文字停留时间，0 = 一直显示到被替换")]
    [SerializeField] private float holdSeconds = 6f;
    [Tooltip("留空则自动查找玩家")]
    [SerializeField] private Transform playerOverride;

    private Transform _player;
    private bool _shown;

    private void Awake()
    {
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

    private void Update()
    {
        if (_shown || _player == null)
        {
            return;
        }

        Vector3 delta = _player.position - transform.position;
        delta.y *= 0.6f;

        if (delta.sqrMagnitude > radius * radius)
        {
            return;
        }

        _shown = true;

        if (UITextManager.Instance != null)
        {
            UITextManager.Instance.ShowText(Loc.Text(textId), holdSeconds, false);
        }
    }
}
