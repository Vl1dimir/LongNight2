using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

[CreateAssetMenu(fileName = "New Load Stack", menuName = "TheProxor/Load Stack")]
public class LoadStack : ScriptableObject
{
    [SerializeField]
    private string stackName = string.Empty;
    [SerializeField, Multiline]
    private string sceneDescription;
    [SerializeField]

    private Sprite background;
#if UNITY_EDITOR
    [SerializeField]
    private SceneAsset[] scenesAssets;
#endif

    [SerializeField, HideInInspector]
    private string[] scenes;


    public string Name => name;
    public string SceneDescription => sceneDescription;
    public Sprite Background => background;
    public string[] Scenes => scenes;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        scenes = new string[scenesAssets.Length];
        for (int i = 0; i < scenes.Length; i++)
            if (scenesAssets[i] != null)
                scenes[i] = scenesAssets[i].name;
    }

    [CustomEditor(typeof(LoadStack))]
    private class LoadStack_Editor : Editor
    {
        private LoadStack Window { get; set; }

        private void OnEnable()
        {
            Window = (LoadStack)target;
            Window.stackName = Window.stackName == string.Empty ? Window.name : Window.stackName;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
    #endif
}
