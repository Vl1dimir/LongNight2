/*

===============================================================================
CHANGELOG 
			- april 2014
				* Removed manual UV flip, not needed anymore, fixed in shader
				* Fixed dirtiness texture format
				* Bloom code is now inside a loop
                * Added shader keyword for bloom tint
 
            - may 2014
				* Added options for downsampling & iterations
				* Render to texture format is now set by Unity source texture
                * Bloom is now done with Gaussian filter instead Kawase
 
            - June 2014
				* Bloom code redone
 
===============================================================================
 
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
[AddComponentMenu("Image Effects/Lens Dirtiness")]

public class LensDirtiness : MonoBehaviour
{
	private Shader Shader_Dirtiness;
	private Material Material_Dirtiness;
	private int ScreenX = 1280, ScreenY = 720;
	public bool ShowScreenControls = false, SceneTintsBloom = true;
	public Texture2D DirtinessTexture;
    public int Iterations = 4, Downsampling=4;
    enum Pass
    {   
        Threshold = 0,
        Kawase = 1,
        Compose = 2,      
        Gaussian=3,
    };
	//Effect parameters
	public float gain = 1.0f, threshold = 1.0f, BloomSize = 5.0f, Dirtiness = 1.0f, BloomIntensity=1;
	public Color BloomColor = Color.white;

    void OnEnable()
	{
		//Create Material
		Shader_Dirtiness = Shader.Find ("Hidden/LensDirtiness");
		if (Shader_Dirtiness == null)
			Debug.Log ("#ERROR# Hidden/LensDirtiness Shader not found");
		Material_Dirtiness = new Material (Shader_Dirtiness);
        Material_Dirtiness.hideFlags = HideFlags.HideAndDontSave;

        SetKeyword();
	}

  

    void SetKeyword()
    {
        if (!Material_Dirtiness)
            return;
        if (SceneTintsBloom)
            Material_Dirtiness.EnableKeyword("_SCENE_TINTS_BLOOM");
        else
            Material_Dirtiness.DisableKeyword("_SCENE_TINTS_BLOOM");

    }

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
        #if UNITY_EDITOR
        SetKeyword();
        #endif

		ScreenX = source.width;
		ScreenY = source.height;
        int RTT_Width = ScreenX;
        int RTT_Height = ScreenY;

		//Send Parameters
		Material_Dirtiness.SetFloat ("_Gain", gain);
		Material_Dirtiness.SetFloat ("_Threshold", threshold);
        Material_Dirtiness.SetFloat("_BloomIntensity", BloomIntensity);
		
		//Get first sample for thersholding
        RenderTexture RTT_BloomThreshold = RenderTexture.GetTemporary(ScreenX, ScreenY, 0, source.format);
        Graphics.Blit(source, RTT_BloomThreshold, Material_Dirtiness, (int)Pass.Threshold);
        RenderTexture.ReleaseTemporary(RTT_BloomThreshold);
        Material_Dirtiness.SetVector("_Offset", new Vector4(1f / ScreenX, 1f / ScreenY, 0, 0));

        for (int i = 1; i <= Downsampling; i++)
        {
            RTT_Width /= 2;
            RTT_Height /= 2;
                               
            RenderTexture RTT_Bloom_1 = RenderTexture.GetTemporary(RTT_Width, RTT_Height, 0, source.format);
            Graphics.Blit(RTT_BloomThreshold, RTT_Bloom_1, Material_Dirtiness, (int)Pass.Kawase);
            RTT_BloomThreshold = RTT_Bloom_1;

            for (int iteration = 1; iteration <= Iterations; iteration++)
            {
                float OffsetX = (BloomSize + iteration / Iterations) / ScreenX;
                float OffsetY = (BloomSize + iteration / Iterations) / ScreenY;

                RenderTexture RTT_Bloom_2 = RenderTexture.GetTemporary(RTT_Width, RTT_Height, 0, source.format);

                Material_Dirtiness.SetVector("_Offset", new Vector4(0, OffsetY, 0, 0));
                Graphics.Blit(RTT_Bloom_1, RTT_Bloom_2, Material_Dirtiness, (int)Pass.Gaussian);
                RenderTexture.ReleaseTemporary(RTT_Bloom_1);
                RTT_Bloom_1=RTT_Bloom_2;

                RTT_Bloom_2 = RenderTexture.GetTemporary(RTT_Width, RTT_Height, 0, source.format);
                Material_Dirtiness.SetVector("_Offset", new Vector4(OffsetX, 0, 0, 0));
                Graphics.Blit(RTT_Bloom_1, RTT_Bloom_2, Material_Dirtiness, (int)Pass.Gaussian);
                RenderTexture.ReleaseTemporary(RTT_Bloom_1);
                RTT_Bloom_1=RTT_Bloom_2;                               
                 
            }
            RenderTexture.ReleaseTemporary(RTT_Bloom_1);
            Material_Dirtiness.SetTexture("_Bloom", RTT_Bloom_1);
            
            
            //Debug.Log(RTT_Bloom_2.width);
        }
		Material_Dirtiness.SetFloat ("_Dirtiness", Dirtiness);
		Material_Dirtiness.SetColor ("_BloomColor", BloomColor);
		Material_Dirtiness.SetTexture ("_DirtinessTexture", DirtinessTexture);
        
        //Graphics.Blit(RTT_BloomThreshold, destination);
         Graphics.Blit(source, destination, Material_Dirtiness, (int)Pass.Compose);
        //Debug.Log(GC.GetTotalMemory(false) + " Used Memory Merge Raster");
        //GC.Collect();
        //Debug.Log(GC.GetTotalMemory(true) + " Mem after GC  Merge Raster");
        
	}
	
	void OnGUI ()
	{

		if (ShowScreenControls) {
			float SliderLeftMargin = 150;

			GUI.Box (new Rect (15, 15, 250, 200), "");
			

			
			//Gain
			GUI.Label (new Rect (25, 25, 100, 20), "Gain= " + gain.ToString ("0.0"));
			gain = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 30, 100, 20), gain, 0.0f, 10.0f);
			
			//threshold
			GUI.Label (new Rect (25, 45, 100, 20), "Threshold= " + threshold.ToString ("0.0"));
			threshold = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 50, 100, 20), threshold, 0.0f, 10.0f);
			
			//BloomSize
			GUI.Label (new Rect (25, 65, 100, 20), "BloomSize= " + BloomSize.ToString ("0.0"));
			BloomSize = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 70, 100, 20), BloomSize, 0.0f, 10.0f);
			
			//Dirtiness
			GUI.Label (new Rect (25, 85, 100, 20), "Dirtiness= " + Dirtiness.ToString ("0.0"));
			Dirtiness = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 90, 100, 20), Dirtiness, 0.0f, 10.0f);
						
			//Color
			GUI.Label (new Rect (25, 125, 100, 20), "R= " + (BloomColor.r * 255).ToString ("0."));
			GUI.color = new Color (BloomColor.r, 0, 0);
			BloomColor.r = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 130, 100, 20), BloomColor.r, 0.0f, 1.0f);
			GUI.color = Color.white;
			GUI.Label (new Rect (25, 145, 100, 20), "G= " + (BloomColor.g * 255).ToString ("0."));
			GUI.color = new Color (0, BloomColor.g, 0);
			BloomColor.g = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 150, 100, 20), BloomColor.g, 0.0f, 1.0f);
			GUI.color = Color.white;
			GUI.Label (new Rect (25, 165, 100, 20), "R= " + (BloomColor.b * 255).ToString ("0."));
			GUI.color = new Color (0, 0, BloomColor.b);
			BloomColor.b = GUI.HorizontalSlider (new Rect (SliderLeftMargin, 170, 100, 20), BloomColor.b, 0.0f, 1.0f);
			GUI.color = Color.white;
		}
	}
}
