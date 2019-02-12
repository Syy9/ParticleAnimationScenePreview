using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Syy.Tools
{
    public class ParticleAnimationScenePreview : EditorWindow
    {
        [MenuItem("Window/ParticleAnimationScenePreview")]
        public static void Open()
        {
            GetWindow<ParticleAnimationScenePreview>();
        }

        ParticleSystem _particleTarget;
        Animator _animTarget;
        AnimationClip _clip;
        bool _isPreviewPlaying;

        void OnEnable()
        {
            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            if (AnimationMode.InAnimationMode())
            {
                ParticleSystemEditorHelper.I.StopEffect();
                AnimationMode.StopAnimationMode();
            }
        }

        void OnGUI()
        {
            _particleTarget = (ParticleSystem)EditorGUILayout.ObjectField(_particleTarget, typeof(ParticleSystem), true);
            _animTarget = (Animator) EditorGUILayout.ObjectField(_animTarget, typeof(Animator), true);
            _clip = (AnimationClip)EditorGUILayout.ObjectField(_clip, typeof(AnimationClip), false);
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("not playing only");
            }
            else
            {
                if (GUILayout.Button("Play"))
                {
                    if (_particleTarget != null)
                    {
                        _particleTarget.Play(true);
                        ParticleSystemEditorHelper.I.PlaybackTime = 0;
                        ParticleSystemEditorHelper.I.LockedParticleSystem = _particleTarget;
                        ParticleSystemEditorHelper.I.CompleteResimulation();
                        AnimationMode.StartAnimationMode();
                        _isPreviewPlaying = true;
                    }
                }

                if (GUILayout.Button("Stop"))
                {
                    ParticleSystemEditorHelper.I.PlaybackTime = 0;
                    ParticleSystemEditorHelper.I.StopEffect();
                    ParticleSystemEditorHelper.I.LockedParticleSystem = null;

                    AnimationMode.StopAnimationMode();
                    _isPreviewPlaying = false;
                }

            }
        }

        void Update()
        {
            if (_isPreviewPlaying && AnimationMode.InAnimationMode())
            {
                if (_animTarget != null && _clip != null)
                {
                    AnimationMode.SampleAnimationClip(_animTarget.gameObject, _clip, ParticleSystemEditorHelper.I.PlaybackTime);
                }
            }
        }
    }

    public class ParticleSystemEditorHelper
    {
        private static ParticleSystemEditorHelper _instance;
        public static ParticleSystemEditorHelper I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ParticleSystemEditorHelper();
                }

                return _instance;
            }
        }

        private static Type _realType;
        private static Type _realType2;
        private static PropertyInfo _lockedParticleSystemPI;
        private static PropertyInfo _playbackTimePI;
        private static Func<float> _playbackTimeGetFunc;
        private static MethodInfo _StopEffectFunc;
        private static MethodInfo _CompleteResimulationFunc;

        private ParticleSystemEditorHelper()
        {
            var assembly = Assembly.GetAssembly(typeof(Editor));
            _realType = assembly.GetType("UnityEditor.ParticleSystemEditorUtils");
            _lockedParticleSystemPI = _realType.GetProperty("lockedParticleSystem", BindingFlags.Static | BindingFlags.NonPublic);
            _playbackTimePI = _realType.GetProperty("playbackTime", BindingFlags.Static | BindingFlags.NonPublic);
            _playbackTimeGetFunc = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), _playbackTimePI.GetGetMethod(true));
            _CompleteResimulationFunc = _realType.GetMethod("PerformCompleteResimulation", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });

            _realType2 = assembly.GetType("UnityEditor.ParticleSystemEffectUtils");
            _StopEffectFunc = _realType2.GetMethod("StopEffect", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
        }

        public ParticleSystem LockedParticleSystem { get { return (ParticleSystem) _lockedParticleSystemPI.GetValue(null, null); } set { _lockedParticleSystemPI.SetValue(null, value, null); }}
        public float PlaybackTime { get { return _playbackTimeGetFunc(); } set { _playbackTimePI.SetValue(null, value, null); } }

        public void StopEffect() => _StopEffectFunc.Invoke(null, null);
        public void CompleteResimulation() => _CompleteResimulationFunc.Invoke(null, null);

        public ParticleSystem GetRoot(ParticleSystem value)
        {
            if (value == null) return null;
            var transform = value.transform;
            while (transform.parent && transform.parent.gameObject.GetComponent<ParticleSystem>() != null)
            {
                transform = transform.parent;
            }
            return transform.gameObject.GetComponent<ParticleSystem>();
        }
    }
}
