﻿// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

Shader "GITest"
{
  Properties {
    _Color ("Color", Color) = (1,1,1,1)
    _ClearCoat("Clearcoat", Range(0, 1)) = 0.5
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _ClearCoatSmoothness("Secondary Smoothness", Range(0, 1)) = 0.5
    _Occlusion("Occlusion Scale", Range(0,1)) = 1
    _Cutoff("Cut off", Range(0, 1)) = 0
    _NormalIntensity("Normal Intensity", Vector) = (1,1,0,0)
    _SpecularIntensity("Specular Intensity", Range(0,1)) = 0.04
    _MetallicIntensity("Metallic Intensity", Range(0, 1)) = 0.1
    _MinDist("Min Tessellation Dist", float) = 20
    _MaxDist("Max Tessellation Dist", float) = 50
    _Tessellation("Tessellation Intensity", Range(1, 63)) = 1
    _HeightmapIntensity("Heightmap Intensity", Range(0, 10)) = 0
    _TileOffset("Texture ScaleOffset", Vector) = (1,1,0,0)
    [NoScaleOffset]_MainTex ("Albedo (RGB)Mask(A)", 2D) = "white" {}
    [NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
    [NoScaleOffset]_SpecularMap("R(Smooth)G(Spec)B(Occ)", 2D) = "white"{}
    [NoScaleOffset]_HeightMap("Height Map", 2D) = "black"{}
    _SecondaryTileOffset("Secondary ScaleOffset", Vector) = (1,1,0,0)
    [NoScaleOffset]_SecondaryMainTex("Secondary Albedo(RGB)Mask(A)", 2D) = "white"{}
    [NoScaleOffset]_SecondaryBumpMap("Secondary Normal", 2D) = "bump"{}
    [NoScaleOffset]_SecondarySpecularMap("Secondary Specuar", 2D) = "white"{}
    _EmissionMultiplier("Emission Multiplier", Range(0, 128)) = 1
    _EmissionColor("Emission Color", Color) = (0,0,0,1)
    [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white"{}
    [HideInInspector]_LightingModel("lm", Int) = 1
    [HideInInspector]_DecalLayer("dl", Int) = 0

    [HideInInspector]_UseTessellation("tess", Int) = 0
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 200

    
    // ------------------------------------------------------------
    // Surface shader code generated out of a CGPROGRAM block:
    

    // ---- forward rendering base pass:
    Pass {
      Name "FORWARD"
      Tags { "LightMode" = "ForwardBase" }

      CGPROGRAM
      // compile directives
      #pragma vertex vert_surf
      #pragma fragment frag_surf
      #pragma target 5.0
      #pragma multi_compile_instancing
      #pragma multi_compile_fog
      #pragma multi_compile_fwdbase
      #include "HLSLSupport.cginc"
      #include "Montcalo_Library.hlsl"
      #include "ProbeGI.cginc"
      #include "SH.cginc"
      #include "SDF.cginc"
      #define UNITY_INSTANCED_LOD_FADE
      #define UNITY_INSTANCED_SH
      #define UNITY_INSTANCED_LIGHTMAPSTS
      #include "UnityShaderVariables.cginc"
      #include "UnityShaderUtilities.cginc"
      // -------- variant for: <when no other keywords are defined>
      #if !defined(INSTANCING_ON)
        // Surface shader code generated based on:
        // writes to per-pixel normal: YES
        // writes to emission: YES
        // writes to occlusion: YES
        // needs world space reflection vector: no
        // needs world space normal vector: no
        // needs screen space position: no
        // needs world space position: no
        // needs view direction: no
        // needs world space view direction: no
        // needs world space position for lighting: YES
        // needs world space view direction for lighting: YES
        // needs world space view direction for lightmaps: no
        // needs vertex color: no
        // needs VFACE: no
        // passes tangent-to-world matrix to pixel shader: YES
        // reads from normal: no
        // 1 texcoords actually used
        //   float2 _MainTex
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityPBSLighting.cginc"
        #include "AutoLight.cginc"

        #define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
        #define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
        #define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))

        // Original surface shader snippet:

        #ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
        #endif
        /* UNITY: Original start of shader */
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf StandardSpecular fullforwardshadows
        //#pragma target 5.0
        struct Input
        {
          float2 uv_MainTex;
          float2 uv_DetailAlbedo;
          float3 worldPos;
        };
        float _SpecularIntensity;
        float _MetallicIntensity;
        float4 _EmissionColor;
        float _Occlusion;
        float _VertexScale;
        float _VertexOffset;
        float _Cutoff;
        float _EmissionMultiplier;
        sampler2D _BumpMap;
        sampler2D _SpecularMap;
        sampler2D _MainTex;
        sampler2D _DetailAlbedo; 
        sampler2D _DetailNormal;
        sampler2D _EmissionMap;
        float4 _TileOffset;
        float _Glossiness;
        float2 _NormalIntensity;
        float4 _Color;
        

        void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
          float2 uv = IN.uv_MainTex;
          uv = uv * _TileOffset.xy + _TileOffset.zw;
          float2 detailUV = IN.uv_DetailAlbedo;
          #ifdef USE_WHITE
            o.Normal = float3(0,0,1);
            o.Albedo = 1;
            o.Smoothness = 0;
            o.Specular = 0;
            o.Emission = 0;
            float4 spec = tex2D(_SpecularMap,uv);
            o.Occlusion = lerp(1, spec.b, _Occlusion);
          #else
            float4 spec = tex2D(_SpecularMap,uv);
            float4 c = tex2D (_MainTex, uv);
            #if CUT_OFF
              clip(c.a * _Color.a - _Cutoff);
            #endif

            o.Normal = UnpackNormal(tex2D(_BumpMap,uv));
            o.Normal.xy *= _NormalIntensity.xy;
            o.Albedo = c.rgb;

            o.Albedo *= _Color.rgb;

            o.Alpha = 1;
            o.Occlusion = lerp(1, spec.b, _Occlusion);
            o.Specular = lerp(_SpecularIntensity, o.Albedo, _MetallicIntensity * spec.g); 
            o.Smoothness = _Glossiness * spec.r;
            o.Emission = _EmissionColor * tex2D(_EmissionMap, uv) * _EmissionMultiplier;
          #endif
        }
        

        // vertex-to-fragment interpolation data
        // no lightmaps:
        #ifndef LIGHTMAP_ON
          // half-precision fragment shader registers:
          #ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            #define FOG_COMBINED_WITH_TSPACE
            struct v2f_surf {
              UNITY_POSITION(pos);
              float2 pack0 : TEXCOORD0; // _MainTex
              float4 tSpace0 : TEXCOORD1;
              float4 tSpace1 : TEXCOORD2;
              float4 tSpace2 : TEXCOORD3;
              #if UNITY_SHOULD_SAMPLE_SH
                half3 sh : TEXCOORD4; // SH
              #endif
              UNITY_LIGHTING_COORDS(5,6)
              #if SHADER_TARGET >= 30
                float4 lmap : TEXCOORD7;
              #endif
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
            };
          #endif
          // high-precision fragment shader registers:
          #ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            struct v2f_surf {
              UNITY_POSITION(pos);
              float2 pack0 : TEXCOORD0; // _MainTex
              float4 tSpace0 : TEXCOORD1;
              float4 tSpace1 : TEXCOORD2;
              float4 tSpace2 : TEXCOORD3;
              #if UNITY_SHOULD_SAMPLE_SH
                half3 sh : TEXCOORD4; // SH
              #endif
              UNITY_FOG_COORDS(5)
              UNITY_SHADOW_COORDS(6)
              #if SHADER_TARGET >= 30
                float4 lmap : TEXCOORD7;
              #endif
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
            };
          #endif
        #endif
        // with lightmaps:
        #ifdef LIGHTMAP_ON
          // half-precision fragment shader registers:
          #ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            #define FOG_COMBINED_WITH_TSPACE
            struct v2f_surf {
              UNITY_POSITION(pos);
              float2 pack0 : TEXCOORD0; // _MainTex
              float4 tSpace0 : TEXCOORD1;
              float4 tSpace1 : TEXCOORD2;
              float4 tSpace2 : TEXCOORD3;
              float4 lmap : TEXCOORD4;
              UNITY_LIGHTING_COORDS(5,6)
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
            };
          #endif
          // high-precision fragment shader registers:
          #ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
            struct v2f_surf {
              UNITY_POSITION(pos);
              float2 pack0 : TEXCOORD0; // _MainTex
              float4 tSpace0 : TEXCOORD1;
              float4 tSpace1 : TEXCOORD2;
              float4 tSpace2 : TEXCOORD3;
              float4 lmap : TEXCOORD4;
              UNITY_FOG_COORDS(5)
              UNITY_SHADOW_COORDS(6)
              UNITY_VERTEX_INPUT_INSTANCE_ID
              UNITY_VERTEX_OUTPUT_STEREO
            };
          #endif
        #endif
        float4 _MainTex_ST;

        // vertex shader
        v2f_surf vert_surf (appdata_full v) {
          UNITY_SETUP_INSTANCE_ID(v);
          v2f_surf o;
          UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
          UNITY_TRANSFER_INSTANCE_ID(v,o);
          UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
          o.pos = UnityObjectToClipPos(v.vertex);
          o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
          float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
          float3 worldNormal = UnityObjectToWorldNormal(v.normal);
          fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
          fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
          fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
          o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
          o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
          o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
          #ifdef DYNAMICLIGHTMAP_ON
            o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
          #endif
          #ifdef LIGHTMAP_ON
            o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
          #endif

          // SH/ambient and vertex lights
          #ifndef LIGHTMAP_ON
            #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
              o.sh = 0;
              // Approximated illumination from non-important point lights
              #ifdef VERTEXLIGHT_ON
                o.sh += Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, worldPos, worldNormal);
              #endif
              o.sh = ShadeSHPerVertex (worldNormal, o.sh);
            #endif
          #endif // !LIGHTMAP_ON

          UNITY_TRANSFER_LIGHTING(o,v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,o.pos); // pass fog coordinates to pixel shader
          #elif defined (FOG_COMBINED_WITH_WORLD_POS)
            UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,o.pos); // pass fog coordinates to pixel shader
          #else
            UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
          #endif
          return o;
        }
        #define PI 3.1415926535
        float3 _CubeStartPos;
        float3 _CubeSize;
        float3 _VoxelResolution;
        Texture3D _SRV_SurfelSHTex0; SamplerState sampler_SRV_SurfelSHTex0;
        Texture3D _SRV_SurfelSHTex1;
        Texture3D _SRV_SurfelSHTex2;
        Texture3D _SRV_SurfelSHTex3;
        Texture3D _SRV_SurfelSHTex4;
        Texture3D _SRV_SurfelSHTex5;
        Texture3D _SRV_SurfelSHTex6;
        StructuredBuffer<SDFPrimitive> _SRV_PrimitiveBuffer;
        Texture3D<uint4> _SRV_SDFPrimitiveIndices;
        float3 _SDFPrimitiveResolution;
        float3 _SDFUVOffsetIntensity;
        float3 _OriginPos;
        // fragment shader
        fixed4 frag_surf (v2f_surf IN) : SV_Target {
          UNITY_SETUP_INSTANCE_ID(IN);
          // prepare and unpack data
          Input surfIN;
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
          #elif defined (FOG_COMBINED_WITH_WORLD_POS)
            UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
          #else
            UNITY_EXTRACT_FOG(IN);
          #endif
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_RECONSTRUCT_TBN(IN);
          #else
            UNITY_EXTRACT_TBN(IN);
          #endif
          UNITY_INITIALIZE_OUTPUT(Input,surfIN);
          surfIN.uv_MainTex.x = 1.0;
          surfIN.uv_DetailAlbedo.x = 1.0;
          surfIN.worldPos.x = 1.0;
          surfIN.uv_MainTex = IN.pack0.xy;
          float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
          #ifndef USING_DIRECTIONAL_LIGHT
            fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
          #else
            fixed3 lightDir = _WorldSpaceLightPos0.xyz;
          #endif
          float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
          #ifdef UNITY_COMPILER_HLSL
            SurfaceOutputStandardSpecular o = (SurfaceOutputStandardSpecular)0;
          #else
            SurfaceOutputStandardSpecular o;
          #endif
          o.Albedo = 0.0;
          o.Emission = 0.0;
          o.Specular = 0.0;
          o.Alpha = 0.0;
          o.Occlusion = 1;
          

          fixed3 normalWorldVertex = fixed3(0,0,1);
          o.Normal = fixed3(0,0,1);

          // call surface function
          surf (surfIN, o);
          // compute lighting & shadowing factor
          UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
          fixed4 c = 0;
          float3 worldN;
          worldN.x = dot(_unity_tbn_0, o.Normal);
          worldN.y = dot(_unity_tbn_1, o.Normal);
          worldN.z = dot(_unity_tbn_2, o.Normal);
          worldN = normalize(worldN);
          o.Normal = worldN;
          o.Occlusion = 0;
          
          
          UnityGI gi;
          UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
          gi.indirect.diffuse = 0;
          gi.indirect.specular = 0;
          gi.light.color = _LightColor0.rgb;
          gi.light.dir = lightDir;
          // Call GI (lightmaps/SH/reflections) lighting function
          UnityGIInput giInput;
          UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
          giInput.light = gi.light;
          giInput.worldPos = worldPos;
          giInput.worldViewDir = worldViewDir;
          giInput.atten = atten;
          #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
            giInput.lightmapUV = IN.lmap;
          #else
            giInput.lightmapUV = 0.0;
          #endif
          #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
            giInput.ambient = IN.sh;
          #else
            giInput.ambient.rgb = 0.0;
          #endif
          giInput.probeHDR[0] = unity_SpecCube0_HDR;
          giInput.probeHDR[1] = unity_SpecCube1_HDR;
          #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
            giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
          #endif
          #ifdef UNITY_SPECCUBE_BOX_PROJECTION
            giInput.boxMax[0] = unity_SpecCube0_BoxMax;
            giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
            giInput.boxMax[1] = unity_SpecCube1_BoxMax;
            giInput.boxMin[1] = unity_SpecCube1_BoxMin;
            giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
          #endif
          LightingStandardSpecular_GI(o, giInput, gi);

          // realtime lighting: call lighting function
          c += LightingStandardSpecular (o, worldViewDir, gi);
          c.rgb += o.Emission;
          SH9 normalSH = SHCosineLobe(o.Normal);
          {
            worldPos -= _OriginPos;
            float3 probeUV = (worldPos - _CubeStartPos) / _CubeSize;
            probeUV = saturate(probeUV);
            uint3 sdfCoord = probeUV * floor(_SDFPrimitiveResolution);
            float3 offsetForce = GetOffsetForce(
            _SRV_SDFPrimitiveIndices[sdfCoord],
            _SRV_PrimitiveBuffer,
            worldPos,
            _SDFUVOffsetIntensity
            );
            Texture3D shTexs[7] = 
            {
              _SRV_SurfelSHTex0,
              _SRV_SurfelSHTex1,
              _SRV_SurfelSHTex2,
              _SRV_SurfelSHTex3,
              _SRV_SurfelSHTex4,
              _SRV_SurfelSHTex5,
              _SRV_SurfelSHTex6
            };
            float3 uvOffset = offsetForce / _CubeSize;
            float3 probeUVBase = (floor(probeUV * floor(_VoxelResolution)) + 0.5);
            float3 targetProbeUv = (probeUV + uvOffset) * floor(_VoxelResolution);
            targetProbeUv = clamp(targetProbeUv,probeUVBase - 0.9999, probeUVBase + 0.9999);
            targetProbeUv /= floor(_VoxelResolution);
            //probeUV += uvOffset;
            SH9Color shColor = GetSHColorFromTexture(shTexs, sampler_SRV_SurfelSHTex0, targetProbeUv);
            float3 finalGIColor = 0;
            for(uint a = 0; a < 9; ++a)
            {
              finalGIColor += normalSH.c[a] * shColor.c[a];
            }
            finalGIColor *= o.Albedo;
            c.rgb += finalGIColor;
          }
          UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
          UNITY_OPAQUE_ALPHA(c.a);
          return c;
        }


      #endif


      ENDCG

    }


    // ---- meta information extraction pass:
    Pass {
      Name "Meta"
      Tags { "LightMode" = "Meta" }
      Cull Off

      CGPROGRAM
      // compile directives
      #pragma vertex vert_surf
      #pragma fragment frag_surf
      #pragma target 5.0
      #pragma multi_compile_instancing
      #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
      #pragma shader_feature EDITOR_VISUALIZATION

      #include "HLSLSupport.cginc"
      #define UNITY_INSTANCED_LOD_FADE
      #define UNITY_INSTANCED_SH
      #define UNITY_INSTANCED_LIGHTMAPSTS
      #include "UnityShaderVariables.cginc"
      #include "UnityShaderUtilities.cginc"
      // -------- variant for: <when no other keywords are defined>
      #if !defined(INSTANCING_ON)
        // Surface shader code generated based on:
        // writes to per-pixel normal: YES
        // writes to emission: YES
        // writes to occlusion: YES
        // needs world space reflection vector: no
        // needs world space normal vector: no
        // needs screen space position: no
        // needs world space position: no
        // needs view direction: no
        // needs world space view direction: no
        // needs world space position for lighting: YES
        // needs world space view direction for lighting: YES
        // needs world space view direction for lightmaps: no
        // needs vertex color: no
        // needs VFACE: no
        // passes tangent-to-world matrix to pixel shader: YES
        // reads from normal: no
        // 1 texcoords actually used
        //   float2 _MainTex
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityPBSLighting.cginc"

        #define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
        #define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
        #define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))

        // Original surface shader snippet:

        #ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
        #endif
        /* UNITY: Original start of shader */
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf StandardSpecular fullforwardshadows
        //#pragma target 5.0
        struct Input
        {
          float2 uv_MainTex;
          float2 uv_DetailAlbedo;
          float3 worldPos;
        };
        float _SpecularIntensity;
        float _MetallicIntensity;
        float4 _EmissionColor;
        float _Occlusion;
        float _VertexScale;
        float _VertexOffset;
        float _Cutoff;
        float _EmissionMultiplier;
        sampler2D _BumpMap;
        sampler2D _SpecularMap;
        sampler2D _MainTex;
        sampler2D _DetailAlbedo; 
        sampler2D _DetailNormal;
        sampler2D _EmissionMap;
        float4 _TileOffset;
        float _Glossiness;
        float2 _NormalIntensity;
        float4 _Color;
        
        void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
          float2 uv = IN.uv_MainTex;
          uv = uv * _TileOffset.xy + _TileOffset.zw;
          float2 detailUV = IN.uv_DetailAlbedo;
          #ifdef USE_WHITE
            o.Normal = float3(0,0,1);
            o.Albedo = 1;
            o.Smoothness = 0;
            o.Specular = 0;
            o.Emission = 0;
            float4 spec = tex2D(_SpecularMap,uv);
            o.Occlusion = lerp(1, spec.b, _Occlusion);
          #else
            float4 spec = tex2D(_SpecularMap,uv);
            float4 c = tex2D (_MainTex, uv);
            #if CUT_OFF
              clip(c.a * _Color.a - _Cutoff);
            #endif

            o.Normal = UnpackNormal(tex2D(_BumpMap,uv));
            o.Normal.xy *= _NormalIntensity.xy;
            o.Albedo = c.rgb;

            o.Albedo *= _Color.rgb;

            o.Alpha = 1;
            o.Occlusion = lerp(1, spec.b, _Occlusion);
            o.Specular = lerp(_SpecularIntensity, o.Albedo, _MetallicIntensity * spec.g); 
            o.Smoothness = _Glossiness * spec.r;
            o.Emission = _EmissionColor * tex2D(_EmissionMap, uv) * _EmissionMultiplier;
          #endif
        }
        
        #include "UnityMetaPass.cginc"

        // vertex-to-fragment interpolation data
        struct v2f_surf {
          UNITY_POSITION(pos);
          float2 pack0 : TEXCOORD0; // _MainTex
          float4 tSpace0 : TEXCOORD1;
          float4 tSpace1 : TEXCOORD2;
          float4 tSpace2 : TEXCOORD3;
          #ifdef EDITOR_VISUALIZATION
            float2 vizUV : TEXCOORD4;
            float4 lightCoord : TEXCOORD5;
          #endif
          UNITY_VERTEX_INPUT_INSTANCE_ID
          UNITY_VERTEX_OUTPUT_STEREO
        };
        float4 _MainTex_ST;

        // vertex shader
        v2f_surf vert_surf (appdata_full v) {
          UNITY_SETUP_INSTANCE_ID(v);
          v2f_surf o;
          UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
          UNITY_TRANSFER_INSTANCE_ID(v,o);
          UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
          o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #ifdef EDITOR_VISUALIZATION
            o.vizUV = 0;
            o.lightCoord = 0;
            if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
            o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
            else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
            {
              o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
              o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
            }
          #endif
          o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
          float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
          float3 worldNormal = UnityObjectToWorldNormal(v.normal);
          fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
          fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
          fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
          o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
          o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
          o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
          return o;
        }

        // fragment shader
        fixed4 frag_surf (v2f_surf IN) : SV_Target {
          UNITY_SETUP_INSTANCE_ID(IN);
          // prepare and unpack data
          Input surfIN;
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
          #elif defined (FOG_COMBINED_WITH_WORLD_POS)
            UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
          #else
            UNITY_EXTRACT_FOG(IN);
          #endif
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_RECONSTRUCT_TBN(IN);
          #else
            UNITY_EXTRACT_TBN(IN);
          #endif
          UNITY_INITIALIZE_OUTPUT(Input,surfIN);
          surfIN.uv_MainTex.x = 1.0;
          surfIN.uv_DetailAlbedo.x = 1.0;
          surfIN.worldPos.x = 1.0;
          surfIN.uv_MainTex = IN.pack0.xy;
          float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
          #ifndef USING_DIRECTIONAL_LIGHT
            fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
          #else
            fixed3 lightDir = _WorldSpaceLightPos0.xyz;
          #endif
          #ifdef UNITY_COMPILER_HLSL
            SurfaceOutputStandardSpecular o = (SurfaceOutputStandardSpecular)0;
          #else
            SurfaceOutputStandardSpecular o;
          #endif
          o.Albedo = 0.0;
          o.Emission = 0.0;
          o.Specular = 0.0;
          o.Alpha = 0.0;
          o.Occlusion = 1.0;
          fixed3 normalWorldVertex = fixed3(0,0,1);

          // call surface function
          surf (surfIN, o);
          UnityMetaInput metaIN;
          UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
          metaIN.Albedo = o.Albedo;
          metaIN.Emission = o.Emission;
          metaIN.SpecularColor = o.Specular;
          #ifdef EDITOR_VISUALIZATION
            metaIN.VizUV = IN.vizUV;
            metaIN.LightCoord = IN.lightCoord;
          #endif
          return UnityMetaFragment(metaIN);
        }


      #endif

      // -------- variant for: INSTANCING_ON 
      #if defined(INSTANCING_ON)
        // Surface shader code generated based on:
        // writes to per-pixel normal: YES
        // writes to emission: YES
        // writes to occlusion: YES
        // needs world space reflection vector: no
        // needs world space normal vector: no
        // needs screen space position: no
        // needs world space position: no
        // needs view direction: no
        // needs world space view direction: no
        // needs world space position for lighting: YES
        // needs world space view direction for lighting: YES
        // needs world space view direction for lightmaps: no
        // needs vertex color: no
        // needs VFACE: no
        // passes tangent-to-world matrix to pixel shader: YES
        // reads from normal: no
        // 1 texcoords actually used
        //   float2 _MainTex
        #include "UnityCG.cginc"
        #include "Lighting.cginc"
        #include "UnityPBSLighting.cginc"

        #define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
        #define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
        #define WorldNormalVector(data,normal) fixed3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))

        // Original surface shader snippet:

        #ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
        #endif
        /* UNITY: Original start of shader */
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf StandardSpecular fullforwardshadows
        //#pragma target 5.0
        struct Input
        {
          float2 uv_MainTex;
          float2 uv_DetailAlbedo;
          float3 worldPos;
        };
        float _SpecularIntensity;
        float _MetallicIntensity;
        float4 _EmissionColor;
        float _Occlusion;
        float _VertexScale;
        float _VertexOffset;
        float _Cutoff;
        float _EmissionMultiplier;
        sampler2D _BumpMap;
        sampler2D _SpecularMap;
        sampler2D _MainTex;
        sampler2D _DetailAlbedo; 
        sampler2D _DetailNormal;
        sampler2D _EmissionMap;
        float4 _TileOffset;
        float _Glossiness;
        float2 _NormalIntensity;
        float4 _Color;
        
        void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
          float2 uv = IN.uv_MainTex;
          uv = uv * _TileOffset.xy + _TileOffset.zw;
          float2 detailUV = IN.uv_DetailAlbedo;
          #ifdef USE_WHITE
            o.Normal = float3(0,0,1);
            o.Albedo = 1;
            o.Smoothness = 0;
            o.Specular = 0;
            o.Emission = 0;
            float4 spec = tex2D(_SpecularMap,uv);
            o.Occlusion = lerp(1, spec.b, _Occlusion);
          #else
            float4 spec = tex2D(_SpecularMap,uv);
            float4 c = tex2D (_MainTex, uv);
            #if CUT_OFF
              clip(c.a * _Color.a - _Cutoff);
            #endif

            o.Normal = UnpackNormal(tex2D(_BumpMap,uv));
            o.Normal.xy *= _NormalIntensity.xy;
            o.Albedo = c.rgb;

            o.Albedo *= _Color.rgb;

            o.Alpha = 1;
            o.Occlusion = lerp(1, spec.b, _Occlusion);
            o.Specular = lerp(_SpecularIntensity, o.Albedo, _MetallicIntensity * spec.g); 
            o.Smoothness = _Glossiness * spec.r;
            o.Emission = _EmissionColor * tex2D(_EmissionMap, uv) * _EmissionMultiplier;
          #endif
        }
        
        #include "UnityMetaPass.cginc"

        // vertex-to-fragment interpolation data
        struct v2f_surf {
          UNITY_POSITION(pos);
          float2 pack0 : TEXCOORD0; // _MainTex
          float4 tSpace0 : TEXCOORD1;
          float4 tSpace1 : TEXCOORD2;
          float4 tSpace2 : TEXCOORD3;
          #ifdef EDITOR_VISUALIZATION
            float2 vizUV : TEXCOORD4;
            float4 lightCoord : TEXCOORD5;
          #endif
          UNITY_VERTEX_INPUT_INSTANCE_ID
          UNITY_VERTEX_OUTPUT_STEREO
        };
        float4 _MainTex_ST;

        // vertex shader
        v2f_surf vert_surf (appdata_full v) {
          UNITY_SETUP_INSTANCE_ID(v);
          v2f_surf o;
          UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
          UNITY_TRANSFER_INSTANCE_ID(v,o);
          UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
          o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
          #ifdef EDITOR_VISUALIZATION
            o.vizUV = 0;
            o.lightCoord = 0;
            if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
            o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
            else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
            {
              o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
              o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
            }
          #endif
          o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
          float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
          float3 worldNormal = UnityObjectToWorldNormal(v.normal);
          fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
          fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
          fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
          o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
          o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
          o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
          return o;
        }

        // fragment shader
        fixed4 frag_surf (v2f_surf IN) : SV_Target {
          UNITY_SETUP_INSTANCE_ID(IN);
          // prepare and unpack data
          Input surfIN;
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
          #elif defined (FOG_COMBINED_WITH_WORLD_POS)
            UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
          #else
            UNITY_EXTRACT_FOG(IN);
          #endif
          #ifdef FOG_COMBINED_WITH_TSPACE
            UNITY_RECONSTRUCT_TBN(IN);
          #else
            UNITY_EXTRACT_TBN(IN);
          #endif
          UNITY_INITIALIZE_OUTPUT(Input,surfIN);
          surfIN.uv_MainTex.x = 1.0;
          surfIN.uv_DetailAlbedo.x = 1.0;
          surfIN.worldPos.x = 1.0;
          surfIN.uv_MainTex = IN.pack0.xy;
          float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
          #ifndef USING_DIRECTIONAL_LIGHT
            fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
          #else
            fixed3 lightDir = _WorldSpaceLightPos0.xyz;
          #endif
          #ifdef UNITY_COMPILER_HLSL
            SurfaceOutputStandardSpecular o = (SurfaceOutputStandardSpecular)0;
          #else
            SurfaceOutputStandardSpecular o;
          #endif
          o.Albedo = 0.0;
          o.Emission = 0.0;
          o.Specular = 0.0;
          o.Alpha = 0.0;
          o.Occlusion = 1.0;
          fixed3 normalWorldVertex = fixed3(0,0,1);

          // call surface function
          surf (surfIN, o);
          UnityMetaInput metaIN;
          UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
          metaIN.Albedo = o.Albedo;
          metaIN.Emission = o.Emission;
          metaIN.SpecularColor = o.Specular;
          #ifdef EDITOR_VISUALIZATION
            metaIN.VizUV = IN.vizUV;
            metaIN.LightCoord = IN.lightCoord;
          #endif
          return UnityMetaFragment(metaIN);
        }


      #endif


      ENDCG

    }

    // ---- end of surface shader generated code

  }
  FallBack "Diffuse"
}