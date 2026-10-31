using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainToThirdPerson
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_Leading.prefab";
    private const string CameraRigPath = "Assets/Prefabs/Camera/ThirdPersonCameraRig.prefab";

    [MenuItem("Tools/Hero/Convert Main To Third Person")]
    public static void Convert()
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

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

        if (playerPrefab == null)
        {
            Debug.LogError("[Convert] 找不到 " + PlayerPrefabPath + "，先跑 Tools/Hero/Build Player Character。");
            return;
        }

        GameObject oldPlayer = GameObject.Find("Player");

        if (oldPlayer == null)
        {
            Debug.LogError("[Convert] 场景里没有名为 Player 的物体。");
            return;
        }

        if (oldPlayer.GetComponent<ThirdPersonMotor>() != null)
        {
            Debug.Log("[Convert] 已经是第三人称玩家了，跳过。");
            return;
        }

        Vector3 position = oldPlayer.transform.position;
        Quaternion rotation = oldPlayer.transform.rotation;

        PlayerRespawnManager oldRespawn = oldPlayer.GetComponent<PlayerRespawnManager>();
        PlayerLandingParticles oldLanding = oldPlayer.GetComponent<PlayerLandingParticles>();

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Player";
        player.transform.position = position;
        player.transform.rotation = rotation;

        if (oldRespawn != null)
        {
            CopyComponent(oldRespawn, player);
        }

        if (oldLanding != null)
        {
            CopyComponent(oldLanding, player);
        }

        ThirdPersonMotor motor = player.GetComponent<ThirdPersonMotor>();
        PlayerBlockAgent agent = player.GetComponent<PlayerBlockAgent>();
        PlayerInputReader reader = player.GetComponent<PlayerInputReader>();

        PlayerRespawnManager respawn = player.GetComponent<PlayerRespawnManager>();

        if (respawn != null)
        {
            SerializedObject so = new SerializedObject(respawn);
            SetRef(so, "motorSource", motor);
            SetRef(so, "look", null);
            SetRef(so, "agent", agent);
            SetRef(so, "hud", null);
            SetRef(so, "input", reader);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.objects = new Object[0];
        Object.DestroyImmediate(oldPlayer);

        CreateCameraRig();

        BindEndingDirector(motor);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[Convert] Main 已切换为第三人称：\n"
                  + "· 新玩家 Player（CharacterController 1m + ThirdPersonMotor + 技能 + 死亡管理）\n"
                  + "· 相机改为 ThirdPersonCameraRig（鼠标环绕 / 自动锁鼠标）\n"
                  + "· 出生点沿用旧位置 " + position.ToString("0.00")
                  + "\n\nPlay 验证：WASD 移动、鼠标转视角、Space 跳、1-4 技能、Esc 暂停、掉落重生。");
    }

    private static void CopyComponent(Component source, GameObject target)
    {
        UnityEditorInternal.ComponentUtility.CopyComponent(source);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
    }

    private static void CreateCameraRig()
    {
        Camera[] leftovers = Object.FindObjectsOfType<Camera>(true);

        for (int i = 0; i < leftovers.Length; i++)
        {
            if (leftovers[i] != null)
            {
                Object.DestroyImmediate(leftovers[i].gameObject);
            }
        }

        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPath);

        if (rigPrefab == null)
        {
            CameraRigBuilder.BuildPrefab();
            rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPath);
        }

        if (rigPrefab == null)
        {
            Debug.LogWarning("[Convert] 相机预制体创建失败，请手动放置。");
            return;
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.name = "ThirdPersonCameraRig";
    }

    private static void BindEndingDirector(ThirdPersonMotor motor)
    {
        EndingDirector ending = Object.FindObjectOfType<EndingDirector>(true);

        if (ending == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(ending);
        SetRef(so, "motorSource", motor);
        SetRef(so, "look", null);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRef(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
