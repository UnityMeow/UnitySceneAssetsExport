Shader "Unlit/SignedDepth"
{
	SubShader
	{

		Tags { "RenderType" = "Opaque" }
		LOD 100
		Pass
		{
			Cull off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;

			};

			struct v2f
			{
				float4 vertex : SV_POSITION;

			};

			float4x4 _VP;
			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(_VP, worldPos);

				return o;
			}

			void frag(v2f i, out float albedoOut : SV_Target0)
			{
				albedoOut = 0;
			}
			ENDCG
		}
		Pass
		{
			Cull off
			CGPROGRAM
			#include "SH.cginc"
			#pragma vertex vert
			#pragma fragment frag

			#pragma target 5.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 worldPos : TEXCOORD0;
			};

			float4x4 _VP;
			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(_VP, worldPos);
				o.worldPos = worldPos.xyz;
				o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
				return o;
			}

			#define PI 3.1415926535
			float3 _CubeStartPos;
			float3 _CubeSize;
			Texture3D<float4> _SkyOcclusion0; SamplerState sampler_SkyOcclusion0;
			Texture3D<float4> _SkyOcclusion1; SamplerState sampler_SkyOcclusion1;
			Texture3D<float> _SkyOcclusion2; SamplerState sampler_SkyOcclusion2;
			void frag(v2f i, out float3 albedoOut : SV_Target0, bool isFrontFace : SV_ISFRONTFACE)
			{
				if(!isFrontFace) i.normal *= -1;
				i.normal = normalize(i.normal);
				SH9 shColor;
				float3 probeUV = (i.worldPos - _CubeStartPos) / _CubeSize;
				probeUV = saturate(probeUV);
				float4 sampleColor = _SkyOcclusion0.SampleLevel(sampler_SkyOcclusion0, probeUV, 0);
				shColor.c[0] = sampleColor.x;
				shColor.c[1] = sampleColor.y;
				shColor.c[2] = sampleColor.z;
				shColor.c[3] = sampleColor.w;
				sampleColor = _SkyOcclusion1.SampleLevel(sampler_SkyOcclusion1, probeUV, 0);
				shColor.c[4] = sampleColor.x;
				shColor.c[5] = sampleColor.y;
				shColor.c[6] = sampleColor.z;
				shColor.c[7] = sampleColor.w;
				shColor.c[8] = _SkyOcclusion2.SampleLevel(sampler_SkyOcclusion2, probeUV, 0);
				shColor = SHMul(shColor, SHCosineLobe(i.normal));
				float value = 0;
				for (uint i = 0; i < 9; ++i)
				{
					value += shColor.c[i];
				}
				albedoOut = max(0.01, value * 4 * PI);
			}
			ENDCG
		}
		//Pass 2: Albedo
		pass
		{
			CGPROGRAM
			#include "SH.cginc"
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma target 5.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;

				float3 worldPos : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};
			Texture2D<float4> _MainTex; SamplerState sampler_MainTex;
			float4x4 _VP;
			float4 _CamPos;
			float4 _TileOffset;
			float4 _Color;

			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(_VP, worldPos);
				o.worldPos = worldPos.xyz;
				o.uv = v.uv * _TileOffset.xy + _TileOffset.zw;
				return o;
			}
			float4 frag(v2f i) : SV_TARGET0
			{
				return float4(
				lerp(1, _MainTex.SampleLevel(sampler_MainTex, i.uv, 0).xyz, _Color.w) * _Color.xyz,
				distance(_CamPos, i.worldPos));
			}
			ENDCG
		}
		//Pass 3: Normal
		pass
		{
			CGPROGRAM
			#include "SH.cginc"
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma target 5.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};
			float4x4 _VP;

			v2f vert(appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(_VP, worldPos);
				o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
				return o;
			}
			float3 frag(v2f i) : SV_TARGET0
			{
				return normalize(i.normal) * 0.5 + 0.5;
			}
			ENDCG
		}
	}
}
