// molly | Final realization fades in over the same star-filled ending view.
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MollyEndingFinalLine : MonoBehaviour
{
    private const string LineId = "ending_final_line";
    private const string FallbackLine = "原来，那颗永不熄灭的星星，\n就是我走过的路。";
    private static MollyEndingFinalLine active;
    private float started;
    private bool shown;
    private bool useOnGui;
    public static void Show() {
        if (!active) new GameObject("MollyEndingFinalLine").AddComponent<MollyEndingFinalLine>();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register() => SceneManager.sceneLoaded += OnSceneLoaded;
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainEnding") { Show(); return; }
        GameObject oldMusic = GameObject.Find("MollyEndingMusic");
        if (oldMusic) Destroy(oldMusic);
    }
    private void Awake() { active = this; started = Time.unscaledTime; }
    private void OnDestroy() { if (active == this) active = null; }
    private static string ResolveLine()
    {
        string line = Loc.Text(LineId);
        return string.IsNullOrEmpty(line) || line == LineId ? FallbackLine : line;
    }
    private void Update()
    {
        if (shown || Time.unscaledTime - started < 1f) return;
        shown = true;
        // molly | Route the line through the game's own text layer so font and layering match.
        if (UITextManager.Instance != null) { UITextManager.Instance.ShowCenter(ResolveLine(), 0f, true); return; }
        useOnGui = true;
    }
    private void OnGUI()
    {
        if (!useOnGui) return;
        float fade = Mathf.Clamp01((Time.unscaledTime - started - 1f) / 2f);
        if (fade <= 0) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.RoundToInt(Screen.height * .032f);
        style.wordWrap = true;
        Color previous = GUI.color;
        GUI.color = new Color(1f, .96f, .86f, fade);
        GUI.Label(new Rect(Screen.width * .12f, Screen.height * .7f, Screen.width * .76f, Screen.height * .16f), ResolveLine(), style);
        GUI.color = previous;
    }
}
