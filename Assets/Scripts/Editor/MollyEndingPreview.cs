// molly | Editor-only preview for the new third-person ending.
using UnityEditor;
using UnityEngine;
public static class MollyEndingPreview
{
    [MenuItem("Tools/Molly/Preview Starfall")]
    private static void Preview()
    {
        Debug.Log("[molly Ending] Preview menu clicked.");
        if (!EditorApplication.isPlaying) { Debug.LogWarning("[molly Ending] Press Play first."); return; }
        MollyEndingAnimation animation = Object.FindObjectOfType<MollyEndingAnimation>();
        PlayerBlockAgent player = Object.FindObjectOfType<PlayerBlockAgent>();
        if (!animation || !player) { Debug.LogWarning("[molly Ending] Open Main and press Play before preview."); return; }
        PlayerRespawnManager respawn = player.GetComponent<PlayerRespawnManager>();
        if (respawn) { respawn.StopAllCoroutines(); respawn.enabled = false; }
        player.StopAllCoroutines();
        player.SetAutoFallRespawn(false);
        Hud_StarEnergy hud = Object.FindObjectOfType<Hud_StarEnergy>();
        if (hud) hud.SetVisible(false);
        if (UITextManager.Instance != null) {
            UITextManager.Instance.SetControlsVisible(false);
            UITextManager.Instance.ShowText(string.Empty, 0f, false);
            UITextManager.Instance.HideCenter();
        }
        animation.StartCoroutine(animation.Play(player.transform));
    }
}





