using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HeroCharacterBuilder
{
    private const string ModelPath = "Assets/Art/Hero/Leading/SM_leading_Rigged.fbx";
    private const string ControllerPath = "Assets/Animations/Player_Leading.controller";
    private const string ActionsPath = "Assets/Settings/PlayerControls.inputactions";
    private const string PrefabFolder = "Assets/Prefabs/Player";
    private const string PrefabPath = PrefabFolder + "/Player_Leading.prefab";
    private const string ScenePath = "Assets/Scenes/Sandbox/HeroGameplayTest.unity";
    private const string BlueBlock = "Assets/Prefabs/Blocks/Block_Art_Blue.prefab";
    private const string RedBlock = "Assets/Prefabs/Blocks/Block_Art_Red.prefab";

    [MenuItem("Tools/Hero/Build Player Character")]
    public static void Build()
    {
        GameObject prefab = BuildPrefab();

        if (prefab == null)
        {
            return;
        }

        BuildScene(prefab);
    }

    private static GameObject BuildPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

        if (model == null)
        {
            Debug.LogError("[HeroChar] 找不到 " + ModelPath);
            return null;
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        GameObject root = new GameObject("Player_Leading");

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.0f;
        cc.center = new Vector3(0f, 0.5f, 0f);
        cc.radius = 0.24f;
        cc.stepOffset = 0.25f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.02f;

        PlayerInputReader reader = root.AddComponent<PlayerInputReader>();
        Object actions = AssetDatabase.LoadAssetAtPath<Object>(ActionsPath);

        if (actions != null)
        {
            SerializedObject readerSo = new SerializedObject(reader);
            readerSo.FindProperty("actions").objectReferenceValue = actions;
            readerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        root.AddComponent<AudioSource>();
        root.AddComponent<ThirdPersonMotor>();
        root.AddComponent<PlayerBlockAgent>();
        root.AddComponent<AbilityController>();

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        modelInstance.name = "Visual";
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;

        Animator animator = modelInstance.GetComponent<Animator>();

        if (animator == null)
        {
            animator = modelInstance.AddComponent<Animator>();
        }

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        animator.applyRootMotion = false;

        GameObject cameraRoot = new GameObject("PlayerCameraRoot");
        cameraRoot.transform.SetParent(root.transform, false);
        cameraRoot.transform.localPosition = new Vector3(0f, 0.72f, 0f);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Player");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void BuildScene(GameObject prefab)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Selection.objects = new Object[0];
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.transform.position = new Vector3(0f, 0.3f, -5f);

        Transform cameraRoot = player.transform.Find("PlayerCameraRoot");
        CreateCameraRig(cameraRoot);

        CreateLight();
        CreateGround();
        BuildCourse();

        ApplySky();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[HeroChar] 已生成：\n  " + PrefabPath + "\n  " + ScenePath
                  + "\nPlay：WASD 移动（相机相对）/ Shift 冲刺 / Space 跳跃 / 1-4 技能 / Esc 暂停。");
    }

    private static void CreateCameraRig(Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 45f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CinemachineBrain>();

        GameObject vcamObject = new GameObject("Player Follow Camera");
        CinemachineVirtualCamera vcam = vcamObject.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 10;
        vcam.Follow = target;
        vcam.LookAt = target;
        vcam.m_Lens.FieldOfView = 45f;

        CinemachineTransposer transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        transposer.m_FollowOffset = new Vector3(0f, 0.55f, -2.1f);
        transposer.m_XDamping = 0.12f;
        transposer.m_YDamping = 0.12f;
        transposer.m_ZDamping = 0.12f;

        CinemachineComposer composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0f, 0.35f, 0f);

        CinemachineCollider collider = vcamObject.AddComponent<CinemachineCollider>();
        collider.m_MinimumDistanceFromTarget = 0.4f;
        collider.m_CameraRadius = 0.15f;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.86f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
    }

    private static void BuildCourse()
    {
        Transform course = new GameObject("Course").transform;

        for (int i = 0; i < 5; i++)
        {
            SpawnBlock(course, new Vector3(0f, 0.5f, 1f + i * 1.5f), i % 2 == 0);
        }

        for (int i = 0; i < 4; i++)
        {
            SpawnBlock(course, new Vector3(2.6f, 0.5f + i, 1f + i * 1.5f), true);
        }

        for (int i = 0; i < 3; i++)
        {
            SpawnBlock(course, new Vector3(-2.6f, 0.5f + i, 4f), false);
        }
    }

    private static void SpawnBlock(Transform parent, Vector3 position, bool blue)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(blue ? BlueBlock : RedBlock);

        if (prefab == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Block";
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.identity;
    }

    private static void ApplySky()
    {
        Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Sky_Dusk.mat");

        if (sky != null)
        {
            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.62f);
        RenderSettings.fog = false;
    }
}
