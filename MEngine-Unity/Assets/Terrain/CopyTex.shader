Shader "Hidden/CopyTex"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {


        // No culling or depth
        Cull Off ZWrite Off ZTest Always
CGINCLUDE
#pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"
            float4 _TileOffset;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
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

            Texture2D _MainTex;
            SamplerState Global_point_clamp_sampler, Global_bilinear_clamp_sampler, Global_trilinear_clamp_sampler, Global_point_repeat_sampler, Global_bilinear_repeat_sampler, Global_trilinear_repeat_sampler;


ENDCG
        Pass
        {
            CGPROGRAM
            
            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _TileOffset.xy + _TileOffset.zw;
                uv.y = 1 - uv.y;
                float4 col = _MainTex.SampleLevel(Global_bilinear_clamp_sampler, uv, 0);
                return col.x;

            }
            ENDCG
        }
        //Pass 1: Get Circle Mask
        Pass
        {
            CGPROGRAM
            float frag (v2f i) : SV_TARGET
            {
                float len = length(i.uv - 0.5) * 2;
                return 1 - saturate(pow(len, _TileOffset.w));
            }
            ENDCG
        }
        //Pass 2: Get Rectangle Mask
        Pass
        {
            CGPROGRAM
           // float4 _TileOffset;    xy: inside z: side  w: power
            float frag(v2f i) : SV_TARGET
            {
                float2 uv = abs(i.uv * 2 - 1);
                uv += _TileOffset.xy;
                uv = max(0, uv);
                float col = pow(lerp(uv.x, uv.y,0.5), _TileOffset.w);
                return saturate(1 - col);
            }
            ENDCG
        }
         //Pass 3: Get Circle Mask With Custom Mask
        Pass
        {
            CGPROGRAM
            
            Texture2D<float> _CustomMask;
            float frag (v2f i) : SV_TARGET
            {

                float len = length(i.uv - 0.5) * 2;
                return 1 - saturate(pow(len, _TileOffset.w))* _CustomMask.SampleLevel(Global_bilinear_clamp_sampler, i.uv, 0);
            }
            ENDCG
        }
        //Pass 4: Get Rectangle Mask With Custom Mask
        Pass
        {
            CGPROGRAM
            Texture2D<float> _CustomMask;
            float frag(v2f i) : SV_TARGET
            {
                float2 uv = abs(i.uv * 2 - 1);
                uv -= _TileOffset.xy;
                float col = pow(lerp(uv.x, uv.y, saturate((uv.x - uv.y) * _TileOffset.z * 0.5 + 0.5)), _TileOffset.w);
                return saturate(1 - col) * _CustomMask.SampleLevel(Global_bilinear_clamp_sampler, i.uv, 0);
            }
            ENDCG
        }
        //Pass 5: Output Normal
        pass
        {
            CGPROGRAM
            float2 frag(v2f i) : SV_TARGET
            {
                return UnpackNormal(_MainTex.SampleLevel(Global_bilinear_clamp_sampler, i.uv, 0)).xy;
            }
            ENDCG
        }
    }
}
