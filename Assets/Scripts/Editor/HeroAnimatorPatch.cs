using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HeroAnimatorPatch
{
    private const string ControllerPath = "Assets/Animations/Player_Leading.controller";
    private const string ModelPath = "Assets/Art/Hero/Leading/SM_leading_Rigged.fbx";
    private const string FallStateName = "Fall";

    [MenuItem("Tools/Hero/Add Fall State")]
    public static void AddFall()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            Debug.LogError("[HeroAnim] 找不到 " + ControllerPath);
            return;
        }

        AnimationClip jump = FindClip("Jump");

        if (jump == null)
        {
            Debug.LogError("[HeroAnim] 找不到 Jump clip。");
            return;
        }

        if (!controller.parameters.Any(p => p.name == "Falling"))
        {
            controller.AddParameter("Falling", AnimatorControllerParameterType.Bool);
        }

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = FindState(machine, "Idle");
        AnimatorState run = FindState(machine, "Run");
        AnimatorState jumpState = FindState(machine, "Jump");

        AnimatorState fall = FindState(machine, FallStateName);

        if (fall != null)
        {
            machine.RemoveState(fall);
        }

        fall = machine.AddState(FallStateName);
        fall.motion = jump;
        fall.speed = 0.35f;

        AddFallTransition(idle, fall);
        AddFallTransition(run, fall);
        AddFallTransition(jumpState, fall);

        AddLandTransition(fall, idle, AnimatorConditionMode.Less, 0.1f);
        AddLandTransition(fall, run, AnimatorConditionMode.Greater, 0.1f);

        if (jumpState != null)
        {
            RequireNotFalling(jumpState);
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("[HeroAnim] 已添加 Fall 状态（用 Jump clip 的 0.35 倍速做下坠）。"
                  + "\n条件：Falling=true 进入，Falling=false 且 Speed 决定回到 Idle 还是 Run。");
    }

    private static void AddFallTransition(AnimatorState from, AnimatorState fall)
    {
        if (from == null)
        {
            return;
        }

        AnimatorStateTransition transition = from.AddTransition(fall);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Falling");
    }

    private static void AddLandTransition(AnimatorState fall, AnimatorState to, AnimatorConditionMode mode, float threshold)
    {
        if (to == null)
        {
            return;
        }

        AnimatorStateTransition transition = fall.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.12f;
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Falling");
        transition.AddCondition(mode, threshold, "Speed");
    }

    private static void RequireNotFalling(AnimatorState jumpState)
    {
        AnimatorStateTransition[] transitions = jumpState.transitions;

        for (int i = 0; i < transitions.Length; i++)
        {
            AnimatorStateTransition transition = transitions[i];

            if (transition.destinationState != null && transition.destinationState.name == FallStateName)
            {
                continue;
            }

            bool hasFalling = transition.conditions.Any(c => c.parameter == "Falling");

            if (!hasFalling)
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Falling");
            }
        }
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        return machine.states
            .Select(s => s.state)
            .FirstOrDefault(s => s != null && s.name == name);
    }

    private static AnimationClip FindClip(string name)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);

        return assets
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == name && !c.name.StartsWith("__preview__"));
    }
}
