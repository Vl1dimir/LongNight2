using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Marggob.SSAO
{
    [System.Serializable]
    public class MarggobSSAO_Properties
    {
        public enum Mode{ Scale, Distance_Scale };
        public enum SamplesCount { Four, Six, Eight, Twelve, Sixteen };
        [SerializeField] private Mode mode = Mode.Distance_Scale;
        [SerializeField] private SamplesCount samplesCount = SamplesCount.Eight;
        [SerializeField, Range(0.3f, 1.0f)] private float scale = 0.3f;
        [SerializeField, Range(1.0f, 2.0f)] private float power = 1.0f;
        [SerializeField, Range(0.05f, 1.0f)] private float threshold = 0.3f;        

        public Mode _Mode                   { get => mode;                  set => mode = value; }
        public SamplesCount _SamplesCount   { get => samplesCount;          set => samplesCount = value; }         
        public float _Scale                 { get => scale;                 set => scale = value; }
        public float _Power                 { get => power;                 set => power = value; }
        public float _Threshold             { get => threshold;             set => threshold = value; }        

        #if UNITY_EDITOR
        public static class MarggobSSAO_Properties_Drawer
        {
            public static void DoLayout(MarggobSSAO_Properties marggobSSAO_Properties)
            {      
                marggobSSAO_Properties.mode = (Mode)EditorGUILayout.EnumPopup("Mode", marggobSSAO_Properties.mode);
                marggobSSAO_Properties.samplesCount = (SamplesCount)EditorGUILayout.EnumPopup("Rays Count", marggobSSAO_Properties.samplesCount);
                marggobSSAO_Properties.scale = EditorGUILayout.Slider("Scale", marggobSSAO_Properties.scale, 0.3f, 1.0f);
                marggobSSAO_Properties.power = EditorGUILayout.Slider("Power", marggobSSAO_Properties.power, 1f, 2.0f);
                marggobSSAO_Properties.threshold = EditorGUILayout.Slider("Threshold", marggobSSAO_Properties.threshold, 0.05f, 1.0f);                
            }
        }
        #endif
    }
}