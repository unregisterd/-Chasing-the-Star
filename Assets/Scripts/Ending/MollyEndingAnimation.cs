// molly | Ending fall, dark star field, remembered blocks and music.
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MollyEndingAnimation : MonoBehaviour
{
    [SerializeField] private float duration = 25f;
    [SerializeField] private float gravity = 3.2f;
    [SerializeField] private float lookUpSeconds = 5f;
    [SerializeField] private float musicVolume = .55f;
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private string returnScene = "MainMenu";
    private readonly List<Vector3> visited = new List<Vector3>();
    private PlayerBlockAgent agent;
    private Camera cameraView;
    private Transform player;
    private Transform oldParent;
    private Vector3 oldLocalPosition;
    private Quaternion oldLocalRotation;
    private float oldFarClip;
    private CinemachineBrain brain;
    private ThirdPersonCameraOrbit orbit;
    private Renderer oldBackdrop;
    private bool oldBackdropEnabled;
    private SkyMoodController mood;
    private AudioSource[] oldMusic;
    private AudioSource endingMusic;
    private GameObject skyQuad;
    private Vector3 skyBaseScale;
    private Material skyMaterial;
    private Texture2D lightTexture;
    private Vector3 startPosition, center;
    private Quaternion startRotation;
    private float elapsed;
    private bool playing;

    private void Awake()
    {
        agent = FindObjectOfType<PlayerBlockAgent>();
        if (agent) agent.StandingBlockChanged += Remember;
    }
    private void Remember(Block block)
    {
        if (!playing && block && (visited.Count == 0 || Vector3.Distance(visited[visited.Count - 1], block.transform.position) > .2f))
            visited.Add(block.transform.position);
    }
    public IEnumerator Play(Transform fallingPlayer)
    {
        cameraView = Camera.main;
        player = fallingPlayer;
        if (!player || !cameraView) { Debug.LogWarning("[molly Ending] Missing player or camera."); yield break; }
        Debug.Log("[molly Ending] Starfall started.");
        playing = true;
        elapsed = 0;
        startPosition = player.position;
        oldParent = cameraView.transform.parent;
        oldLocalPosition = cameraView.transform.localPosition;
        oldLocalRotation = cameraView.transform.localRotation;
        oldFarClip = cameraView.farClipPlane;
        brain = cameraView.GetComponent<CinemachineBrain>();
        if (brain) brain.enabled = false;
        orbit = FindObjectOfType<ThirdPersonCameraOrbit>();
        if (orbit) orbit.enabled = false;
        cameraView.transform.SetParent(player, false);
        cameraView.transform.localPosition = new Vector3(0, 1.25f, 0);
        cameraView.transform.rotation = Quaternion.LookRotation(player.forward, Vector3.up);
        startRotation = cameraView.transform.rotation;
        cameraView.farClipPlane = Mathf.Max(oldFarClip, 2000f);
        center = visited.Count > 0 ? Vector3.zero : startPosition + player.forward * 4f;
        if (visited.Count > 0) { foreach (var point in visited) center += point; center /= visited.Count; }
        // molly | Keep the existing level's 3D platforms visible in front of the star image.
        mood = SkyMoodController.Instance;
        if (mood) {
            mood.enabled = false;
            oldMusic = mood.GetComponents<AudioSource>();
            foreach (var source in oldMusic) if (source.isPlaying) source.Pause();
        }
        // molly | Prevent the level fog from washing out the dark star texture.
        RenderSettings.fog = false;
        GameObject existingBackdrop = GameObject.Find("Backdrop");
        if (existingBackdrop) { oldBackdrop = existingBackdrop.GetComponent<Renderer>(); if (oldBackdrop) { oldBackdropEnabled = oldBackdrop.enabled; oldBackdrop.enabled = false; } }
        Texture2D sky = Resources.Load<Texture2D>("Sky/Backdrop_Starry");
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Debug.Log("[molly Ending] Sky image=" + (sky ? sky.name : "missing") + ", shader=" + (shader ? shader.name : "missing"));
        if (sky && shader) {
            skyMaterial = new Material(shader); skyMaterial.mainTexture = sky; skyMaterial.SetTexture("_BaseMap", sky); skyMaterial.SetColor("_BaseColor", new Color(.85f, .9f, 1f, 1f));
            // molly | Use only the upper star field; the mountain strip stays outside the UV window.
            skyMaterial.SetTextureScale("_BaseMap", new Vector2(1f, .6f));
            skyMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, .4f));
            skyQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyQuad.name = "MollyEndingDarkSky";
            Destroy(skyQuad.GetComponent<Collider>());
            skyQuad.GetComponent<Renderer>().sharedMaterial = skyMaterial;
            skyQuad.transform.SetParent(cameraView.transform, false);
            skyQuad.transform.localPosition = new Vector3(0, 0, 800);
            skyQuad.transform.localRotation = Quaternion.identity;
            float height = 1600 * Mathf.Tan(cameraView.fieldOfView * .5f * Mathf.Deg2Rad);
            skyQuad.transform.localScale = new Vector3(height * cameraView.aspect * 2.6f, height * 2.6f, 1);
            skyBaseScale = skyQuad.transform.localScale;
        }
        AudioClip clip = Resources.Load<AudioClip>("Audio/BGM/ending");
        if (clip) {
            GameObject musicObject = new GameObject("MollyEndingMusic");
            DontDestroyOnLoad(musicObject);
            endingMusic = musicObject.AddComponent<AudioSource>();
            endingMusic.clip = clip; endingMusic.spatialBlend = 0; endingMusic.volume = musicVolume;
            endingMusic.Play();
        } else {
            Debug.LogWarning("[molly Ending] Ending song is missing.");
        }
        // molly | Stop the old fall-respawn check while the cinematic controls the body.
        if (agent) { agent.StopAllCoroutines(); agent.enabled = false; }
        PlayerRespawnManager respawnManager = player.GetComponent<PlayerRespawnManager>();
        if (respawnManager) { respawnManager.StopAllCoroutines(); respawnManager.enabled = false; }
        if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlpha(0f);
        var motor = player.GetComponent<ThirdPersonMotor>();
        var firstMotor = player.GetComponent<FirstPersonMotor>();
        var controller = player.GetComponent<CharacterController>();
        bool motorEnabled = motor && motor.enabled;
        bool firstEnabled = firstMotor && firstMotor.enabled;
        bool controllerEnabled = controller && controller.enabled;
        if (motor) motor.enabled = false;
        if (firstMotor) firstMotor.enabled = false;
        if (controller) controller.enabled = false;
        bool finalLineShown = false;
        while (elapsed < duration) {
            // molly | Let the player skip the fall instead of being locked in for the whole duration.
            if (allowSkip && SkipPressed()) elapsed = Mathf.Max(elapsed, duration - 4f);
            elapsed += Time.deltaTime;
            if (!finalLineShown && elapsed >= duration - 4f) {
                MollyEndingFinalLine.Show();
                finalLineShown = true;
            }
            UpdateSkyMotion(elapsed);
            player.position = startPosition + Vector3.down * (.5f * Mathf.Max(.1f, gravity) * elapsed * elapsed);
            Vector3 direction = center - cameraView.transform.position;
            if (direction.sqrMagnitude > 1) {
                Vector3 up = Vector3.ProjectOnPlane(player.forward, direction.normalized);
                if (up.sqrMagnitude < .01f) up = Vector3.forward;
                Quaternion lookingUp = Quaternion.LookRotation(direction.normalized, up.normalized);
                cameraView.transform.rotation = Quaternion.Slerp(startRotation, lookingUp, Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.1f, lookUpSeconds)));
            }
            yield return null;
        }
        // molly | Hold this camera and star field so the ending has no hard screen change.
        ShowContinuePrompt(true);
        // molly | Give the player a way out instead of trapping them on the ending screen.
        float readyAt = Time.unscaledTime + 1.5f;
        while (Time.unscaledTime < readyAt) yield return null;
        while (!SkipPressed()) yield return null;
        ShowContinuePrompt(false);
        if (ScreenFader.Instance != null) yield return ScreenFader.Instance.Fade(1f, .8f);
        SceneManager.LoadScene(returnScene);
    }
    private void UpdateSkyMotion(float time)
    {
        if (skyQuad == null)
        {
            return;
        }

        float fall = .5f * Mathf.Max(.1f, gravity) * time * time;
        float maxDrift = skyBaseScale.y * 0.15f;
        float drift = Mathf.Min(fall * 0.8f, maxDrift);

        skyQuad.transform.localPosition = new Vector3(
            Mathf.Sin(time * 0.15f) * 14f,
            drift,
            800f);

        float roll = time * 0.3f + Mathf.Sin(time * 0.1f) * 2f;
        skyQuad.transform.localRotation = Quaternion.Euler(0f, 0f, roll);

        float scaleMul = 1f + time * 0.008f;
        skyQuad.transform.localScale = skyBaseScale * scaleMul;
    }

    private static bool SkipPressed()
    {
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
    }
    private static void ShowContinuePrompt(bool visible)
    {
        if (UITextManager.Instance == null) return;
        if (visible) UITextManager.Instance.ShowText(Loc.Text("ending_continue"), 0f, true);
        else UITextManager.Instance.HideText();
    }
    private void OnGUI()
    {
        if (!playing || !cameraView) return;
        if (!lightTexture) {
            lightTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) {
                float radius = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16;
                lightTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 3)));
            }
            lightTexture.Apply();
        }
        Color original = GUI.color;
        float gather = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(duration * .45f, duration * .9f, elapsed));
        foreach (var position in visited) DrawLight(Vector3.Lerp(position, center, gather), (1 - gather) * .8f, 18);
        DrawLight(center, Mathf.Clamp01((gather - .3f) / .7f), Mathf.Lerp(20, 45, gather));
        GUI.color = original;
    }
    private void DrawLight(Vector3 world, float alpha, float size)
    {
        Vector3 screen = cameraView.WorldToScreenPoint(world);
        if (screen.z <= 0 || alpha <= 0) return;
        float y = Screen.height - screen.y;
        GUI.color = new Color(1, .88f, .65f, alpha);
        GUI.DrawTexture(new Rect(screen.x - size, y - size, size * 2, size * 2), lightTexture);
        GUI.color = new Color(1, .98f, .91f, alpha);
        GUI.DrawTexture(new Rect(screen.x - 3, y - 3, 6, 6), lightTexture);
    }
    private void OnDestroy()
    {
        if (agent) agent.StandingBlockChanged -= Remember;
        if (lightTexture) Destroy(lightTexture);
    }
}
