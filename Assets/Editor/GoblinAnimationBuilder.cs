using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

public static class GoblinAnimationBuilder
{
    private const string LibraryPath = "Assets/Sprites/SpriteLib/EnemiesSpriteLib/Goblin1.spriteLib";
    private const string OutputFolder = "Assets/Animations/GoblinAnimations";
    private const string ControllerPath = OutputFolder + "/Goblin.controller";
    private const float FrameDuration = 0.1f;

    private sealed class StateDefinition
    {
        public string Category;
        public string ClipState;
        public string LabelPrefix;
        public int FramesPerDirection;
        public bool Loop;
        public bool Attack;
    }

    private static readonly StateDefinition[] States =
    {
        new() { Category = "Goblin1_Idle", ClipState = "Idle", LabelPrefix = "Idle0_without_shadow_", FramesPerDirection = 4, Loop = true },
        new() { Category = "Goblin1_Walk", ClipState = "Walk", LabelPrefix = "Walk0_without_shadow_", FramesPerDirection = 6, Loop = true },
        new() { Category = "Goblin1_Run", ClipState = "Run", LabelPrefix = "Run0_without_shadow_", FramesPerDirection = 8, Loop = true },
        new() { Category = "Goblin1_Attack", ClipState = "Attack", LabelPrefix = "Attack0_without_shadow_", FramesPerDirection = 5, Attack = true },
        new() { Category = "Goblin1_WalkAttack", ClipState = "WalkAttack", LabelPrefix = "Walk_Attack0_without_shadow_", FramesPerDirection = 6, Attack = true },
        new() { Category = "Goblin1_RunAttack", ClipState = "RunAttack", LabelPrefix = "Run_Attack0_without_shadow_", FramesPerDirection = 8, Attack = true },
        new() { Category = "Goblin1_Hurt", ClipState = "Hurt", LabelPrefix = "Hurt0_without_shadow_", FramesPerDirection = 4 },
        new() { Category = "Goblin1_Death", ClipState = "Death", LabelPrefix = "Death0_without_shadow_", FramesPerDirection = 6 }
    };

    private static readonly (string Name, int Block)[] Directions =
    {
        ("Down", 0),
        ("Up", 1),
        ("Left", 2),
        ("Right", 3)
    };

    [MenuItem("Tools/Goblin/Rebuild Goblin1 Animations")]
    public static void Build()
    {
        SpriteLibraryAsset library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(LibraryPath);
        if (library == null)
            throw new InvalidOperationException($"Sprite Library not found: {LibraryPath}");

        EnsureFolder(OutputFolder);

        GameObject resolverObject = new("GoblinAnimationBuilder_TemporaryResolver");
        try
        {
            resolverObject.AddComponent<SpriteRenderer>();
            SpriteLibrary spriteLibrary = resolverObject.AddComponent<SpriteLibrary>();
            spriteLibrary.spriteLibraryAsset = library;
            SpriteResolver resolver = resolverObject.AddComponent<SpriteResolver>();

            foreach (StateDefinition state in States)
            {
                foreach ((string directionName, int block) in Directions)
                    BuildClip(library, resolver, state, directionName, block);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(resolverObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Goblin1 animation build complete: 32 directional clips with one virtual end frame each.");
    }

    [MenuItem("Tools/Goblin/Rebuild Goblin Controller")]
    public static void BuildController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.parameters = new[]
        {
            FloatParameter("InputX"),
            FloatParameter("InputY"),
            FloatParameter("LastInputX"),
            FloatParameter("LastInputY"),
            BoolParameter("isWalking"),
            BoolParameter("isRunning"),
            TriggerParameter("Attack"),
            TriggerParameter("isHit"),
            BoolParameter("isDead")
        };

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        ClearStateMachine(controller, machine);

        AnimatorState idle = CreateDirectionalState(controller, machine, "Idle", "Idle", "LastInputX", "LastInputY", new Vector3(300, 40));
        AnimatorState walk = CreateDirectionalState(controller, machine, "Walk", "Walk", "InputX", "InputY", new Vector3(300, 130));
        AnimatorState run = CreateDirectionalState(controller, machine, "Run", "Run", "InputX", "InputY", new Vector3(300, 220));
        AnimatorState runAttack = CreateDirectionalState(controller, machine, "RunAttack", "RunAttack", "LastInputX", "LastInputY", new Vector3(560, 120));
        AnimatorState hurt = CreateDirectionalState(controller, machine, "Hurt", "Hurt", "LastInputX", "LastInputY", new Vector3(800, 80));
        AnimatorState death = CreateDirectionalState(controller, machine, "Death", "Death", "LastInputX", "LastInputY", new Vector3(800, 200));
        machine.defaultState = idle;

        // Goblin attacks only while chasing/running. Idle and Walk intentionally
        // have no Attack transition; the unused clips remain as standalone assets.
        AddTriggerTransition(run, runAttack, "Attack");

        AddImmediateTransition(idle, walk, ("isWalking", true), ("isRunning", false));
        AddImmediateTransition(idle, run, ("isWalking", true), ("isRunning", true));
        AddImmediateTransition(walk, idle, ("isWalking", false));
        AddImmediateTransition(walk, run, ("isRunning", true));
        AddImmediateTransition(run, idle, ("isWalking", false));
        AddImmediateTransition(run, walk, ("isWalking", true), ("isRunning", false));

        AddAttackCompletionTransitions(runAttack, walk, run);
        AddFinishedStateTransitions(hurt, idle, walk, run);

        AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(death);
        ConfigureImmediate(deathTransition);
        deathTransition.canTransitionToSelf = false;
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "isDead");

        AnimatorStateTransition hurtTransition = machine.AddAnyStateTransition(hurt);
        ConfigureImmediate(hurtTransition);
        hurtTransition.canTransitionToSelf = false;
        hurtTransition.AddCondition(AnimatorConditionMode.If, 0f, "isHit");
        hurtTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "isDead");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Goblin controller build complete: 6 directional Blend Tree states; only Run transitions to RunAttack.");
    }

    private static AnimatorControllerParameter FloatParameter(string name) =>
        new() { name = name, type = AnimatorControllerParameterType.Float };

    private static AnimatorControllerParameter BoolParameter(string name) =>
        new() { name = name, type = AnimatorControllerParameterType.Bool };

    private static AnimatorControllerParameter TriggerParameter(string name) =>
        new() { name = name, type = AnimatorControllerParameterType.Trigger };

    private static void ClearStateMachine(AnimatorController controller, AnimatorStateMachine machine)
    {
        foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            machine.RemoveAnyStateTransition(transition);
        foreach (ChildAnimatorState child in machine.states)
            machine.RemoveState(child.state);
        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
            machine.RemoveStateMachine(child.stateMachine);

        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
        {
            if (asset is BlendTree)
                UnityEngine.Object.DestroyImmediate(asset, true);
        }
    }

    private static AnimatorState CreateDirectionalState(
        AnimatorController controller,
        AnimatorStateMachine machine,
        string stateName,
        string clipState,
        string parameterX,
        string parameterY,
        Vector3 position)
    {
        BlendTree tree = new()
        {
            name = stateName + " Blend Tree",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = parameterX,
            blendParameterY = parameterY,
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(LoadClip(clipState, "Down"), Vector2.down);
        tree.AddChild(LoadClip(clipState, "Left"), Vector2.left);
        tree.AddChild(LoadClip(clipState, "Right"), Vector2.right);
        tree.AddChild(LoadClip(clipState, "Up"), Vector2.up);

        AnimatorState state = machine.AddState(stateName, position);
        state.motion = tree;
        state.writeDefaultValues = true;
        return state;
    }

    private static AnimationClip LoadClip(string state, string direction)
    {
        string path = $"{OutputFolder}/Goblin_{state}_{direction}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"Animation clip not found: {path}");
        return clip;
    }

    private static void AddTriggerTransition(AnimatorState source, AnimatorState destination, string trigger)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediate(transition);
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddImmediateTransition(
        AnimatorState source,
        AnimatorState destination,
        params (string parameter, bool expected)[] conditions)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediate(transition);
        foreach ((string parameter, bool expected) in conditions)
            transition.AddCondition(expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddFinishedStateTransitions(
        AnimatorState source,
        AnimatorState idle,
        AnimatorState walk,
        AnimatorState run)
    {
        AddExitTransition(source, run, ("isWalking", true), ("isRunning", true));
        AddExitTransition(source, walk, ("isWalking", true), ("isRunning", false));
        AddExitTransition(source, idle, ("isWalking", false));
    }

    private static void AddAttackCompletionTransitions(
        AnimatorState source,
        AnimatorState walk,
        AnimatorState run)
    {
        // On attack entry EnemyUniversal immediately sets both locomotion flags
        // false. Waiting for them to become true ensures the final animation event
        // has called FinishAttackAnimation before Animator leaves RunAttack.
        AddImmediateTransition(source, run, ("isWalking", true), ("isRunning", true));
        AddImmediateTransition(source, walk, ("isWalking", true), ("isRunning", false));
    }

    private static void AddExitTransition(
        AnimatorState source,
        AnimatorState destination,
        params (string parameter, bool expected)[] conditions)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = 0.95f;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        foreach ((string parameter, bool expected) in conditions)
            transition.AddCondition(expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void ConfigureImmediate(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static void BuildClip(
        SpriteLibraryAsset library,
        SpriteResolver resolver,
        StateDefinition state,
        string direction,
        int directionBlock)
    {
        string path = $"{OutputFolder}/Goblin_{state.ClipState}_{direction}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = $"Goblin_{state.ClipState}_{direction}";
        clip.frameRate = 10f;

        List<Keyframe> keys = new();
        int startIndex = directionBlock * state.FramesPerDirection;
        for (int frame = 0; frame < state.FramesPerDirection; frame++)
        {
            string label = state.LabelPrefix + (startIndex + frame);
            ValidateLabel(library, state.Category, label);
            keys.Add(new Keyframe(frame * FrameDuration, GetSpriteKey(resolver, state.Category, label)));
        }

        // The extra virtual frame gives events/transitions a stable final sample.
        int virtualSourceFrame = state.Loop ? 0 : state.FramesPerDirection - 1;
        string virtualLabel = state.LabelPrefix + (startIndex + virtualSourceFrame);
        keys.Add(new Keyframe(state.FramesPerDirection * FrameDuration,
            GetSpriteKey(resolver, state.Category, virtualLabel)));

        AnimationCurve curve = new(keys.ToArray());
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }

        EditorCurveBinding binding = new()
        {
            path = string.Empty,
            type = typeof(SpriteResolver),
            propertyName = "m_SpriteHash"
        };
        AnimationUtility.SetEditorCurve(clip, binding, curve);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = state.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AnimationUtility.SetAnimationEvents(clip, BuildEvents(state));
        EditorUtility.SetDirty(clip);
    }

    private static AnimationEvent[] BuildEvents(StateDefinition state)
    {
        if (!state.Attack)
            return Array.Empty<AnimationEvent>();

        float openTime = Mathf.Max(FrameDuration, (state.FramesPerDirection / 2) * FrameDuration);
        float finishTime = state.FramesPerDirection * FrameDuration;
        return new[]
        {
            new AnimationEvent { time = openTime, functionName = "OpenAttackWindow" },
            new AnimationEvent { time = finishTime, functionName = "FinishAttackAnimation" }
        };
    }

    private static float GetSpriteKey(SpriteResolver resolver, string category, string label)
    {
        resolver.SetCategoryAndLabel(category, label);
        resolver.ResolveSpriteToSpriteRenderer();
        SerializedObject serializedResolver = new(resolver);
        serializedResolver.Update();
        int spriteHash = serializedResolver.FindProperty("m_SpriteHash").intValue;
        return BitConverter.ToSingle(BitConverter.GetBytes(spriteHash), 0);
    }

    private static void ValidateLabel(SpriteLibraryAsset library, string category, string label)
    {
        if (library.GetSprite(category, label) == null)
            throw new InvalidOperationException($"Missing SpriteLib label: {category}/{label}");
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
