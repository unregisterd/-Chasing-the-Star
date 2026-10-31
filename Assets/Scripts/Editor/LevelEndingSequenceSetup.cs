using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelEndingSequenceSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string EndingScenePath = "Assets/Scenes/MainEnding.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Level/Setup Ending Sequence")]
    public static void Setup()
    {
        Selection.objects = new Object[0];

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Block[] blocks = Object.FindObjectsOfType<Block>(true);

        if (blocks.Length == 0)
        {
            Debug.LogError("[Ending] 场景里没有 Block。");
            return;
        }

        Block last = blocks
            .Where(b => b != null)
            .OrderByDescending(b => b.GetTopHeight())
            .First();

        EndingHintTrigger oldHint = last.GetComponent<EndingHintTrigger>();

        if (oldHint != null)
        {
            Object.DestroyImmediate(oldHint);
        }

        EndingSequence existing = last.GetComponent<EndingSequence>();

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        EndingSequence sequence = last.gameObject.AddComponent<EndingSequence>();

        SerializedObject so = new SerializedObject(sequence);
        so.FindProperty("narrationId").stringValue = "ending_narration";
        so.FindProperty("hintId").stringValue = "ending_jump_hint";
        so.FindProperty("narrationHold").floatValue = 6f;
        so.FindProperty("fallLatchDistance").floatValue = 1.5f;
        so.FindProperty("voidDropDistance").floatValue = 6f;
        so.FindProperty("fadeDuration").floatValue = 1.2f;
        so.FindProperty("blackHold").floatValue = 0.3f;
        so.FindProperty("nextScene").stringValue = "MainEnding";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        RegisterScenes();

        Debug.Log("[Ending] 已挂在最高方块 " + last.name + "（顶面 " + last.GetTopHeight().ToString("0.0") + "m）"
                  + "\n流程：靠近 → 长旁白 → 6s 后「再跳一步」→ 下坠超过 6m → 黑屏 1.2s → MainEnding"
                  + "\nMainEnding 已加入 Build Settings。");
    }

    private static void RegisterScenes()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        AddOrMoveFirst(scenes, MenuScenePath, 0);
        AddOrMoveFirst(scenes, ScenePath, 1);

        scenes.RemoveAll(s => s.path == EndingScenePath);
        scenes.Insert(Mathf.Min(2, scenes.Count), new EditorBuildSettingsScene(EndingScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddOrMoveFirst(List<EditorBuildSettingsScene> scenes, string path, int index)
    {
        scenes.RemoveAll(s => s.path == path);
        scenes.Insert(Mathf.Min(index, scenes.Count), new EditorBuildSettingsScene(path, true));
    }
}
