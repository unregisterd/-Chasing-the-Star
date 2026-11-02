using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DistantSilhouetteBuilder
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string MaterialPath = "Assets/Art/Materials/M_Silhouette.mat";
    private const string RootName = "DistantSilhouettes";

    private static readonly Color SilhouetteColor = new Color(0.055f, 0.06f, 0.10f, 1f);

    [MenuItem("Tools/Polish/Add Distant Silhouettes")]
    public static void Build()
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

        GameObject existing = GameObject.Find(RootName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        Material material = GetOrCreateMaterial();
        GameObject root = new GameObject(RootName);
        Transform parent = root.transform;

        BuildBrokenArch(parent, material, new Vector3(-34f, -6f, 62f), 0f, 1.0f);
        BuildFallenStatue(parent, material, new Vector3(41f, -8f, 74f), 18f, 1.0f);
        BuildSpire(parent, material, new Vector3(6f, -10f, 86f), 0f, 1.35f);
        BuildMonolith(parent, material, new Vector3(-15f, -7f, 79f), -12f, 0.9f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[Silhouette] 已在雾的尽头放置 4 组远景剪影（拱门/倒像/尖塔/石碑）。"
                  + "\n它们没有碰撞体、不参与雾，呈纯暗色剪影；想更明显可加大 Scale 或靠近点。");
    }

    private static Material GetOrCreateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("FX/Silhouette");

        if (shader == null)
        {
            Debug.LogWarning("[Silhouette] 找不到 FX/Silhouette shader，退回 Unlit。");
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        material = new Material(shader);
        material.SetColor("_Color", SilhouetteColor);
        material.SetColor("_BaseColor", SilhouetteColor);

        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        }

        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void BuildBrokenArch(Transform parent, Material material, Vector3 origin, float yaw, float scale)
    {
        Transform group = NewGroup(parent, "BrokenArch", origin, yaw, scale);

        AddBlock(group, material, new Vector3(-7f, 9f, 0f), new Vector3(6f, 18f, 6f));
        AddBlock(group, material, new Vector3(7f, 6f, 0f), new Vector3(6f, 12f, 6f));
        AddBlock(group, material, new Vector3(0.5f, 19.5f, 0f), new Vector3(9f, 4f, 6f), new Vector3(0f, 0f, 6f));
        AddBlock(group, material, new Vector3(-7f, 0.5f, 0f), new Vector3(9f, 2f, 9f));
    }

    private static void BuildFallenStatue(Transform parent, Material material, Vector3 origin, float yaw, float scale)
    {
        Transform group = NewGroup(parent, "FallenStatue", origin, yaw, scale);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Configure(body, group, material, new Vector3(0f, 4f, 0f), new Vector3(9f, 9f, 9f), new Vector3(0f, 0f, 78f));

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Configure(head, group, material, new Vector3(-9f, 7.5f, 0f), new Vector3(7f, 7f, 7f), Vector3.zero);

        AddBlock(group, material, new Vector3(10f, 1f, 0f), new Vector3(14f, 2f, 8f));
    }

    private static void BuildSpire(Transform parent, Material material, Vector3 origin, float yaw, float scale)
    {
        Transform group = NewGroup(parent, "Spire", origin, yaw, scale);

        AddBlock(group, material, new Vector3(0f, 12f, 0f), new Vector3(4f, 24f, 4f));
        AddBlock(group, material, new Vector3(0f, 26f, 0f), new Vector3(2.2f, 6f, 2.2f), new Vector3(0f, 45f, 0f));
        AddBlock(group, material, new Vector3(0f, 0.5f, 0f), new Vector3(10f, 2f, 10f));
    }

    private static void BuildMonolith(Transform parent, Material material, Vector3 origin, float yaw, float scale)
    {
        Transform group = NewGroup(parent, "Monolith", origin, yaw, scale);

        AddBlock(group, material, new Vector3(0f, 10f, 0f), new Vector3(5f, 20f, 3f), new Vector3(0f, 0f, 7f));
        AddBlock(group, material, new Vector3(5.5f, 3f, 0f), new Vector3(4f, 6f, 3f), new Vector3(0f, 0f, -14f));
    }

    private static Transform NewGroup(Transform parent, string name, Vector3 position, float yaw, float scale)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.position = position;
        group.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        group.transform.localScale = Vector3.one * scale;
        return group.transform;
    }

    private static void AddBlock(Transform parent, Material material, Vector3 localPosition, Vector3 size)
    {
        AddBlock(parent, material, localPosition, size, Vector3.zero);
    }

    private static void AddBlock(Transform parent, Material material, Vector3 localPosition, Vector3 size, Vector3 euler)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Configure(cube, parent, material, localPosition, size, euler);
    }

    private static void Configure(GameObject instance, Transform parent, Material material,
        Vector3 localPosition, Vector3 scale, Vector3 euler)
    {
        Collider collider = instance.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localScale = scale;
        instance.transform.localRotation = Quaternion.Euler(euler);

        Renderer renderer = instance.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = false;
        }
    }
}
