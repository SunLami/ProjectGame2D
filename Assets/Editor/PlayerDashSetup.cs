using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the dash animation set from the existing 4-direction Run clips (D-085): each DashX clip is
/// the FIRST and LAST keyframe of RunX held for half of the dash each. The clips only store
/// SpriteResolver label hashes, so they work for every player body/head SpriteLibrary (level 1-9).
/// Also adds the 'isDashing' parameter and a 'Dash' blend-tree state (LastInputX/Y) to Player.controller.
/// Idempotent: re-running rebuilds the clips and leaves an existing Dash state alone.
/// </summary>
public static class PlayerDashSetup
{
    private const string AnimationFolder = "Assets/Animations/PlayerAnimations";
    private const string ControllerPath = AnimationFolder + "/Player.controller";
    private const float DashClipLength = 0.2f;

    private static readonly (string direction, Vector2 position)[] Directions =
    {
        ("Down", new Vector2(0f, -1f)),
        ("Left", new Vector2(-1f, 0f)),
        ("Right", new Vector2(1f, 0f)),
        ("Up", new Vector2(0f, 1f)),
    };

    [MenuItem("Tools/Project Game/Player/Build Dash Animations")]
    public static void Build()
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach ((string direction, _) in Directions)
            clips[direction] = BuildClip(direction);

        AddToController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PlayerDashSetup: Dash clips built from Run first/last frames and Player.controller updated.");
    }

    private static AnimationClip BuildClip(string direction)
    {
        string runPath = $"{AnimationFolder}/Run{direction}.anim";
        string dashPath = $"{AnimationFolder}/Dash{direction}.anim";
        var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(runPath);
        if (run == null)
            throw new System.InvalidOperationException($"Missing {runPath}");

        var dash = AssetDatabase.LoadAssetAtPath<AnimationClip>(dashPath);
        bool isNew = dash == null;
        if (isNew)
            dash = new AnimationClip { name = $"Dash{direction}" };
        else
            dash.ClearCurves();

        dash.frameRate = run.frameRate;
        float half = DashClipLength * 0.5f;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(run))
        {
            AnimationCurve source = AnimationUtility.GetEditorCurve(run, binding);
            if (source == null || source.length == 0)
                continue;

            float first = source.keys[0].value;
            float last = source.keys[source.length - 1].value;
            var curve = new AnimationCurve(
                ConstantKey(0f, first),
                ConstantKey(half, last),
                ConstantKey(DashClipLength, last));
            AnimationUtility.SetEditorCurve(dash, binding, curve);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(run))
        {
            ObjectReferenceKeyframe[] source = AnimationUtility.GetObjectReferenceCurve(run, binding);
            if (source == null || source.Length == 0)
                continue;

            var keys = new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = source[0].value },
                new ObjectReferenceKeyframe { time = half, value = source[source.Length - 1].value },
            };
            AnimationUtility.SetObjectReferenceCurve(dash, binding, keys);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(dash);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(dash, settings);

        if (isNew)
            AssetDatabase.CreateAsset(dash, dashPath);
        else
            EditorUtility.SetDirty(dash);

        return dash;
    }

    // Step curve: infinite tangents hold the value (same as the original Run curves).
    private static Keyframe ConstantKey(float time, float value) =>
        new Keyframe(time, value, float.PositiveInfinity, float.PositiveInfinity);

    private static void AddToController(Dictionary<string, AnimationClip> clips)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new System.InvalidOperationException($"Missing {ControllerPath}");

        bool hasParameter = false;
        foreach (AnimatorControllerParameter parameter in controller.parameters)
            hasParameter |= parameter.name == "isDashing";
        if (!hasParameter)
            controller.AddParameter("isDashing", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState dashState = null;
        AnimatorState idleState = null;
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Dash")
                dashState = child.state;
            else if (child.state.name == "Idle")
                idleState = child.state;
        }

        if (dashState != null)
            return;

        if (idleState == null)
            throw new System.InvalidOperationException("Player.controller has no 'Idle' state.");

        var tree = new BlendTree
        {
            name = "Dash",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "LastInputX",
            blendParameterY = "LastInputY",
            useAutomaticThresholds = false,
        };
        foreach ((string direction, Vector2 position) in Directions)
            tree.AddChild(clips[direction], position);
        AssetDatabase.AddObjectToAsset(tree, controller);

        dashState = machine.AddState("Dash");
        dashState.motion = tree;
        dashState.writeDefaultValues = true;

        AnimatorStateTransition enter = machine.AddAnyStateTransition(dashState);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, "isDashing");
        enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "isDead");

        AnimatorStateTransition exit = dashState.AddTransition(idleState);
        exit.hasExitTime = false;
        exit.duration = 0f;
        exit.AddCondition(AnimatorConditionMode.IfNot, 0f, "isDashing");

        EditorUtility.SetDirty(controller);
    }
}
