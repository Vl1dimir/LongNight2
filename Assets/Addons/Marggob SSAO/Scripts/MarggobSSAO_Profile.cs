using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Marggob.SSAO
{
    [CreateAssetMenu(fileName = "New Marggob SSAO Properties", menuName = "Marggob/SSAO Properties", order = 51)]
    public class MarggobSSAO_Profile : ScriptableObject
    {
        [SerializeField]
        private new string name = "New";
        [SerializeField]
        private MarggobSSAO_Properties marggobSSAO_Properties = new MarggobSSAO_Properties();

        public MarggobSSAO_Properties _MarggobSSAO_Properties { get => marggobSSAO_Properties; set => marggobSSAO_Properties = value; }
        public string Name => name;
    }

    #if UNITY_EDITOR
    [CanEditMultipleObjects, CustomEditor(typeof(MarggobSSAO_Profile))]
    public class MarggobSSAO_Properties_Editor : Editor
    {
        private MarggobSSAO_Profile window;

        private void OnEnable()
        {
            window = target as MarggobSSAO_Profile;
        }

        public override void OnInspectorGUI()
        {
            GUIStyle Header = new GUIStyle(EditorStyles.boldLabel);
            Header.alignment = TextAnchor.LowerCenter;
            Header.fontSize = 14;
            EditorGUILayout.LabelField("Marggob SSAO Profile", Header, GUILayout.Height(20));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                window.name = EditorGUILayout.TextField("Name", window.name);
                GUILayout.Space(5);
                MarggobSSAO_Properties.MarggobSSAO_Properties_Drawer.DoLayout(window._MarggobSSAO_Properties);
            }
            EditorGUILayout.EndVertical();
        }
    }
    #endif
}
