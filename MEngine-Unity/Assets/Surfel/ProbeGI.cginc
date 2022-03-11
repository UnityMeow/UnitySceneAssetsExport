#ifndef PROBE_GI_INCLUDE
    #define PROBE_GI_INCLUDE
    #define PI 3.1415926536
    #define SURFEL_COUNT 512
    #include "Plane.cginc"
    #include "SH.cginc"
    struct SurfelSampleData
    {
        float avaliable;
        float3 position;
        float3 normal;
        float3 albedo;
    };
    struct CellData
    {   float3 position;
        float3 normal;
        float3 albedo;
    };
    static const float3 _CellDir[6] = 
    {
        float3(-1, 0, 0),
        float3(1, 0, 0),
        float3(0, 1, 0),
        float3(0, -1, 0),
        float3(0, 0, -1),
        float3(0, 0, 1)
    };
 
#endif