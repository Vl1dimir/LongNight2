// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*

===============================================================================
CHANGELOG 
			- april 2014
				* Fixed UV coordinates				
				* Added option to tint bloom with scene color 

            - may 2014
				* Added Gaussian filter
				* Improved brightpass method

===============================================================================

*/

Shader "Hidden/LensDirtiness" {
	
	Properties {
	_MainTex ("Base (RGB)", 2D) = "black" {}
}

	CGINCLUDE
	
	#include "UnityCG.cginc"
	
	//Receive parameters
	uniform sampler2D		_MainTex, 
							_Bloom, 
							_DirtinessTexture;
	
	float					_Gain,
							_Threshold,
							_Iteration,
							_BloomIntensity,  
							_Dirtiness;
	
	float4					_BloomColor, 
							_Offset,
							_MainTex_TexelSize;	
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
		
	};
	
	//Common Vertex Shader
	v2f vert( appdata_img v )
	{
	v2f o;
	o.pos = UnityObjectToClipPos (v.vertex);
	
	o.uv[0] = v.texcoord.xy;
	 
	
		#if UNITY_UV_STARTS_AT_TOP
		if(_MainTex_TexelSize.y<0.0)
			o.uv[0].y = 1.0-o.uv[0].y;
		#endif
		
		o.uv[1] =  v.texcoord.xy;	
	
		return o;
	
	} 

	half4 Threshold(v2f IN) : COLOR
	{
		float2 coords = IN.uv[0];	
		float4 SceneColor = tex2D(_MainTex ,coords );
		half TotalLuminance = (SceneColor.r + SceneColor.g + SceneColor.b) / 3;
		half ThresholdLuminance = TotalLuminance * _Gain - _Threshold;
		half FinalLuminance = max(0, ThresholdLuminance / 2.0f);

		return FinalLuminance * SceneColor;
	}		
	
	
	half4 Kawase(v2f IN) : COLOR
	{
		float2 texCoord = IN.uv[0];	
		float2 texCoordSample = 0;

		float2 dUV = _Offset.xy;
		
		float4 cOut=0;
		
		// Sample top left pixel
		texCoordSample.x = texCoord.x - dUV.x;
		texCoordSample.y = texCoord.y + dUV.y;
		cOut += tex2D (_MainTex, texCoordSample);
		
		// Sample top right pixel
		texCoordSample.x = texCoord.x + dUV.x;
		texCoordSample.y = texCoord.y + dUV.y;
		cOut += tex2D (_MainTex, texCoordSample);
		
		// Sample bottom right pixel
		texCoordSample.x = texCoord.x + dUV.x;
		texCoordSample.y = texCoord.y - dUV.y;
		cOut += tex2D (_MainTex, texCoordSample);
		
		// Sample bottom left pixel
		texCoordSample.x = texCoord.x - dUV.x;
		texCoordSample.y = texCoord.y - dUV.y;
		cOut += tex2D (_MainTex, texCoordSample);
		
		// Average
		cOut *= 0.25f;
		return (cOut);
	
	}
	
	
	struct v2f_blur {
		half4 pos : POSITION;
		half2 uv : TEXCOORD0;
		half4 uv01 : TEXCOORD1;
		half4 uv23 : TEXCOORD2;
		half4 uv45 : TEXCOORD3;
		half4 uv67 : TEXCOORD4;
	};
	v2f_blur vertWithMultiCoords2 (appdata_img v) {
		v2f_blur o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv.xy = v.texcoord.xy;
		o.uv01 =  v.texcoord.xyxy + _Offset.xyxy * half4(1,1, -1,-1);
		o.uv23 =  v.texcoord.xyxy + _Offset.xyxy * half4(1,1, -1,-1) * 2.0;
		o.uv45 =  v.texcoord.xyxy + _Offset.xyxy * half4(1,1, -1,-1) * 3.0;
		o.uv67 =  v.texcoord.xyxy + _Offset.xyxy * half4(1,1, -1,-1) * 4.0;
		
		return o;  
	}

	half4 fragGaussBlur (v2f_blur i) : COLOR {
		half4 color = half4 (0,0,0,0);
		color += 0.225 * tex2D (_MainTex, i.uv);
		color += 0.150 * tex2D (_MainTex, i.uv01.xy);
		color += 0.150 * tex2D (_MainTex, i.uv01.zw);
		color += 0.110 * tex2D (_MainTex, i.uv23.xy);
		color += 0.110 * tex2D (_MainTex, i.uv23.zw);
		color += 0.075 * tex2D (_MainTex, i.uv45.xy);
		color += 0.075 * tex2D (_MainTex, i.uv45.zw);	
		color += 0.0525 * tex2D (_MainTex, i.uv67.xy);
		color += 0.0525 * tex2D (_MainTex, i.uv67.zw);
		return color;
	} 
	half4 Compose(v2f IN) : COLOR
	{		
		half2 DirtnessUV = IN.uv[0];
		half2 ScreenUV = IN.uv[1];
		float4 Final=1;
		
		#ifdef _SCENE_TINTS_BLOOM
		float4 BloomSample = tex2D(_Bloom, ScreenUV);
		#else
		float4 BloomSample = tex2D(_Bloom, ScreenUV);
		BloomSample.rgb = BloomSample.r + BloomSample.g + BloomSample.b;
		BloomSample.rgb /=3;
		#endif
		BloomSample.rgb *=_BloomIntensity;
		half4 DirtinessSample = tex2D(_DirtinessTexture, DirtnessUV);
		float4 Scene = tex2D(_MainTex, ScreenUV);
		Final.rgb = Scene.rgb + BloomSample.rgb * DirtinessSample.rgb * _Dirtiness + BloomSample.rgb * _BloomColor.rgb;
	  
		return Final;
	}

	ENDCG 
	
Subshader {

	ZTest Off
	Cull Off
	ZWrite Off
	Fog { Mode off }
	
//Pass 0 Threshold
 Pass 
 {
 Name "Threshold"

      CGPROGRAM
      #pragma fragmentoption ARB_precision_hint_fastest 
      #pragma vertex vert
      #pragma fragment Threshold
      ENDCG
  }
  
 
  
   //Pass 1 Kawase for downsampling
 Pass 
 {
 Name "Kawase"

      CGPROGRAM
	  #pragma fragmentoption ARB_precision_hint_fastest
      #pragma vertex vert
      #pragma fragment Kawase
      ENDCG
  }
  
  //Pass 2 Done, compose everything
 Pass 
 {
 Name "Compose"

      CGPROGRAM
	  #pragma fragmentoption ARB_precision_hint_fastest
	  #pragma multi_compile _ _SCENE_TINTS_BLOOM 
      #pragma vertex vert
      #pragma fragment Compose
      ENDCG
  }


  //Pass 3 Gaussian
  Pass {     

      CGPROGRAM
      
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers flash
      #pragma vertex vertWithMultiCoords2
      #pragma fragment fragGaussBlur
      
      ENDCG
  } 
}

Fallback off
	
}