#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-click setup for Hammer tool visuals and weapon animation controller.
/// </summary>
public static class SetupHammerTool
{
    private const string HammerDataPath = "Assets/_Game/Data/ToolData/Hammer.asset";
    private const string HammerSpriteSheetPath = "Assets/_Game/Art/Sprites/hammer.png";
    private const string HammerControllerPath = "Assets/_Game/Art/Animations/hammer/hammerAnimator.controller";
    private const string HammerAttackClipPath = "Assets/_Game/Art/Animations/hammer/attack.anim";
    private const string HammerIdleClipPath = "Assets/_Game/Art/Animations/hammer/idle.anim";

    [MenuItem("Tools/Setup/Configure Hammer Tool")]
    public static void Configure()
    {
        ToolData hammerData = AssetDatabase.LoadAssetAtPath<ToolData>(HammerDataPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HammerControllerPath);
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HammerAttackClipPath);

        if (hammerData == null || controller == null || attackClip == null)
        {
            Debug.LogError("[SetupHammerTool] Missing required assets.");
            return;
        }

        Sprite[] sprites = AssetDatabase
            .LoadAllAssetsAtPath(HammerSpriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("[SetupHammerTool] No sprites found in hammer.png.");
            return;
        }

        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HammerIdleClipPath);
        if (idleClip == null)
        {
            idleClip = new AnimationClip();
            AssetDatabase.CreateAsset(idleClip, HammerIdleClipPath);
        }

        ConfigureIdleClip(idleClip, sprites[0]);
        ConfigureAttackClip(attackClip, sprites);
        ConfigureAnimatorController(controller, idleClip, attackClip);
        ConfigureHammerData(hammerData, controller, sprites[0]);

        EditorUtility.SetDirty(idleClip);
        EditorUtility.SetDirty(attackClip);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(hammerData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SetupHammerTool] Done. Sprites found: {sprites.Length}");
    }

    private static void ConfigureIdleClip(AnimationClip idleClip, Sprite sprite)
    {
        var binding = SpriteBinding();
        var keys = new[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprite }
        };

        AnimationUtility.SetObjectReferenceCurve(idleClip, binding, keys);
        idleClip.frameRate = 12f;

        var settings = AnimationUtility.GetAnimationClipSettings(idleClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idleClip, settings);
    }

    private static void ConfigureAttackClip(AnimationClip attackClip, Sprite[] sprites)
    {
        var binding = SpriteBinding();
        int frameCount = Mathf.Min(4, sprites.Length);
        var keys = new ObjectReferenceKeyframe[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / 12f,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(attackClip, binding, keys);
        attackClip.frameRate = 12f;

        var settings = AnimationUtility.GetAnimationClipSettings(attackClip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(attackClip, settings);
    }

    private static void ConfigureAnimatorController(
        AnimatorController controller,
        AnimationClip idleClip,
        AnimationClip attackClip)
    {
        if (!controller.parameters.Any(p => p.name == "Attack"))
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        if (controller.layers == null || controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        foreach (ChildAnimatorState state in stateMachine.states.ToArray())
            stateMachine.RemoveState(state.state);

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState attackState = stateMachine.AddState("Attack");
        attackState.motion = attackClip;

        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.hasExitTime = false;
        idleToAttack.hasFixedDuration = true;
        idleToAttack.duration = 0f;
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.95f;
        attackToIdle.hasFixedDuration = true;
        attackToIdle.duration = 0f;
    }

    private static void ConfigureHammerData(ToolData hammerData, AnimatorController controller, Sprite defaultSprite)
    {
        hammerData.toolSprite = defaultSprite;
        hammerData.toolIcon = defaultSprite;
        hammerData.attackAnimator = controller;
        // Melee weapon does not need attackPrefab by default.
        hammerData.attackPrefab = null;
    }

    private static EditorCurveBinding SpriteBinding()
    {
        return new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
    }
}
#endif
