using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelEndingHint
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Level/Add Ending Hint")]
    public static void Add()
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
            Debug.LogError("[EndingHint] 场景里没有 Block。");
            return;
        }

        Block last = blocks
            .Where(b => b != null)
            .OrderByDescending(b => b.GetTopHeight())
            .First();

        EndingHintTrigger existing = last.GetComponent<EndingHintTrigger>();

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        EndingHintTrigger trigger = last.gameObject.AddComponent<EndingHintTrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("textId").stringValue = "ending_jump_hint";
        so.FindProperty("radius").floatValue = 1.8f;
        so.FindProperty("holdSeconds").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[EndingHint] 已挂在最高方块 " + last.name + "（顶面 "
                  + last.GetTopHeight().ToString("0.0") + "m），玩家靠近会显示「再跳一步」。");
    }
}
