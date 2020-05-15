using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Marggob.SSAO
{
    using UnityEngine.Rendering;

    [ExecuteAlways, RequireComponent(typeof(Camera)), AddComponentMenu("Image Effects/MargGob SSAO"), ImageEffectAllowedInSceneView]
    public class MarggobSSAO : MonoBehaviour
    {
        #region Public Properties
            public enum DebugMode
            {
                None,
                AO
            };

            [SerializeField] private MarggobSSAO_Properties marggobSSAO_Properties = new MarggobSSAO_Properties();
            [SerializeField] private List<MarggobSSAO_Profile> marggobSSAO_Profile_List;
            [SerializeField] private DebugMode debugMode = DebugMode.None;
            [SerializeField] private int profileIndex;

            public MarggobSSAO_Properties _MarggobSSAO_Properties       { get => marggobSSAO_Properties; set => marggobSSAO_Properties = value; }
            public DebugMode _DebugMode                                 { get => debugMode; set => debugMode = value; }        
            public List<MarggobSSAO_Profile> _MarggobSSAO_Profile_List  { get => marggobSSAO_Profile_List; set => marggobSSAO_Profile_List = value; }
            public int _ProfileIndex                                    
            { 
                get => profileIndex; 
                set 
                {
                    profileIndex = value;
                    if (value >= 0
                        && _MarggobSSAO_Profile_List.Count - 1 >= value
                        && _MarggobSSAO_Profile_List[value] != null
                        && marggobSSAO_Properties != _MarggobSSAO_Profile_List[value]._MarggobSSAO_Properties)
                        marggobSSAO_Properties = _MarggobSSAO_Profile_List[value]._MarggobSSAO_Properties;
                }
            }

        #endregion

        #region Private Resources

            private const string NAME = "Marggob SSAO";
            private const CameraEvent CAMERA_EVENT = CameraEvent.AfterReflections; 
            private CommandBuffer commandBuffer;
            private Camera cam => GetComponent<Camera>();
            private int CurFrame; 
            Matrix4x4 previousVP = Matrix4x4.identity;
            [SerializeField, HideInInspector] public Texture2D _Noise;
            private Material material;
            private Rect screenResCur;
        #endregion

        #region MonoBehaviour Functions

        private void UpdateVariables()
        {
            material.shaderKeywords = null;            
            material.EnableKeyword("_MODE_" + marggobSSAO_Properties._Mode.ToString());
            material.EnableKeyword("_SAMPLES_" + marggobSSAO_Properties._SamplesCount.ToString());
            material.SetTexture("_Noise", _Noise);

            material.SetFloat("_scale", marggobSSAO_Properties._Scale);
            material.SetFloat("_Threshold", marggobSSAO_Properties._Threshold);
            material.SetFloat("_power", marggobSSAO_Properties._Power);
            material.SetInt("_CurFrame", CurFrame);
        }

        private void Init()
	    {
            if(material == null)
                material = new Material(Shader.Find("Hidden/MargGob SSAO"));
            
            commandBuffer = new CommandBuffer();
			commandBuffer.name = NAME;
            cam.AddCommandBuffer(CAMERA_EVENT, commandBuffer);

            var rt_1        = Shader.PropertyToID("_RT_1"); 
			var rt_2        = Shader.PropertyToID("_RT_2");
            var rt_3        = Shader.PropertyToID("_RT_3");
            var oldFrame    = Shader.PropertyToID("_OldFrame");
			
			commandBuffer.GetTemporaryRT(rt_1, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
			commandBuffer.GetTemporaryRT(rt_2, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            commandBuffer.GetTemporaryRT(rt_3, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Point,    RenderTextureFormat.ARGBHalf);            
            commandBuffer.GetTemporaryRT(oldFrame, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32); 

            UpdateVariables();

            commandBuffer.Blit(null, rt_3, material, 0); 
            commandBuffer.SetGlobalTexture("_normalDepth", rt_3);

            // Calculate AO    
            commandBuffer.Blit(null, rt_2, material, 1);  

            // blur horizontal
            commandBuffer.Blit(rt_2, rt_1, material, 2);
            // blur vertical
            commandBuffer.Blit(rt_1, rt_2, material, 3); 

            // Temporal denoising
            commandBuffer.Blit(rt_2, rt_1, material, 4);
            // Save  temporal denoising result
            commandBuffer.Blit(rt_1, oldFrame);
            commandBuffer.SetGlobalTexture("_OldFrame", oldFrame);

            // Apply AO to ambient
            commandBuffer.Blit(rt_1, rt_3, material, 5);
            commandBuffer.Blit(rt_3, BuiltinRenderTextureType.CameraTarget);

            // Apply AO to reflection            
            commandBuffer.Blit(rt_1, rt_2, material, 6);                                   // calculate reflection occlusion
            commandBuffer.SetGlobalTexture("_ReflectionOcclusion", rt_2);
            commandBuffer.Blit(BuiltinRenderTextureType.Reflections, rt_3);                // copy reflection pass            
            commandBuffer.Blit(rt_3, BuiltinRenderTextureType.Reflections, material, 7);   // aply reflection occlusion

            // Reprojeection into the previous view matrix
            material.SetMatrix("_Reprojection", previousVP * transform.localToWorldMatrix);
            var p = cam.nonJitteredProjectionMatrix;
            var v = cam.worldToCameraMatrix;
            previousVP = GL.GetGPUProjectionMatrix(p, true) * v;
        }

#if UNITY_EDITOR
        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if(debugMode.ToString() == "None")
                Graphics.Blit(src, dest);
            else if(debugMode.ToString() == "AO")
                Graphics.Blit(src, dest, material, 8);
        }
#endif

        private void Cleanup()
        {
            if (commandBuffer != null)
                commandBuffer.Clear();
            foreach (var buf in cam.GetCommandBuffers(CAMERA_EVENT))
                if (buf.name == NAME)
                    cam.RemoveCommandBuffer(CAMERA_EVENT, buf);
            if (material != null)
                material = null;
        }

        private void Awake()
        {   
            Cleanup();
            Init();
            UpdateVariables();
        }
        private void OnEnabled()
        {            
            Cleanup();
            Init();
            UpdateVariables();
        } 

        private void Start()
        {
            Cleanup();
            Init();
            UpdateVariables();
        }

        void OnPreRender()
	    {    
            Cleanup();
            Init();
            UpdateVariables();
        }

        private void Update()
        {         
            if(material == null)
                material = new Material(Shader.Find("Hidden/MargGob SSAO"));

            if(screenResCur != cam.pixelRect)
            {
                screenResCur = cam.pixelRect; 
                Cleanup();
                Init();
            }

            if(CurFrame == 15)
                CurFrame = 0;
            else
                CurFrame ++; 
            
            UpdateVariables();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }        
        #endregion
    }
}
