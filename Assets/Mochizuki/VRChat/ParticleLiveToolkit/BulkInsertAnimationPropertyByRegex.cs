using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;

namespace Mochizuki.VRChat.ParticleLiveToolkit
{
    public class BulkInsertAnimationPropertyByRegex : EditorWindow
    {
        private const string Version = "1.0.0";
        private const string Product = "BulkInsert Props by Regex";

        private List<EditorCurveBinding> _allBindings;
        private List<GameObject> _allObjects;
        private AnimationClip _animation;
        private string _curveRegex;
        private GameObject _gameObject;
        private string _objRegex;
        private Vector2 _scroll1;
        private Vector2 _scroll2;
        private List<EditorCurveBinding> _selectedBindings;
        private List<GameObject> _selectedObjects;

        [MenuItem("Mochizuki/VRChat/Particle Live Toolkit/BulkInsert Props by Regex")]
        private static void ShowWindow()
        {
            var window = GetWindow<BulkInsertAnimationPropertyByRegex>();
            window.titleContent = new GUIContent("BulkInsert Props by Regex");

            window.Show();
        }

        private void OnGUI()
        {
            EditorStyles.label.wordWrap = true;
            EditorGUIUtility.labelWidth = 200.0f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{Product} - {Version}");
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                EditorGUILayout.LabelField("正規表現を用いて、オブジェクトおよびプロパティを選択し、対象となったプロパティを Animation へ一括追加を行うツールです。");

            _animation = ObjectPicker("対象の Animation Clip", _animation);
            _gameObject = ObjectPicker("紐付くルート GameObject", _gameObject);
            _objRegex = EditorGUILayout.TextField("対象オブジェクトの正規表現", _objRegex);
            _curveRegex = EditorGUILayout.TextField("対象プロパティの正規表現", _curveRegex);

            using (new EditorGUI.DisabledGroupScope(string.IsNullOrWhiteSpace(_objRegex) || string.IsNullOrWhiteSpace(_curveRegex)))
            {
                if (GUILayout.Button("オブジェクトを検索する"))
                {
                    _selectedObjects = new List<GameObject>();

                    try
                    {
                        var regex = new Regex(_objRegex, RegexOptions.Singleline);

                        _allObjects = _gameObject.GetComponentsInChildren<Transform>(true)
                                                 .Where(w => regex.IsMatch(AnimationUtility.CalculateTransformPath(w, _gameObject.transform)))
                                                 .Select(w => w.gameObject)
                                                 .ToList();
                    }
                    catch
                    {
                        _allObjects = new List<GameObject>();
                    }

                    _selectedObjects.AddRange(_allObjects);
                }

                if (GUILayout.Button("プロパティを検索する"))
                {
                    _allBindings = new List<EditorCurveBinding>();
                    _selectedBindings = new List<EditorCurveBinding>();

                    try
                    {
                        var regex = new Regex(_curveRegex, RegexOptions.Singleline);

                        foreach (var binding in _allObjects.SelectMany(w => AnimationUtility.GetAnimatableBindings(w, _gameObject)))
                        {
                            if (!regex.IsMatch(binding.propertyName) || _allBindings.Any(w => w.propertyName == binding.propertyName))
                                continue;

                            _allBindings.Add(binding);
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    _selectedBindings.AddRange(_allBindings);
                }
            }

            EditorGUILayout.LabelField("対象のオブジェクト一覧");

            using (var view = new EditorGUILayout.ScrollViewScope(_scroll1, GUILayout.ExpandHeight(false)))
            {
                _scroll1 = view.scrollPosition;

                if (_allObjects != null)
                    foreach (var gameObject in _allObjects)
                    {
                        var rect = EditorGUILayout.GetControlRect(true);
                        var b = EditorGUI.Toggle(rect, _selectedObjects.Contains(gameObject));
                        if (_selectedObjects.Contains(gameObject) && !b)
                            _selectedObjects.Remove(gameObject);
                        if (!_selectedObjects.Contains(gameObject) && b)
                            _selectedObjects.Add(gameObject);

                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUIUtility.labelWidth -= 24;

                            rect.x += 24;
                            ObjectPicker(rect, gameObject.name, gameObject);

                            EditorGUIUtility.labelWidth += 24;
                        }
                    }
            }

            EditorGUILayout.LabelField("対象のプロパティ一覧");

            using (var view = new EditorGUILayout.ScrollViewScope(_scroll2, GUILayout.ExpandHeight(false)))
            {
                _scroll2 = view.scrollPosition;

                if (_allBindings != null)
                    foreach (var binding in _allBindings)
                    {
                        var b = EditorGUILayout.ToggleLeft(binding.propertyName, _selectedBindings.Any(w => w.propertyName == binding.propertyName));
                        if (_selectedBindings.Any(w => w.propertyName == binding.propertyName) && !b)
                            _selectedBindings.Remove(binding);
                        if (_selectedBindings.All(w => w.propertyName != binding.propertyName) && b)
                            _selectedBindings.Add(binding);
                    }
            }

            using (new EditorGUI.DisabledGroupScope(_animation == null || _gameObject == null))
            {
                if (GUILayout.Button("追加する"))
                    OnSubmit(_animation, _gameObject, _selectedObjects, _selectedBindings);
            }
        }

        private static void OnSubmit(AnimationClip animation, GameObject go, List<GameObject> objects, List<EditorCurveBinding> bindings)
        {
            foreach (var gameObject in objects)
            {
                var values = AnimationUtility.GetAnimatableBindings(gameObject, go).Where(w => bindings.Any(v => v.propertyName == w.propertyName));
                var curve = AnimationCurve.Constant(0, 1 / 60.0f, 0);

                foreach (var binding in values)
                    if (binding.isPPtrCurve)
                        AnimationUtility.SetObjectReferenceCurve(animation, binding, new ObjectReferenceKeyframe[] { });
                    else
                        AnimationUtility.SetEditorCurve(animation, binding, curve);
            }
        }

        private static T ObjectPicker<T>(string label, T obj) where T : Object
        {
            return EditorGUILayout.ObjectField(new GUIContent(label), obj, typeof(T), true) as T;
        }

        private static T ObjectPicker<T>(Rect rect, string label, T obj) where T : Object
        {
            return EditorGUI.ObjectField(rect, new GUIContent(label), obj, typeof(T), true) as T;
        }
    }
}