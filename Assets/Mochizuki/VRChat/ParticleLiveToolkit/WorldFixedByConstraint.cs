using System.Collections.Generic;
using System.Linq;

using Mochizuki.VRChat.ParticleLiveToolkit.Internal;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;
using UnityEngine.Animations;

using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Mochizuki.VRChat.ParticleLiveToolkit
{
    public class WorldFixedByConstraint : EditorWindow
    {
        private const string Version = "1.0.0";
        private const string Product = "World Fixed by Constraint";
        private const string InternalName = "MPT_Constraint";
        private const string PrefabGuid = "1bbb7ef4ea9c47746abc45bfd68fe7fe";

        private VRCAvatarDescriptor _avatar;
        private GameObject _gameObject;

        [MenuItem("Mochizuki/VRChat/Particle Live Toolkit/World Fixed by Constraint")]
        private static void ShowWindow()
        {
            var window = GetWindow<WorldFixedByConstraint>();
            window.titleContent = new GUIContent("World Fixed by Constraint");

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
                EditorGUILayout.LabelField("作成したパーティクルライブなどのオブジェクトをワールドに固定するための設定を生成するツールです。");
                EditorGUILayout.LabelField("※このツールは Avatar に設定してある Animator の Deep Copy を変更し、それをアバターに自動設定します。");
            }

            _avatar = ObjectPicker("Avatar", _avatar);
            _gameObject = ObjectPicker("固定したい Object", _gameObject);

            using (new EditorGUI.DisabledGroupScope(_avatar == null || _gameObject == null))
            {
                if (GUILayout.Button("生成する"))
                    OnSubmit(_avatar, _gameObject);
            }
        }

        private static void OnSubmit(VRCAvatarDescriptor avatar, GameObject go)
        {
            var parameters = CreateExpressionParameters(avatar);
            var expression = CreateExpressionMenus(avatar);
            var animations = CreateAnimations();
            var controller = CreateAnimatorController(avatar, animations);

            var prefab = LoadAssetFromGuid<GameObject>(PrefabGuid);
            var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab, avatar.transform);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            go.transform.parent = instance.GetComponentsInChildren<Transform>().First(w => w.name == "Object");

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

            expression.controls.Add(new VRCExpressionsMenu.Control { name = "Toggle World Fixed Joint", parameter = new VRCExpressionsMenu.Control.Parameter { name = InternalName }, type = VRCExpressionsMenu.Control.ControlType.Toggle });

            AssetDatabase.CreateAsset(expression, dest);

            return expression;
        }

        private static List<AnimationClip> CreateAnimations()
        {
            return new List<AnimationClip>
            {
                CreateActivationAnimation(),
                CreateDeactivationAnimation()
            };
        }

        private static AnimationClip CreateActivationAnimation()
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Constraint Activation Animation to...", "ActivateWorldFixed", "anim", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var animation = new AnimationClip();

            for (var i = 0; i < 2; i++)
            {
                var curve = AnimationCurve.Constant(0, i / 60f, 0);
                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.DiscreteCurve($"{InternalName}/ToWorld/Object", typeof(ParentConstraint), "m_Active"), curve);
            }

            AssetDatabase.CreateAsset(animation, dest);

            return animation;
        }

        private static AnimationClip CreateDeactivationAnimation()
        {
            var dest = EditorUtility.SaveFilePanelInProject("Save Constraint Deactivation Animation to...", "DeactivateWorldFixed", "anim", "");
            if (string.IsNullOrWhiteSpace(dest))
                return null;

            var animation = new AnimationClip();

            for (var i = 0; i < 2; i++)
            {
                var curve = AnimationCurve.Constant(0, i / 60f, 1);
                AnimationUtility.SetEditorCurve(animation, EditorCurveBinding.DiscreteCurve($"{InternalName}/ToWorld/Object", typeof(ParentConstraint), "m_Active"), curve);
            }

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

            if (controller.HasLayer(InternalName) && controller.HasParameter(InternalName))
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

            AssetDatabase.SaveAssets();

            return controller;
        }

        private static T ObjectPicker<T>(string label, T obj) where T : Object
        {
            return EditorGUILayout.ObjectField(new GUIContent(label), obj, typeof(T), true) as T;
        }

        private static T LoadAssetFromGuid<T>(string guid) where T : Object
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}