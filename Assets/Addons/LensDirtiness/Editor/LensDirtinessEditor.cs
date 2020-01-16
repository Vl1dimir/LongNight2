/*=============================================================================
CHANGELOG 
			- april 2014
				* Fixed parameter update in editor mode
				* Added Scene Tints Bloom checker             
            
            - June 2014
				* Added bloom intensity
                * Undo support
 
=============================================================================*/

using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(LensDirtiness))]

public class LensDirtinessEditor : Editor
{
    LensDirtiness _target;
    void Awake()
    {
         _target = (LensDirtiness)target;
    }
	public override void OnInspectorGUI ()
	{        
        Undo.RecordObject(_target, "Changes");
        
        _target.gain = EditorGUILayout.Slider("Threshold gain", _target.gain, 0, 10);
		_target.threshold = EditorGUILayout.Slider ("Threshold", _target.threshold, 0, 10);
		_target.BloomSize = EditorGUILayout.Slider ("Bloom Size", _target.BloomSize, 0, 50);
        _target.Downsampling = (int)EditorGUILayout.Slider("Downsampling", _target.Downsampling, 1, 6);
        _target.Iterations = (int)EditorGUILayout.Slider("Bloom Iterations", _target.Iterations, 1, 10);
		_target.Dirtiness = EditorGUILayout.Slider ("Dirtiness", _target.Dirtiness, 0, 10);
		_target.BloomColor = EditorGUILayout.ColorField ("Bloom Color", _target.BloomColor);
        _target.BloomIntensity = EditorGUILayout.Slider("Bloom intensity", _target.BloomIntensity, 1, 5);
		_target.DirtinessTexture = (Texture2D)EditorGUILayout.ObjectField ("Dirtiness Texture", _target.DirtinessTexture, typeof(Texture2D), true);
        _target.SceneTintsBloom = EditorGUILayout.Toggle("Scene Tints Bloom", _target.SceneTintsBloom);
		_target.ShowScreenControls = EditorGUILayout.Toggle("Show Screen Controls", _target.ShowScreenControls);
       
		EditorGUILayout.HelpBox("v 1.07 June 2014.", MessageType.None);
        if (GUI.changed)
        {            
            EditorUtility.SetDirty(target);
        }
    }
    
}