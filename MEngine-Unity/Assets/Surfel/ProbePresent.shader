Shader "Unlit/ProbePresent"
{
Properties
{
    _SampleCount("SampleCount", Float) = 0
}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "SH.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
            };
            float _SampleCount;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                return o;
            }
            StructuredBuffer<SH9Color> _SHBuffer;

            float4 frag (v2f i) : SV_Target
            {
                SH9Color color = _SHBuffer[_SampleCount];
                SH9 sh = SHCosineLobe(normalize(i.normal));
                float3 result = 0;
                for(uint a = 0; a < 9; ++a)
                {
                    result += color.c[a] * sh.c[a];
                }
                return float4(result, 1);
            }
            ENDCG
        }
    }
}
