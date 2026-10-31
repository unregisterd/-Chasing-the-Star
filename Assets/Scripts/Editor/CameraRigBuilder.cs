using Cinemachine;
using UnityEditor;
using UnityEngine;

public static class CameraRigBuilder
{
    private const string Folder = "Assets/Prefabs/Camera";
    private const string PrefabPath = Folder + "/ThirdPersonCameraRig.prefab";

    [MenuItem("Tools/Hero/Build Camera Rig Prefab")]
    public static void BuildPrefab()
    {
        GameObject root = BuildRigObject();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Camera");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Selection.objects = new Object[0];
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        Debug.Log("[CameraRig] 已生成 " + PrefabPath
                  + "\n拖进任何场景即可：会自动找场景里的玩家，并把 PlayerCameraRoot 绑给相机。");
    }

    [MenuItem("Tools/Hero/Place Camera Rig In Scene")]
    public static void PlaceInScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            BuildPrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        if (prefab == null)
        {
            Debug.LogError("[CameraRig] 预制体创建失败。");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "ThirdPersonCameraRig";

        Undo.RegisterCreatedObjectUndo(instance, "Place Camera Rig");
        Selection.activeGameObject = instance;

        Debug.Log("[CameraRig] 已放入当前场景，Play 后会自动绑定玩家。");
    }

    private static GameObject BuildRigObject()
    {
        GameObject root = new GameObject("ThirdPersonCameraRig");
        root.AddComponent<ThirdPersonCameraBinder>();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root.transform, false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 45f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<CinemachineBrain>();

        GameObject vcamObject = new GameObject("Player Follow Camera");
        vcamObject.transform.SetParent(root.transform, false);

        CinemachineVirtualCamera vcam = vcamObject.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 10;
        vcam.m_Lens.FieldOfView = 45f;

        CinemachineTransposer transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        transposer.m_FollowOffset = new Vector3(0f, 1.1f, -3.4f);
        transposer.m_XDamping = 0.12f;
        transposer.m_YDamping = 0.12f;
        transposer.m_ZDamping = 0.12f;

        CinemachineComposer composer = vcam.AddCinemachineComponent<CinemachineComposer>();
        composer.m_TrackedObjectOffset = new Vector3(0f, 0.45f, 0f);

        ThirdPersonCameraOrbit orbit = vcamObject.AddComponent<ThirdPersonCameraOrbit>();
        SerializedObject orbitSo = new SerializedObject(orbit);
        orbitSo.FindProperty("distance").floatValue = 3.4f;
        orbitSo.FindProperty("startPitch").floatValue = 12f;
        orbitSo.ApplyModifiedPropertiesWithoutUndo();

        CinemachineCollider collider = vcamObject.AddComponent<CinemachineCollider>();
        collider.m_MinimumDistanceFromTarget = 0.4f;
        collider.m_CameraRadius = 0.15f;

        return root;
    }
}
