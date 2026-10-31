using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HeroSetup
{
    private const string Root = "Assets/Art/Hero/Leading";
    private const string FbxPath = Root + "/SM_leading.fbx";
    private const string BaseColor = Root + "/T_leading_BaseColor.tga";
    private const string NormalMap = Root + "/T_leading_Normal.tga";
    private const string MetalSmooth = Root + "/T_leading_MetallicSmoothness.tga";
    private const string MaterialFolder = "Assets/Materials";
    private const string MaterialPath = MaterialFolder + "/M_Leading.mat";
    private const string PrefabFolder = "Assets/Prefabs/Player";
    private const string PrefabPath = PrefabFolder + "/Player_Visual_Leading.prefab";
    private const string ScenePath = "Assets/Scenes/Sandbox/HeroPreview.unity";

    [MenuItem("Tools/Hero/Setup Leading")]
    public static void Setup()
    {
        if (!File.Exists(FbxPath))
        {
            Debug.LogError("[Hero] 找不到 " + FbxPath);
            return;
        }

        ConfigureTextures();
        Material material = BuildMaterial();
        GameObject prefab = BuildPrefab(material);

        if (prefab != null)
        {
            BuildPreviewScene(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Hero] 完成：\n  " + MaterialPath + "\n  " + PrefabPath + "\n  " + ScenePath
                  + "\n模型原始高度约 1.0 m，导入后不用缩放；朝向如果反了，把预制体 Y 旋转 180°。");
    }

    private static void ConfigureTextures()
    {
        SetTexture(BaseColor, true, false, TextureImporterType.Default, false);
        SetTexture(NormalMap, false, true, TextureImporterType.NormalMap, false);
        SetTexture(MetalSmooth, false, false, TextureImporterType.Default, false);
        SetTexture(Root + "/T_leading_Metallic.tga", false, false, TextureImporterType.Default, false);
        SetTexture(Root + "/T_leading_Roughness.tga", false, false, TextureImporterType.Default, false);
    }

    private static void SetTexture(string path, bool sRGB, bool normal, TextureImporterType type, bool alphaIsTransparency)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = type;
        importer.sRGBTexture = sRGB;
        importer.alphaIsTransparency = alphaIsTransparency;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;

        if (normal)
        {
            importer.textureType = TextureImporterType.NormalMap;
        }

        importer.SaveAndReimport();
    }

    private static Material BuildMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

        if (material == null)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;

        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColor);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalMap);
        Texture2D metalSmooth = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalSmooth);

        if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
        material.SetColor("_BaseColor", Color.white);

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }

        if (metalSmooth != null)
        {
            material.SetTexture("_MetallicGlossMap", metalSmooth);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_SmoothnessTextureChannel", 1f);
        }

        material.SetFloat("_Metallic", 1f);
        material.SetFloat("_Smoothness", 1f);
        material.SetFloat("_EnvironmentReflections", 1f);
        material.enableInstancing = true;

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject BuildPrefab(Material material)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);

        if (model == null)
        {
            Debug.LogError("[Hero] FBX 没有导入成功。");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Player_Visual_Leading";

        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("[Hero] 模型里没有 MeshRenderer。");
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Player");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void BuildPreviewScene(GameObject prefab)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Selection.objects = new Object[0];
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 40f;
        cameraObject.transform.position = new Vector3(0f, 1.15f, -2.4f);
        cameraObject.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.86f);
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2f, 1f, 2f);

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelInstance.transform.position = Vector3.zero;
        modelInstance.AddComponent<HeroTurntable>();

        Shader skyShader = Shader.Find("Sky/Gradient");

        if (skyShader != null)
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Sky_Dusk.mat");

            if (sky == null)
            {
                sky = new Material(skyShader);
                sky.SetColor("_TopColor", new Color(0.42f, 0.34f, 0.58f));
                sky.SetColor("_HorizonColor", new Color(1.00f, 0.79f, 0.64f));
                sky.SetColor("_GroundColor", new Color(0.22f, 0.18f, 0.26f));
                AssetDatabase.CreateAsset(sky, "Assets/Materials/M_Sky_Dusk.mat");
            }

            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.62f);
        RenderSettings.fog = false;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
    }
}
