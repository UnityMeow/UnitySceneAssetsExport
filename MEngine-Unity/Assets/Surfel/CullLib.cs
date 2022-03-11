using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Mathematics;

public unsafe static class CullLib
{
    [DllImport("CullLib")] public static extern void Initialize(float fov, float aspect, float nearPlane, float farPlane);
    [DllImport("CullLib")] public static extern void ClearObjects();
    [DllImport("CullLib")] public static extern void PushObject(float3 right, float3 up, float3 forward, float3 localScale, float3 position, float3 boundCenter, float3 boundExtent);
    [DllImport("CullLib")] public static extern void GetCullResult(uint index, uint** resultData, uint* length);
    [DllImport("CullLib")] public static extern void StartNextCull(float3 position);
    [DllImport("CullLib")] public static extern void Dispose();
    [DllImport("CullLib")] public static extern void Wait();
    [DllImport("CullLib")] public static extern void GetCubemapViewMarices(float3 position, float4x4* viewMatrices);
}
