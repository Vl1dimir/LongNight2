using UnityEngine;
using Marggob.SSAO;

public class SwitchSSAOProfiles : MonoBehaviour
{
    #region Properties        
        private MarggobSSAO marggobSSAO;
    #endregion

    private void Init()
    {
        if(marggobSSAO == null)
            marggobSSAO = GetComponent<MarggobSSAO>();
    }

    private void ChangeSSAOQuality()
    {
        if (Input.GetKey("1"))
            marggobSSAO._ProfileIndex = 0;
        if (Input.GetKey("2"))
            marggobSSAO._ProfileIndex = 1;
        if (Input.GetKey("3"))
            marggobSSAO._ProfileIndex = 2;
        if (Input.GetKey("4"))
            marggobSSAO._ProfileIndex = 3;
        if (Input.GetKey("5"))
            marggobSSAO._ProfileIndex = 4;
    }

    private void Update()
    {
        Init();
        ChangeSSAOQuality();
    }


    
}
