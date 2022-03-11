Shader "Unlit/SDFTest"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZTest Always ZWrite off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "SDF.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            StructuredBuffer<SDFPrimitive> _PrimitiveBuffer;
            StructuredBuffer<float4> _SphereBuffer;
            uint _PrimitiveCount;
            uint _SphereCount;
            uint _IterateCount;
            float4x4 _InvVP;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float4 worldPos = mul(_InvVP, o.vertex);
                o.worldPos = worldPos.xyz / worldPos.w;
                return o;
            }

            float3 frag (v2f i) : SV_Target
            {
                float dist = 1;
                float3 startPoint = _WorldSpaceCameraPos;
                float3 view = normalize(i.worldPos - startPoint);
                for(uint ite = 0; ite < _IterateCount; ++ite)
                {
                    float currDist = GetDistanceFromArea(
                        _PrimitiveBuffer, _PrimitiveCount,
                        _SphereBuffer, _SphereCount,
                        startPoint
                    );
                    if(currDist <= 0)
                    {
                        break;
                    }
                    dist = currDist;
                    startPoint += view * dist;
                }
                return 0;
            }
            ENDCG
        }
    }
}
