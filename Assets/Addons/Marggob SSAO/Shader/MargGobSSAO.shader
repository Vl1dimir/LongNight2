Shader "Hidden/MargGob SSAO"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Normal and Depth"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;
			sampler2D _CameraGBufferTexture2;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				return float4(
					tex2D(_CameraGBufferTexture2, i.uv).xyz,
					SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)
				);
			}
			ENDCG
		}
		
		Pass
		{
			Name "Calculate AO"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define UNITY_GBUFFER_INCLUDED
			#include "UnityCG.cginc" 
			#include "UnityGBuffer.cginc"

			#pragma multi_compile _MODE_Scale _MODE_Distance_Scale
			#pragma multi_compile _SAMPLES_Four _SAMPLES_Six _SAMPLES_Eight _SAMPLES_Twelve _SAMPLES_Sixteen
					
			sampler2D _CameraGBufferTexture0, _CameraGBufferTexture1, _CameraGBufferTexture2;
			sampler2D_float _CameraDepthTexture;
			sampler2D _Noise;
			float _scale, _Threshold, _power;
			float3 samplesDir [16];
			int samplesCount, _CurFrame;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half4 gbuffer0 = tex2D (_CameraGBufferTexture0, i.uv);
    			half4 gbuffer1 = tex2D (_CameraGBufferTexture1, i.uv);

				float aoMask = max(step(0.001, gbuffer0.xyz), step(0.001, gbuffer1.xyz));				
				
				// Sample a view-space normal vector on the g-buffer.
				float3 norm_o = tex2D(_CameraGBufferTexture2, i.uv).xyz;
				norm_o = mul((float3x3)unity_WorldToCamera, norm_o * 2 - 1);	

				// Sample a linear depth on the depth buffer.
				float depth_o = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				depth_o = LinearEyeDepth(depth_o);

				// Reconstruct the view-space position.
				float3x3 proj = (float3x3)unity_CameraProjection;
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
				float3 pos_o = float3((i.uv * 2 - 1 - p13_31) / p11_22, 1) * depth_o + norm_o * 0.003 * depth_o;

				//Noise Texture
				const float2 noiseUVoffset[16] = {
					0.0, 0.0,	0.5, 0.0,	0.75, 0.0,	0.25, 0.0,
					0.0, 0.5,	0.5, 0.25,	0.75, 0.5,	0.25, 0.25,
					0.0, 0.75,	0.5, 0.75,	0.75, 0.75,	0.25, 0.75,
					0.0, 0.25,	0.5, 0.5,	0.75, 0.25,	0.25, 0.5,
				};

				float4 noise = tex2D(_Noise, i.uv / 4 * _ScreenParams.xy + noiseUVoffset[_CurFrame]); 
				noise.xyz = noise.xyz * 2 - 1;
								
				#ifdef _SAMPLES_Four
					samplesCount = 4;
					samplesDir [0] = float3(0.0, 0.0, 1.0);						samplesDir [1] = float3(-0.816497, 0.471405, -0.333333);
					samplesDir [2] = float3(0.0, -0.942809, -0.333333);			samplesDir [3] = float3(0.816497, 0.471405, -0.333333);					
				#elif _SAMPLES_Six	
					samplesCount = 6;
					samplesDir [0] = float3(0.0, 0.0, 1.0);						samplesDir [1] = float3(0.0, 0.0, -1.0);
					samplesDir [2] = float3(0.0, 1.0, 0.0);						samplesDir [3] = float3(0.0, -1.0, 0.0);	
					samplesDir [4] = float3(1.0, 0.0, 0.0);						samplesDir [5] = float3(-1.0, 0.0, 0.0);	
				#elif _SAMPLES_Eight
					samplesCount = 8;
					samplesDir [0] = float3(0.0, 0.0, 1.0);						samplesDir [1] = float3(0.0, 0.0, -1.0);
					samplesDir [2] = float3(-0.816497, -0.471405, 0.333333);	samplesDir [3] = float3(0.0, 0.942809, 0.333333);	
					samplesDir [4] = float3(0.816497, -0.471405, 0.333333);		samplesDir [5] = float3(-0.816497, 0.471405, -0.333333);	
					samplesDir [6] = float3(0.0, -0.942809, -0.333333);			samplesDir [7] = float3(0.816497, 0.471405, -0.333333);	
				#elif _SAMPLES_Twelve
					samplesCount = 12;
					samplesDir [0] = float3(0.0, 0.0, 1.0);						samplesDir [1] = float3(0.0, -0.894427, 0.447214);
					samplesDir [2] = float3(0.850651, -0.276393, 0.447214);		samplesDir [3] = float3(-0.850651, -0.276393, 0.447214);	
					samplesDir [4] = float3(0.525731, 0.723607, 0.447214);		samplesDir [5] = float3(-0.525731, 0.723607, 0.447214);	
					samplesDir [6] = float3(0.525731, -0.723607, -0.447214);	samplesDir [7] = float3(-0.525731, -0.723607, -0.447214);	
					samplesDir [8] = float3(0.850651, 0.276393, -0.447214);		samplesDir [9] = float3(-0.850651, 0.276393, -0.447214);	
					samplesDir [10] = float3(0.0, 0.894427, -0.447214);			samplesDir [11] = float3(0.0, 0.0, -1.0);
				#elif _SAMPLES_Sixteen
					samplesCount = 16;
					samplesDir [0] = float3(0.0, 0.0, -1.0);		samplesDir [1] = float3(-0.83, -0.47, 0.33);
					samplesDir [2] = float3(0.8, -0.47, 0.33);		samplesDir [3] = float3(0.0, 0.95, 0.33);	
					samplesDir [4] = float3(-0.43, 0.24, 0.87);		samplesDir [5] = float3(-0.44, 0.73, -0.53);	
					samplesDir [6] = float3(0.85, 0.0, -0.53);		samplesDir [7] = float3(-0.43, -0.75, -0.53);	
					samplesDir [8] = float3(0.0, -0.5, 0.87);		samplesDir [9] = float3(0.85, 0.49, 0.17);	
					samplesDir [10] = float3(0.0, -1.0, 0.17);		samplesDir [11] = float3(-0.86, 0.0, -0.53);
					samplesDir [12] = float3(0.42, 0.24, 0.87);		samplesDir [13] = float3(0.42, -0.75, -0.53);	
					samplesDir [14] = float3(-0.86, 0.49, 0.17);	samplesDir [15] = float3(0.43, 0.73, -0.53);
				#endif			

				float stepsLength [3] ={
					0.33, 0.66, 1.0 
					//0.25, 0.5,  1.0 // alternative variant					
				};

				float4 occ = 0;

				float depthPow = pow(depth_o, 0.45);
				float checkBias = 0.01 * depth_o;				
				_Threshold *= depth_o;
				_scale *= noise.a;

				#ifdef _MODE_Distance_Scale
					_scale *= depthPow;
				#endif

				for (int s = 0; s < samplesCount; s++) 
				{
					// Hemispherical random vector
					float3 delta = normalize(reflect(samplesDir[s], noise) + norm_o);
					delta = faceforward(delta, -norm_o, delta);

					for (int r = 0; r < 3; r++)
					{						
						//Ray distance
						float rayStep = stepsLength[r] * _scale;
						//Get sampled point
						float3 pos_s0 = pos_o + delta * rayStep;							
						//Re-project the sampling point
						float2 uv_s = ( mul(proj, pos_s0).xy / pos_s0.z + 1 ) * 0.5;						
						//Check intersection with geometry
						float checkIntersection = pos_s0.z - LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv_s)).r;
						// Check near surface
						float check = step(0, checkIntersection);
						// Check depth from surface to ray point
						check *= 1 - step(rayStep / _Threshold, checkIntersection - rayStep - checkBias); 
						// Invert result
						check = 1 - check;
						// Summ result
						occ.a += check;
						// Calculate fast approximate bent normal
						occ.xyz += lerp(norm_o - delta, delta, check);
						// Break cycle if ray check surface
						if(check < 1)
							break;
					}
				}

				occ.a = occ.a / (samplesCount * 3);
				occ.a = pow(occ.a, 3 - occ.a);

				occ.xyz = normalize(lerp(occ.xyz, norm_o, occ.a * occ.a * occ.a * occ.a));	
				occ.xyz = mul(unity_CameraToWorld, occ.xyz) * 0.5 + 0.5;
				
				occ.a = lerp(1, pow(occ.a, _power), aoMask);

				return occ;		
			}
			ENDCG
		}

		Pass
		{
			Name "Blur Horizontal"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag			
			#include "UnityCG.cginc"	
			#include "MarggobSSAO.cginc"

			sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 uvBlur : TEXCOORD1;				
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				half2 d1 = float2(1.35, 0) * _MainTex_TexelSize.xy;
				o.uvBlur = float4(o.uv + d1, o.uv - d1);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				float4 ao0 = tex2D(_MainTex, i.uv);
				float4 ao1 = tex2D(_MainTex, i.uvBlur.zw);
				float4 ao2 = tex2D(_MainTex, i.uvBlur.xy);
				
				ao0.xyz = ao0.xyz * 2 - 1;
				ao1.xyz = ao1.xyz * 2 - 1;
				ao2.xyz = ao2.xyz * 2 - 1;
				
				float4 normalDepth_0 = getDepthNormal(i.uv);
				float4 normalDepth_1 = getDepthNormal(i.uvBlur.zw);
				float4 normalDepth_2 = getDepthNormal(i.uvBlur.xy);

				// Reconstruct the view-space position.
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
				float3 pos_o = float3((i.uv * 2 - 1 - p13_31) / p11_22, 1) * normalDepth_0.w;
				// Get view direction
				float3 viewDir = normalize(mul((float3x3)unity_CameraToWorld, pos_o));
				
				// Calculate threshold value for depth compare
				float angle = max(0, dot(-viewDir, normalDepth_0.xyz));
				angle = pow(angle, 2.2);
				float threshold = lerp(0.03, 0.003, angle) * normalDepth_0.w;

				//Blur
				float w0 = 0.24;
				float w1 = compare(normalDepth_0, normalDepth_1, threshold, 0.65) * 0.38;
				float w2 = compare(normalDepth_0, normalDepth_2, threshold, 0.65) * 0.38;				
				float accumWeight = w0 + w1 + w2;

				ao0.a *= w0;
				ao0.a += ao1.a * w1;
				ao0.a += ao2.a * w2;
				
				ao0.xyz *= w0;
				ao0.xyz += ao1.xyz * w1;
				ao0.xyz += ao2.xyz * w2;

				return float4(normalize(ao0.xyz) * 0.5 + 0.5, ao0.a / accumWeight);
			}
			ENDCG
		}
		
		Pass
		{
			Name "Blur Vertical"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "MarggobSSAO.cginc"

			sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 uvBlur : TEXCOORD1;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				half2 d1 = float2(0, 1.35) * _MainTex_TexelSize.xy;
				o.uvBlur = float4(o.uv + d1, o.uv - d1);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				float4 ao0 = tex2D(_MainTex, i.uv);
				float4 ao1 = tex2D(_MainTex, i.uvBlur.zw);
				float4 ao2 = tex2D(_MainTex, i.uvBlur.xy);
				
				ao0.xyz = ao0.xyz * 2 - 1;
				ao1.xyz = ao1.xyz * 2 - 1;
				ao2.xyz = ao2.xyz * 2 - 1;
				
				float4 normalDepth_0 = getDepthNormal(i.uv);
				float4 normalDepth_1 = getDepthNormal(i.uvBlur.zw);
				float4 normalDepth_2 = getDepthNormal(i.uvBlur.xy);

				// Reconstruct the view-space position.
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
				float3 pos_o = float3((i.uv * 2 - 1 - p13_31) / p11_22, 1) * normalDepth_0.w;
				// Get view direction
				float3 viewDir = normalize(mul((float3x3)unity_CameraToWorld, pos_o));
				
				// Calculate threshold value for depth compare
				float angle = max(0, dot(-viewDir, normalDepth_0.xyz));
				angle = pow(angle, 2.2);
				float threshold = lerp(0.03, 0.003, angle) * normalDepth_0.w;

				//Blur
				float w0 = 0.24;
				float w1 = compare(normalDepth_0, normalDepth_1, threshold, 0.65) * 0.38;
				float w2 = compare(normalDepth_0, normalDepth_2, threshold, 0.65) * 0.38;
				float accumWeight = w0 + w1 + w2;

				ao0.a *= w0;
				ao0.a += ao1.a * w1;
				ao0.a += ao2.a * w2;
				
				ao0.xyz *= w0;
				ao0.xyz += ao1.xyz * w1;
				ao0.xyz += ao2.xyz * w2;				

				return float4(normalize(ao0.xyz) * 0.5 + 0.5, ao0.a / accumWeight); 
			}
			ENDCG
		}

		Pass
		{
			Name "Temporal Denoising"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "MargGobSSAO.cginc"
			
			sampler2D _OldFrame;
			sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;	

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 uvOffset : TEXCOORD1;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uvOffset = _MainTex_TexelSize.xyxy * float4(1, 1, -1, 0);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{			
				float2 uv = i.uv;

				float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				float lastDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_LastCameraDepthTexture, uv));
				float2 oldUV = i.uv - CalculateMotionVector(i.uv);
				fixed oldUVclamp = 1 - step(0, oldUV.x) * (1 - step(1, oldUV.x)) * step(0, oldUV.y) * (1 - step(1, oldUV.y));

				float4 oldFrame = tex2D(_OldFrame, oldUV);

				float4 ao1 = tex2D(_MainTex, uv - i.uvOffset.xy);				
				float4 ao2 = tex2D(_MainTex, uv - i.uvOffset.wy);
				float4 ao3 = tex2D(_MainTex, uv - i.uvOffset.zy);
				float4 ao4 = tex2D(_MainTex, uv - i.uvOffset.xw);
				float4 ao5 = tex2D(_MainTex, uv          	   );
				float4 ao6 = tex2D(_MainTex, uv + i.uvOffset.xw);
				float4 ao7 = tex2D(_MainTex, uv + i.uvOffset.xy);
				float4 ao8 = tex2D(_MainTex, uv + i.uvOffset.wy);
				float4 ao9 = tex2D(_MainTex, uv + i.uvOffset.zy);
				ao1.xyz = ao1.xyz * 2 - 1;
				ao2.xyz = ao2.xyz * 2 - 1;
				ao3.xyz = ao3.xyz * 2 - 1;
				ao4.xyz = ao4.xyz * 2 - 1;
				ao5.xyz = ao5.xyz * 2 - 1;
				ao6.xyz = ao6.xyz * 2 - 1;
				ao7.xyz = ao7.xyz * 2 - 1;
				ao8.xyz = ao8.xyz * 2 - 1;
				ao9.xyz = ao9.xyz * 2 - 1;

				float aomin = min(min(min(min(min(min(min(min(ao1.a, ao2.a), ao3.a), ao4.a), ao5.a), ao6.a), ao7.a), ao8.a), ao9.a);
				float aomax = max(max(max(max(max(max(max(max(ao1.a, ao2.a), ao3.a), ao4.a), ao5.a), ao6.a), ao7.a), ao8.a), ao9.a);
				oldFrame.a = clamp(oldFrame.a, aomin, aomax);
				float3 vecSumm = normalize(ao1.xyz + ao2.xyz + ao3.xyz + ao4.xyz + ao5.xyz + ao6.xyz + ao7.xyz + ao8.xyz + ao9.xyz);
				oldFrame.xyz = normalize(oldFrame.xyz * 2 - 1 + vecSumm);

				float depthCheck = (smoothstep(0.0, 0.1, abs(lastDepth - depth)) + 0.15) / 1.15 ;

				float denoise = lerp(oldFrame.a, ao5.a, depthCheck);
				float3 bentVec = lerp(oldFrame.xyz, ao5.xyz, depthCheck) * 0.5 + 0.5;

				return float4(bentVec, denoise);
				//return ao5;
			}
			ENDCG
		}

		Pass
		{
			Name "Apply SSAO to Lighting"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;
			sampler2D _CameraGBufferTexture3;
			#pragma multi_compile _DEBUG_None _DEBUG_AO

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				float4 c = tex2D(_CameraGBufferTexture3, i.uv);		
				float ao = tex2D(_MainTex, i.uv).a;
				c.rgb *= ao;				
				return c;
			}
			ENDCG
		}

		Pass
		{
			Name "Calculate Reflection Occlusion"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;
			sampler2D _CameraGBufferTexture1, _CameraGBufferTexture2;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				half3 gbuffer2 = tex2D (_CameraGBufferTexture2, i.uv).xyz * 2 - 1;	
				half4 bentAO   = tex2D(_MainTex, i.uv);
				bentAO.xyz 	   = bentAO.xyz * 2 - 1;

				// Reconstruct the view-space position.
				float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
				float3 pos_o = float3((i.uv * 2 - 1 - p13_31) / p11_22, 1) * depth;
				// View direction
				float3 eyeVec = mul(unity_CameraToWorld, normalize(pos_o));
				// Reflect vector from normal
				float3 reflectVec = reflect(eyeVec, gbuffer2);
				// Reflect vector from bent normal
				float3 reflectBentVec = reflect(eyeVec, bentAO.xyz);
				// Reflection ocllusion
				return max(0, dot(reflectVec, reflectBentVec)) * bentAO.a;
			}
			ENDCG
		}

		Pass
		{
			Name "Apply SSAO to Reflection"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex, _ReflectionOcclusion;
			#pragma multi_compile _DEBUG_None _DEBUG_AO

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				float ao = tex2D(_ReflectionOcclusion, i.uv).x;
				float4 c = tex2D(_MainTex, i.uv);				
				return c * ao;
			}
			ENDCG
		}

		Pass
		{
			Name "Debug mode"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _OldFrame;

			struct appdata	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : POSITION;
			};

			struct v2f	
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)	
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			} 

			fixed4 frag (v2f i) : SV_Target
			{
				float4 c = tex2D(_OldFrame, i.uv);				
				return c.a;
			}
			ENDCG
		}
	}
}
