using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class ThirdPersonCameraBinder : MonoBehaviour
{
    [SerializeField] private string cameraRootName = "PlayerCameraRoot";
    [Tooltip("留空则自动查找场景里的玩家")]
    [SerializeField] private Transform targetOverride;
    [SerializeField] private float cameraRootHeight = 0.72f;

    private CinemachineVirtualCamera _virtualCamera;

    public Transform Target { get; private set; }

    private void Awake()
    {
        Bind();
    }

    public void Bind()
    {
        if (_virtualCamera == null)
        {
            _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
        }

        if (_virtualCamera == null)
        {
            Debug.LogWarning("[CameraRig] 没有 CinemachineVirtualCamera。", this);
            return;
        }

        Target = targetOverride != null ? targetOverride : ResolveTarget();

        if (Target == null)
        {
            Debug.LogWarning("[CameraRig] 没找到玩家 / PlayerCameraRoot，相机保持静止。", this);
            return;
        }

        if (_virtualCamera.Follow == null) _virtualCamera.Follow = Target;
        if (_virtualCamera.LookAt == null) _virtualCamera.LookAt = Target;
    }

    private Transform ResolveTarget()
    {
        MonoBehaviour motor = FindObjectOfType<ThirdPersonMotor>();

        if (motor == null)
        {
            motor = FindObjectOfType<FirstPersonMotor>();
        }

        if (motor == null)
        {
            return null;
        }

        Transform found = FindDeepChild(motor.transform, cameraRootName);

        if (found != null)
        {
            return found;
        }

        GameObject created = new GameObject(cameraRootName);
        created.transform.SetParent(motor.transform, false);
        created.transform.localPosition = new Vector3(0f, cameraRootHeight, 0f);
        return created.transform;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
