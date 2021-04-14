using System.Collections.Generic;

using Mochizuki.VRChat.ParticleLiveToolkit.Internal;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;

using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Mochizuki.VRChat.ParticleLiveToolkit
{
    public class ToggleAnimation : EditorWindow
    {
        private const string Version = "1.0.0";
        private const string Product = "Toggle Animation";
        private const string InternalName = "MPT_ToggleAnim";
        private AnimationClip _animation;

        private Animator _animator;
        private VRCAvatarDescriptor _avatar;

        [MenuItem("Mochizuki/VRChat/Particle Live Toolkit/Toggle Animation")]
        private static void ShowWindow()
        {
            var window = GetWindow<ToggleAnimation>();
            window.titleContent = new GUIContent("Toggle Animation");

            window.Show();
        }

        private void OnGUI()
        {
            EditorStyles.label.wordWrap = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{Product} - {Version}");
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("作成したパーティクルライブなどのオブジェクトを出したり閉まったりするための設定を生成するツールです。");
                EditorGUILayout.LabelField("※このツールは Avatar に設定してある Animator の Deep Copy を変更し、それをアバターに自動設定します。");
            }

            _avatar = ObjectPicker("Avatar", _avatar);
            _animator = ObjectPicker("切り替えたい Object", _animator);
            _animation = ObjectPicker("基準となる Animation", _animation);

            using (new EditorGUI.DisabledGroupScope(_avatar == null || _animator == null || _animation == null))
            {
                if (GUILayout.Button("生成する"))
                    OnSubmit(_avatar, _animator, _animation);
            }
        }

        private static void OnSubmit(VRCAvatarDescriptor avatar, Animator animator, AnimationClip animation)
        {
            var parameters = CreateExpressionParameters(avatar);
            var expression = CreateExpressionMenus(avatar);
            var animations = CreateAnimations(avatar, animator, animation);
            var controller = CreateAnimatorController(avatar, animations);

            avatar.expressionParameters = parameters;
            avatar.expressionsMenu = expression;
            avatar.SetAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX, controller);
        }

        private static VRCExpressionParameters CreateExpressionParameters(VRCAvatarDescriptor avatar)
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Copied Expression Parameters to...", "NewExpressionParameter", "asset", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var parameters = CreateInstance<VRCExpressionParameters>();
            parameters.InitExpressionParameters(true);

            if (avatar.customExpressions && avatar.expressionParameters != null)
                parameters.MergeParameters(avatar.expressionParameters);

            if (!parameters.HasParameter(InternalName))
                parameters.AddParameter(new VRCExpressionParameters.Parameter { defaultValue = 0, name = InternalName, saved = false, valueType = VRCExpressionParameters.ValueType.Bool });

            AssetDatabase.CreateAsset(parameters, dest);

            return parameters;
        }

        private static VRCExpressionsMenu CreateExpressionMenus(VRCAvatarDescriptor avatar)
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Copied Expressions Menu to...", "NewExpressionsMenu", "asset", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var expression = CreateInstance<VRCExpressionsMenu>();

            if (avatar.customExpressions && avatar.expressionsMenu != null)
                expression.MergeExpressions(avatar.expressionsMenu);

            expression.controls.Add(new VRCExpressionsMenu.Control { name = "Toggle Animation", parameter = new VRCExpressionsMenu.Control.Parameter { name = InternalName }, type = VRCExpressionsMenu.Control.ControlType.Toggle });

            AssetDatabase.CreateAsset(expression, dest);

            return expression;
        }

        private static List<AnimationClip> CreateAnimations(VRCAvatarDescriptor avatar, Animator animator, AnimationClip animation)
        {
            return new List<AnimationClip>
            {
                CreateActivationAnimation(avatar, animator, animation),
                CreateDeactivationAnimation(avatar, animator)
            };
        }

        private static AnimationClip CreateActivationAnimation(VRCAvatarDescriptor avatar, Animator animator, AnimationClip sourceAnimation)
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Constraint Activation Animation to...", "Activation", "anim", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var animation = new AnimationClip();

            var curve = AnimationCurve.Constant(0, sourceAnimation.length, 1);
            AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.DiscreteCurve(avatar.gameObject.GetRelativePathFor(animator.gameObject), typeof(GameObject), "m_IsActive"), curve);

            AssetDatabase.CreateAsset(animation, dest);

            return animation;
        }

        private static AnimationClip CreateDeactivationAnimation(VRCAvatarDescriptor avatar, Animator animator)
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Constraint Deactivation Animation to...", "Deactivation", "anim", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var animation = new AnimationClip();

            var curve = AnimationCurve.Constant(0, 1 / 60f, 0);
            AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.DiscreteCurve(avatar.gameObject.GetRelativePathFor(animator.gameObject), typeof(GameObject), "m_IsActive"), curve);

            AssetDatabase.CreateAsset(animation, dest);

            return animation;
        }

        private static AnimatorController CreateAnimatorController(VRCAvatarDescriptor avatar, List<AnimationClip> animations)
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Copied Animator Controller to...", "NewAnimatorController", "controller", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var controller = new AnimatorController();
            AssetDatabase.CreateAsset(controller, dest);

            if (avatar.customizeAnimationLayers && avatar.HasAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX, false))
                controller.MergeControllers((AnimatorController) avatar.GetAnimationLayer(VRCAvatarDescriptor.AnimLayerType.FX).animatorController);

            if (controller.HasLayer(InternalName))
            {
                AssetDatabase.SaveAssets();
                return controller;
            }

            controller.AddParameter(InternalName, AnimatorControllerParameterType.Bool);
            controller.AddLayer(InternalName);

            var layer = controller.GetLayer(InternalName);
            layer.defaultWeight = 1.0f;
            controller.SetLayer(InternalName, layer);

            var stateMachine = layer.stateMachine;

            var deactivationState = stateMachine.AddState("Deactivation");
            deactivationState.motion = animations[1];
            deactivationState.writeDefaultValues = false;
            deactivationState.AddExitTransition(true);

            var activationState = stateMachine.AddState("Activation");
            activationState.motion = animations[0];
            activationState.writeDefaultValues = false;

            var transitionFromAny = stateMachine.AddAnyStateTransition(activationState);
            transitionFromAny.AddCondition(AnimatorConditionMode.If, 1.0f, InternalName); // true

            var transitionToDeactivation = activationState.AddTransition(deactivationState);
            transitionToDeactivation.AddCondition(AnimatorConditionMode.IfNot, 1.0f, InternalName); // false
            transitionToDeactivation.hasExitTime = false;

            AssetDatabase.SaveAssets();

            return controller;
        }

        private static T ObjectPicker<T>(string label, T obj) where T : Object
        {
            return EditorGUILayout.ObjectField(new GUIContent(label), obj, typeof(T), true) as T;
        }
    }
}