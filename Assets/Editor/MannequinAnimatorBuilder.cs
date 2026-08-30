using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MannequinAnimatorBuilder
{
    private const string ControllerPath = "Assets/Animations/ObjectAnimations/Attacked_Manequin1.controller";
    private const string IdleClipPath = "Assets/Animations/ObjectAnimations/Idle.anim";
    private const string HitClipPath = "Assets/Animations/ObjectAnimations/Manequin1_Hit.anim";

    [MenuItem("Tools/Project Game/World/Rebuild Mannequin1 Animator")]
    public static void Rebuild()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipPath);
        if (controller == null || idleClip == null || hitClip == null)
        {
            Debug.LogError("Mannequin1 animator assets are missing.");
            return;
        }

        controller.parameters = controller.parameters
            .Where(parameter => parameter.name != "Hit").ToArray();
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.layers = System.Array.Empty<AnimatorControllerLayer>();
        controller.AddLayer("Base Layer");

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(300f, 100f));
        AnimatorState hit = stateMachine.AddState("Manequin1_Hit", new Vector3(300f, 220f));
        idle.motion = idleClip;
        hit.motion = hitClip;
        stateMachine.defaultState = idle;

        AnimatorStateTransition enterHit = stateMachine.AddAnyStateTransition(hit);
        enterHit.hasExitTime = false;
        enterHit.duration = 0f;
        enterHit.canTransitionToSelf = false;
        enterHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

        AnimatorStateTransition returnIdle = hit.AddTransition(idle);
        returnIdle.hasExitTime = true;
        returnIdle.exitTime = 1f;
        returnIdle.duration = 0f;
        returnIdle.hasFixedDuration = true;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Mannequin1 Animator rebuilt: Idle -> Hit trigger -> Manequin1_Hit -> Idle.");
    }
}
