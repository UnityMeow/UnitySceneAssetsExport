using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;
public static unsafe class Montcalo
{
    [DllImport("DLL_Tool")] public static extern void ReverseBits32(uint bits, uint* result);
    [DllImport("DLL_Tool")] public static extern void ReverseBits32_UInt3(uint3 bits, uint3* result);
    [DllImport("DLL_Tool")] public static extern void SobolIndex(uint2 Base, int Index, int Bits, uint2* result); // Bits default: 10
    [DllImport("DLL_Tool")] public static extern void HaltonSequence(uint Index, uint baseV, uint* result);       // Bits default: 3
    [DllImport("DLL_Tool")] public static extern void Hammersley_DefaultRandom(uint Index, uint NumSamples, float2* result);
    [DllImport("DLL_Tool")] public static extern void Hammersley(uint Index, uint NumSamples, uint2 Random, float2* result);
    [DllImport("DLL_Tool")] public static extern void GetTangentBasis(float3 TangentZ, float4x4* result);
    [DllImport("DLL_Tool")] public static extern void TangentToWorld_Vec3(float3 Vec, float3 TangentZ, float3* result);
    [DllImport("DLL_Tool")] public static extern void TangentToWorld_Vec4(float3 Vec, float4 TangentZ, float4* result);
    [DllImport("DLL_Tool")] public static extern void RandToCircle(uint2 Rand, float2* result);
    [DllImport("DLL_Tool")] public static extern void UniformSampleSphere(float2 E, float4* result);
    [DllImport("DLL_Tool")] public static extern void UniformSampleHemisphere(float2 E, float4* result);
    [DllImport("DLL_Tool")] public static extern void UniformSampleDisk(float2 Random, float2* result);
    [DllImport("DLL_Tool")] public static extern void CosineSampleHemisphere(float2 E, float4* result);
    [DllImport("DLL_Tool")] public static extern void UniformSampleCone(float2 E, float CosThetaMax, float4* result);
    [DllImport("DLL_Tool")] public static extern void ImportanceSampleLambert(float2 E, float4* result);
    [DllImport("DLL_Tool")] public static extern void ImportanceSampleBlinn(float2 E, float Roughness, float4* result);
    [DllImport("DLL_Tool")] public static extern void ImportanceSampleGGX_N(float2 E, float3 N, float Roughness, float3* result);
    [DllImport("DLL_Tool")] public static extern void ImportanceSampleGGX(float2 E, float Roughness, float4* result);
    [DllImport("DLL_Tool")] public static extern void ImportanceSampleInverseGGX(float2 E, float Roughness, float4* result);
    [DllImport("DLL_Tool")] public static extern void GetHammersleySphereDistribution(uint count, float3* results);
    [DllImport("DLL_Tool")] public static extern void SampleAnisoGGXDir(float2 u, float3 V, float3 N, float3 tX, float3 tY, float roughnessT, float roughnessB, float3* H, float3* L);
    [DllImport("DLL_Tool")]
    public static extern void ImportanceSampleAnisoGGX(float2 u, float3 V, float3 N, float3 tX, float3 tY,
       float roughnessT, float roughnessB, float NoV, float3* L, float* VoH, float* NoL, float* weightOverPdf);
    [DllImport("DLL_Tool")] public static extern void MISWeight(uint Num, float PDF, uint OtherNum, float OtherPDF, float* result);
    [DllImport("DLL_Tool")] public static extern void OutputSurfelData(uint3 size, byte* pathChr, uint pathLen, float3* results);
    [DllImport("DLL_Tool")] public static extern void InputSurfelData(uint3 size, byte* pathChr, uint pathLen, float3* results);
}
