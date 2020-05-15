using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Marggob.SSAO
{
    [CanEditMultipleObjects, CustomEditor(typeof(MarggobSSAO))]
    public class MarggobSSAOEditor : Editor
    {
        private MarggobSSAO window;
        private ReorderableList RListProfiles;
        private MarggobSSAO_Properties marggobSSAO_Properties;
        private bool update = false;

        private (string[], List<MarggobSSAO_Profile>) RealProfilesData
        {
            get
            {
                List<string> resultNames = new List<string>(window._MarggobSSAO_Profile_List.Count);
                List<MarggobSSAO_Profile> resultProfiles = new List<MarggobSSAO_Profile>(window._MarggobSSAO_Profile_List.Count);
                foreach (var profile in window._MarggobSSAO_Profile_List)
                {
                    if (profile != null)
                    {
                        resultNames.Add(profile.name);
                        resultProfiles.Add(profile);
                    }
                }
                return (resultNames.ToArray(), resultProfiles);
            }
        }

        private bool CheckDisabled => window.GetComponent<Camera>()?.actualRenderingPath != RenderingPath.DeferredShading;

        private void OnEnable()
        {
            window = target as MarggobSSAO;

            if (window._MarggobSSAO_Profile_List == null)
                window._MarggobSSAO_Profile_List = new List<MarggobSSAO_Profile>();

            if (RListProfiles == null)
            {
                RListProfiles = new ReorderableList(window._MarggobSSAO_Profile_List, typeof(MarggobSSAO_Profile), true, true, true, true);
                RListProfiles.elementHeight = 16;

                RListProfiles.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    window._MarggobSSAO_Profile_List[index] = (MarggobSSAO_Profile)EditorGUI.ObjectField(rect, "Profile " + index, window._MarggobSSAO_Profile_List[index], typeof(MarggobSSAO_Profile), false);
                };


                RListProfiles.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Profiles List:");
                };

                RListProfiles.onAddCallback = (list) =>
                {
                    window._MarggobSSAO_Profile_List.Add(null);
                };

                RListProfiles.onRemoveCallback = (list) =>
                {
                    window._MarggobSSAO_Profile_List.RemoveAt(list.index);
                };
            }

            if (window._MarggobSSAO_Profile_List.Count - 1 >= window._ProfileIndex && window._MarggobSSAO_Profile_List[window._ProfileIndex] == null)
                window._ProfileIndex = -1;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle Header = new GUIStyle(EditorStyles.boldLabel);
                Header.alignment = TextAnchor.LowerCenter;
                Header.fontSize = 14;
                EditorGUILayout.LabelField("Marggob SSAO", Header, GUILayout.Height(20));

                if (CheckDisabled)
                    EditorGUILayout.HelpBox("To enable the effect, change Rendering Path to Deferred.", MessageType.Warning);
                else
                {
                    Header.fontSize = 10;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.LabelField("Profile Browser", Header, GUILayout.Height(20));
                        GUILayout.Space(5);
                        RListProfiles.DoLayoutList();
                        GUILayout.Space(5);

                        var realData = RealProfilesData;
                        var names = realData.Item1;
                        var profiles = realData.Item2;
                        if (window._MarggobSSAO_Profile_List != null && window._MarggobSSAO_Profile_List.Count > 0 && names.Length > 0)
                        {
                            window._ProfileIndex = EditorGUILayout.Popup("Quality profile", window._ProfileIndex, names);
                            GUILayout.Space(5);                            
                            update = true;
                        }
                        else
                        {
                            if (update)
                            {
                                update = false;
                                window._MarggobSSAO_Properties = new MarggobSSAO_Properties();
                            }
                            EditorGUILayout.HelpBox("List of profiles can't be a null or empty!", MessageType.Warning);
                        }
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(10);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.LabelField("Current Properties", Header, GUILayout.Height(20));
                        GUILayout.Space(5);
                        MarggobSSAO_Properties.MarggobSSAO_Properties_Drawer.DoLayout(window._MarggobSSAO_Properties);
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(10);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.LabelField("Debug", Header, GUILayout.Height(20));
                        GUILayout.Space(5);
                        window._DebugMode = (MarggobSSAO.DebugMode)EditorGUILayout.EnumPopup("Debug Mode", window._DebugMode);             
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
