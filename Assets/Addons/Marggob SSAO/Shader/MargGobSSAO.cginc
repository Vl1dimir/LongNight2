#ifndef MARGGOBSSAO_INCLUDED
#define MARGGOBSSAO_INCLUDED

sampler2D_float _CameraDepthTexture, _LastCameraDepthTexture;
sampler2D _normalDepth;
float4x4 _Reprojection;

///////////////// Blur functions /////////////////
inline float compare(float4 n1, float4 n2, half threshold, float blurSharpness)
{
    float c = smoothstep(blurSharpness, 1, dot(n1.xyz, n2.xyz));
    c *= saturate(1.0 - abs(n1.w - n2.w) / threshold);				
    return c;
}

inline float4 getDepthNormal(half2 uv)
{
    float4 nd = tex2D(_normalDepth, uv); 
    nd.xyz = nd.xyz * 2 - 1;
    nd.a = LinearEyeDepth(nd.a);
    return nd;
}

///////////////// Temporal Denoise functions /////////////////

float SampleRawDepth(float2 uv)
{
    float z = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0));
#if defined(UNITY_REVERSED_Z)
    z = 1 - z;
#endif
    return z;
}

// Inverse project UV + raw depth into the view space.
float3 InverseProjectUVZ(float2 uv, float z)
{
    float4 cp = float4(float3(uv, z) * 2 - 1, 1);
    float4 vp = mul(unity_CameraInvProjection, cp);
    return float3(vp.xy, -vp.z) / vp.w;
}

// Inverse project UV into the view space with sampling the depth buffer.
float3 InverseProjectUV(float2 uv)
{
    return InverseProjectUVZ(uv, SampleRawDepth(uv));
}

float2 CalculateMotionVector(float2 uv)
{
    float4 cp = mul(_Reprojection, float4(InverseProjectUV(uv), 1));
    float2 prev = (cp.xy / cp.w + 1) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    prev.y = 1 - prev.y;
#endif
    return uv - prev;
}

#endif