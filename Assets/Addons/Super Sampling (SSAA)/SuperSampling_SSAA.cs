using UnityEngine;
using System.Collections;
using SSAA;

public class SuperSampling_SSAA : MonoBehaviour 
{
    /// <summary>
    /// SSAA sampling factor. Please use ChangeScale() or disable and enable to apply changes
    /// </summary>
    public float Scale = 0.9f;

    /// <summary>
    /// If false SSAA will use your Camera.depth + 1 for the RenderTarget's Camera
    /// if true set RenderTargetCameraDepth with your desired depth
    /// </summary>
    public bool UseFixedRenderTargetCameraDepth = false;

    /// <summary>
    /// The RenderTargetCamera's depth; ignored if UseFixedRenderTargetCameraDepth == false
    /// </summary>
    public float RenderTargetCameraDepth = 100f;


    /// <summary>
    /// Editor mono-bug workaround var
    /// </summary>
    private bool turnOn = false;

    /// <summary>
    /// Editor mono-bug workaround var
    /// </summary>
    private bool applyChanges = false;

    /// <summary>
    /// The Camera's SSAA instance
    /// </summary>
    public internal_SSAA SSAA;


    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("Missing Camera on SSAA-Camera!");
            return;
        }

        SSAA = new internal_SSAA();

        SSAA.BindToCamera(cam, Scale, false);
        
    }

    void Update()
    {
        if(turnOn)
        {
            if (!SSAA.Active)
            {
                SSAA.StartSSAA();
            }
            turnOn = false;
        }
        if(applyChanges)
        {
            SSAA.ApplyChanges();
            applyChanges = false;
        }
    }

    /// <summary>
    /// Changes SSAA scale factor
    /// </summary>
    /// <param name="newScale"></param>
    /// <param name="applyChange"></param>
    public void ChangeScale(float newScale, bool applyChange = true)
    {
        Scale = newScale;
        if (SSAA != null)
            SSAA.Scale = Scale;
        if (applyChange)
        {
            if (!Application.isEditor)
                SSAA.ApplyChanges();
            else
                applyChanges = true;
        }
    }


    public void SetFixedRenderTargetCameraDepth(bool state, bool applyChange = true)
    {
        UseFixedRenderTargetCameraDepth = state;
        if (SSAA != null)
            SSAA.UseFixedRenderTargetCameraDepth = UseFixedRenderTargetCameraDepth;
        if (applyChange)
        {
            if (!Application.isEditor)
                SSAA.ApplyChanges();
            else
                applyChanges = true;
        }
    }


    public void SetRenderTargetCameraDepth(float depth, bool applyChange = true)
    {
        RenderTargetCameraDepth = depth;
        if (SSAA != null)
            SSAA.RenderTargetCameraDepth = RenderTargetCameraDepth;
        if (applyChange)
        {
            if (!Application.isEditor)
                SSAA.ApplyChanges();
            else
                applyChanges = true;
        }
    }

	void OnEnable() 
    {
        SSAA.Scale = Scale;

        if(Application.isEditor)
        {
            turnOn = true;
            return;
        }

        if (!SSAA.Active)
        {
            SSAA.StartSSAA();
        }
	}

	
	void OnDisable () 
    {
        if (SSAA.Active)
        {
            SSAA.StopSSAA();
        }
	}

    void OnDestroy()
    {
        SSAA.ReleaseCamera();
    }

  

}