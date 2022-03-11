Shader "Unreal/Rock"
{
	Properties 
	{
		_DetailNormalUVScale("DetailNormalUVScale", Range(0, 5)) = 3
		_DetailNormalIntesity("DetailNormalIntesity", Range(0, 5)) = 1
		_DetailNormalMaskIntesity("DetailNormalMaskIntesity", Range(0, 5)) = 1
		
		_LUTSelect("LUTSelect", Range(0, 7.9)) = 3.385715
		_LUTColorPower("LUTColorPower", float) = 1
		_AOMaskIntesity("AOMaskIntesity", Range(0, 5)) = 1
		_AOLerp("AOLerp", Range(0, 5)) = 0.76
		_NormalMaskIntesity("_NormalMaskIntesity", Range(0, 5)) = 1
		_DirtLerp("DirtLerp", Range(0, 1)) = 0.714286
		_DirMaskPowerIntesity("DirMaskPowerIntesity", Range(0, 5)) = 0.76
		_RoughnessMaskPowerIntesity("RoughnessMaskPowerIntesity", Range(0, 1)) = 1
		_RoughnessMin("RoughnessMin", Range(0, 1)) = 0.752381
		_RoughnessMax("RoughnessMax", Range(0, 2)) = 1
		
		// 贴图
		[NoScaleOffset]_Normal ("NormalTex", 2D) = "bump" {}
		[NoScaleOffset]_DetailNormal ("DetailNormalTex", 2D) = "bump" {}
		[NoScaleOffset]_LUTColor ("LUTColor", 2D) = "white" {}
		[NoScaleOffset]_Noise ("Noise", 2D) = "white" {}
		[NoScaleOffset]_UV_OC ("UV_OC", 2D) = "white" {}
		[NoScaleOffset]_Tilling_CV ("Tilling_CV", 2D) = "white" {}
		
		_DirColorTint ("DirColorTint", Color) = (0,0,0,1)
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma target 5.0
        struct Input
        {
        
            float2 uv_DetailNormal;
			float2 uv_DetailAlbedo;
			float3 worldPos;
        };
		
		// --------------------
		float _DirtLerp;
		float _DetailNormalUVScale;
		float _DetailNormalIntesity;
		float _DetailNormalMaskIntesity;
		float _NormalMaskIntesity;
		float _LUTSelect;
		float _LUTColorPower;
		float _AOMaskIntesity;
		float _AOLerp;
		float _DirMaskPowerIntesity;
		float _RoughnessMaskPowerIntesity;
		float _RoughnessMin;
		float _RoughnessMax;
		float4 _DirColorTint;
		
		// --------------------
		sampler2D _LUTColor;
		sampler2D _Normal;
		sampler2D _DetailNormal;
		sampler2D _Noise;
		sampler2D _UV_OC;
		sampler2D _Tilling_CV;
		
		// --------------------
		float3 GetNormal(float4 normal);
		float GetClampNormalAlpha(float4 normal);
		
		
        void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
        {
			float2 uv = IN.uv_DetailNormal;
			
			// normal & detail normal
			float4 normal = tex2D(_Normal,uv);
			float2 detailUV = uv * _DetailNormalUVScale;
			float4 detailNormal = tex2D(_DetailNormal,detailUV);
			float3 detailNormalIntesity = float3(_DetailNormalIntesity,_DetailNormalIntesity,0);
			
			// oc cv
			float4 oc = tex2D(_UV_OC,uv);
			float4 cv = tex2D(_Tilling_CV,uv * _DetailNormalUVScale);
		
			o.Normal = GetNormal(detailNormal) * detailNormalIntesity + GetNormal(normal);
			
			// rocky lut
			float dna = pow(cv.x,_DetailNormalMaskIntesity);
			float na = oc.y;
			
			float lutV = 1 - clamp(max(na,dna), 0, 1);
			float lutU =  clamp((floor(_LUTSelect + 0.01) + 0.5) / 8, 0, 1);
			float4 rockySampleLUT = tex2D(_LUTColor,float2(lutU,lutV)) * _LUTColorPower;
			
			// noise
			float noise = tex2D(_Noise,uv * 6).z * 0.05;
			noise = clamp((noise + pow(oc.x,_AOMaskIntesity)) - (0.5 * 0.05), 0, 1);
			
			// dirt mask --
			float dm = clamp(pow(min(na,dna),_DirMaskPowerIntesity), 0, 1);
			dm = 1 - lerp(dm, dm * noise, _AOLerp);
			
			// basecolor
			float4 bcTmp = lerp(rockySampleLUT, rockySampleLUT * noise, _AOLerp);
			o.Albedo = lerp(bcTmp, lerp(bcTmp,_DirColorTint,dm), _DirtLerp);
			
			// roughness mask --
			float rm = clamp(pow(min(na,dna),_RoughnessMaskPowerIntesity), 0, 1);
			rm = 1 - lerp(rm, rm * noise, _AOLerp);
			
			// roughness
			float low = 0;
			float hight = 1;
			float rhTmp = (rm - low) / (hight - low);
			float end = rhTmp * (_RoughnessMax - _RoughnessMin) + _RoughnessMin;
			o.Smoothness  = 1 - clamp(end, 0, 1);
		}
		
		float3 GetNormal(float4 normal)
		{
		    float2 tmp = normal.xy * 2 - 1;
		    tmp *= 0.5;
		    float normalZ = sqrt(1 - (tmp.x * tmp.x + tmp.y * tmp.y));
		    return float3(tmp.x,tmp.y,normalZ);
		}
		
		float GetClampNormalAlpha(float4 normal)
		{
		    float tmp = pow(normal.w,_NormalMaskIntesity);
			return clamp(tmp,0,1);
		}
		
        ENDCG
    }
    FallBack "Diffuse"
}