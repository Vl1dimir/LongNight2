using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindWater_Dev : MonoBehaviour
{
    public Camera[] cams;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Find")]
    public void FindCam()
    {
        cams = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (var cam in cams)
            cam.hideFlags = HideFlags.None;
    }
}
