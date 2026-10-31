using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HeroTestBuilder
{
    private const string ModelPath = "Assets/Art/Hero/Leading/SM_leading_Rigged.fbx";
    private const string ControllerPath = "Assets/Animations/Player_Leading.controller";
    private const string ScenePath = "Assets/Scenes/Sandbox/HeroPlayTest.unity";

    [MenuItem("Tools/Hero/Build Hero Test")]
    public static void Build()
    {
        AnimationClip run = FindClip("Run");
        AnimationClip jump = FindClip("Jump");

        if (run == null || jump == null)
        {
            Debug.LogError("[HeroTest] 找不到 Run / Jump 动画，先跑 Tools/Hero/Setup Rigged Hero。");
            return;
        }

        AnimatorController controller = BuildController(run, jump);

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Selection.objects = new Object[0];
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateLight();
        CreateGround();

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        model.name = "Leading";
        model.transform.position = Vector3.zero;

        Animator animator = model.GetComponent<Animator>();

        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        HeroAnimTest tester = model.AddComponent<HeroAnimTest>();
        SerializedObject so = new SerializedObject(tester);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.ApplyModifiedPropertiesWithoutUndo();

        ApplySky();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[HeroTest] 已生成 " + ScenePath
                  + "\nPlay 后按：1 站立 / 2 跑步 / 3 跳跃 / 4 慢动作 / 5 半速。"
                  + "\n重点看：跑步的脚是否打滑、跳跃的起落是否自然、手臂和披风有没有被扭坏。");
    }

    private static AnimatorController BuildController(AnimationClip run, AnimationClip jump)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idle = machine.AddState("Idle");
        idle.motion = run;
        idle.speed = 0f;

        AnimatorState runState = machine.AddState("Run");
        runState.motion = run;
        runState.speed = 1f;

        AnimatorState jumpState = machine.AddState("Jump");
        jumpState.motion = jump;
        jumpState.speed = 1f;

        machine.defaultState = idle;

        AnimatorStateTransition toRun = idle.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition toIdle = runState.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition anyJump = machine.AddAnyStateTransition(jumpState);
        anyJump.hasExitTime = false;
        anyJump.duration = 0.05f;
        anyJump.canTransitionToSelf = false;
        anyJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        AnimatorStateTransition jumpToRun = jumpState.AddTransition(runState);
        jumpToRun.hasExitTime = true;
        jumpToRun.exitTime = 0.9f;
        jumpToRun.duration = 0.1f;
        jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idle);
        jumpToIdle.hasExitTime = true;
        jumpToIdle.exitTime = 0.9f;
        jumpToIdle.duration = 0.1f;
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip FindClip(string name)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        return assets
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == name && !c.name.StartsWith("__preview__"));
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 40f;
        cameraObject.transform.position = new Vector3(0f, 1.05f, -2.6f);
        cameraObject.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.86f);
        light.intensity = 1.3f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
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
