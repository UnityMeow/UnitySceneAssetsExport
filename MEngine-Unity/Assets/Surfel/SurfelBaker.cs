using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.IO;
using Unity.Collections;
public struct ushort4
{
    public ushort x;
    public ushort y;
    public ushort z;
    public ushort w;
}
[RequireComponent(typeof(DistanceToCam))]
public unsafe class SurfelBaker : MonoBehaviour
{
    public struct CellData
    {
        public float3 position;
        public float3 normal;
        public float3 albedo;
    };
    public struct SDFPrimitive
    {
        public float4x4 localToWorldMatrix;
        public uint type;
    };
    [DllImport("DLL_Tool")] public static extern void InitCellProcessor(ulong* handlePtr);
    [DllImport("DLL_Tool")] public static extern void DisposeCellProcessor(ulong handle);
    [DllImport("DLL_Tool")] public static extern void ClearData(ulong handle, float3 cubeStartPos, float3 cubeExtent, uint3 cellResolution);//Change
    [DllImport("DLL_Tool")] public static extern void AddCellDatas(ulong handle, DistanceToCam.GeometryData* cells, ulong count);//Change
    [DllImport("DLL_Tool")] public static extern void RunProcess(ulong handle);
    [DllImport("DLL_Tool")] public static extern void GetAllCellData(ulong handle, CellData** datas, ulong* dataCount);
    [DllImport("DLL_Tool")] public static extern void GetSurfelCellBoundingBox(ulong handle, float3* center, float3* extent);
    [DllImport("DLL_Tool")] public static extern void GetAllProbeIndices(ulong handle, int** indices, ulong* indCount);
    [DllImport("DLL_Tool")]
    public static extern void ProbeMipCulling(
        float* distances, float3* positions, uint* outputVoxelIndices,
        uint3 probeResolution, float3 cascadeDistances, uint4* probeOffset);
    [DllImport("DLL_Tool")]
    public static extern void SetPrimitives(ulong handle, SDFPrimitive* primitive, uint primitiveCount, uint3 cullResolution, float distExtent);
    [DllImport("DLL_Tool")]
    public static extern void GetSDFCullResultData(ulong handle, uint4** cullResult);
    public struct SurfelSaveHeader
    {
        public double3 originPos;
        public float3 worldSize;
        public float3 boundingCenter;
        public float3 boundingExtent;
        public uint3 probeResolution;
        public uint cellDataCount;
        public uint cellIndicesCount;
        public uint primitiveCount;
        public uint4 voxelMipOffset;
        public uint3 sdfCullResolution;
    }
    public uint3 cellDensity = uint3(4, 4, 4);
    public uint3 probeResolution = uint3(16, 8, 16);
    public uint3 sdfCullResolution = uint3(8, 4, 8);
    private DistanceToCam distToCam;
    public GameObject sphereTest;
    public Transform[] sdfCubes;
    public Transform[] sdfCylinders;
    public float targetDist = 0.1f;
    public struct Cube
    {
        public float4x4 worldToLocalMatrix;
        public float3 localExtent;
        public uint type;
    };
    public GameObject[] parents;
    public GameObject testSphere;
    public float3 cascadeDistances = float3(1, 2, 3);
    // Start is called before the first frame update
    [EasyButtons.Button]
    void PrintData()
    {
        float3 originPos = transform.position;
        transform.position = Vector3.zero;
        distToCam = GetComponent<DistanceToCam>();
        distToCam.Init(-originPos);
        //ComputeBuffer skyOcclusionBuffer = null;
        ComputeBuffer geometrySampleData = null;
        float3 cubeStartPos = transform.position - 0.5f * transform.localScale;
        float3 cubeSize = transform.localScale;
        //Get ProbePosition
        ComputeShader cs = distToCam.surfelshader;
        float3[] probePositions = new float3[probeResolution.x * probeResolution.y * probeResolution.z];
        ComputeBuffer probeBuffer = new ComputeBuffer(probePositions.Length, sizeof(float3));
        ComputeBuffer probeDistanceBuffer = new ComputeBuffer(probePositions.Length, sizeof(float));
        if (sdfCubes == null) sdfCubes = new Transform[0];
        if (sdfCylinders == null) sdfCylinders = new Transform[0];
        NativeArray<Cube> cubeDatas = new NativeArray<Cube>(sdfCubes.Length + sdfCylinders.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < sdfCubes.Length; ++i)
        {
            Cube c;
            Transform tr = sdfCubes[i];
            float4x4 localToWorld = new float4x4();
            localToWorld.c0 = float4(tr.right, 0);
            localToWorld.c1 = float4(tr.up, 0);
            localToWorld.c2 = float4(tr.forward, 0);
            localToWorld.c3 = float4(tr.position, 1);
            c.worldToLocalMatrix = inverse(localToWorld);
            c.localExtent = abs(tr.localScale * 0.5f);
            c.type = 0;
            cubeDatas[i] = c;
        }
        for (int i = sdfCubes.Length; i < cubeDatas.Length; ++i)
        {
            Cube c;
            Transform tr = sdfCylinders[i - sdfCubes.Length];
            float4x4 localToWorld = new float4x4();
            localToWorld.c0 = float4(tr.right, 0);
            localToWorld.c1 = float4(tr.up, 0);
            localToWorld.c2 = float4(tr.forward, 0);
            localToWorld.c3 = float4(tr.position, 1);
            c.worldToLocalMatrix = inverse(localToWorld);
            c.localExtent = abs(tr.localScale * 0.5f);
            c.type = 1;
            cubeDatas[i] = c;
        }
        ComputeBuffer primBuffer = new ComputeBuffer(max(1, cubeDatas.Length), sizeof(Cube));
        if (cubeDatas.Length > 0)
            primBuffer.SetData(cubeDatas);
        cs.SetVector("_VoxelResolution", float4(probeResolution.x, probeResolution.y, probeResolution.z, 1));
        cs.SetVector("_CubeStartPos", float4(cubeStartPos, 1));
        cs.SetVector("_CubeSize", float4(cubeSize, 1));
        cs.SetBuffer(4, "_PrimitiveBuffer", primBuffer);
        cs.SetBuffer(4, "_ProbePos", probeBuffer);
        cs.SetBuffer(4, "_ProbeDistance", probeDistanceBuffer);
        cs.SetBuffer(4, "_SphereBuffer", primBuffer);
        cs.SetInt("_PrimitiveCount", sdfCubes.Length + sdfCylinders.Length);
        cs.SetInt("_SphereCount", 0);
        cs.SetInt("_IterateTimes", 8);
        cs.SetFloat("_TargetDistance", targetDist);
        cs.Dispatch(4, (int)probeResolution.x / 4, (int)probeResolution.y / 4, (int)probeResolution.z / 4);
        probeBuffer.GetData(probePositions);
        float[] allProbeDistances = new float[probePositions.Length];
        probeDistanceBuffer.GetData(allProbeDistances);
        //Probe Cull
        uint4 probeLevelCounts = 0;
        NativeArray<uint> allVoxelIndices = new NativeArray<uint>(allProbeDistances.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        ProbeMipCulling(
            allProbeDistances.Ptr(),
            probePositions.Ptr(),
            allVoxelIndices.Ptr(),
            probeResolution,
            cascadeDistances,
            probeLevelCounts.Ptr());
        //Debug
        if (parents != null)
        {
            foreach(var i in parents)
            {
                DestroyImmediate(i);
            }
        }
        parents = new GameObject[4];
        parents[0] = new GameObject("Parent0");
        parents[1] = new GameObject("Parent1");
        parents[2] = new GameObject("Parent2");
        parents[3] = new GameObject("Parent3");
        for (uint i = 0; i < probeLevelCounts.x; ++i)
        {
            Instantiate(testSphere, probePositions[i], Quaternion.identity, parents[0].transform);
        }
        for (uint i = probeLevelCounts.x; i < probeLevelCounts.y; ++i)
        {
            Instantiate(testSphere, probePositions[i], Quaternion.identity, parents[1].transform);
        }
        for (uint i = probeLevelCounts.y; i < probeLevelCounts.z; ++i)
        {
            Instantiate(testSphere, probePositions[i], Quaternion.identity, parents[2].transform);
        }
        for (uint i = probeLevelCounts.z; i < probeLevelCounts.w; ++i)
        {
            Instantiate(testSphere, probePositions[i], Quaternion.identity, parents[3].transform);
        }
        foreach(var i in parents)
        {
            i.transform.position = originPos;
        }
       
        probeBuffer.Dispose();
        probeDistanceBuffer.Dispose();
        primBuffer.Dispose();
        distToCam.DrawAll(transform.position - 0.5f * transform.localScale, transform.position + 0.5f * transform.localScale, probePositions.Ptr(), (int)probeLevelCounts.w, /*ref skyOcclusionBuffer,*/ ref geometrySampleData);
        DistanceToCam.GeometryData[] geometryDatas = new DistanceToCam.GeometryData[geometrySampleData.count];
        geometrySampleData.GetData(geometryDatas);
        /*    foreach(var i in geometryDatas)
            {
                if (i.avaliable > 0.5f)
                    GameObject.Instantiate(sphereTest, i.position, Quaternion.identity, go.transform);
            }*/
        //Process Surfel Data
        ulong handle = 0;
        InitCellProcessor(handle.Ptr());

        ClearData(handle, cubeStartPos, cubeSize, cellDensity);
        NativeArray<SDFPrimitive> primArr = new NativeArray<SDFPrimitive>(sdfCubes.Length + sdfCylinders.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < sdfCubes.Length; ++i)
        {
            primArr[i] = new SDFPrimitive
            {
                localToWorldMatrix = sdfCubes[i].localToWorldMatrix,
                type = 0
            };
        }
        for (int i = 0; i < sdfCylinders.Length; ++i)
        {
            primArr[i + sdfCubes.Length] = new SDFPrimitive
            {
                localToWorldMatrix = sdfCylinders[i].localToWorldMatrix,
                type = 1
            };
        }
       
        AddCellDatas(handle, geometryDatas.Ptr(), (ulong)geometryDatas.Length);
        float3 pixelExtent = cubeSize / float3(probeResolution);
        
        SetPrimitives(handle, primArr.Ptr(), (uint)primArr.Length, sdfCullResolution, max(pixelExtent.x, max(pixelExtent.z, pixelExtent.y)));

        RunProcess(handle);
        ulong cellData_Ptr = 0; ulong cellData_Count = 0;
        ulong indices_Ptr = 0; ulong indCount = 0;
        ulong sdfCullResults_Ptr = 0;
       
        GetAllCellData(handle, (CellData**)cellData_Ptr.Ptr(), cellData_Count.Ptr());
        GetAllProbeIndices(handle, (int**)indices_Ptr.Ptr(), indCount.Ptr());
        GetSDFCullResultData(handle, (uint4**)sdfCullResults_Ptr.Ptr());
        //Output
        ulong headerSize = (ulong)sizeof(SurfelSaveHeader);
        // ulong skyOccProbeSize = (ulong)(sizeof(float3x3) * skyOcclusionBuffer.count);
        // ulong probeSize = (ulong)(sizeof(float3) * probePositions.Length);
        ulong cellDataSize = (cellData_Count * (ulong)sizeof(CellData));
        ulong cellIndicesSize = (indCount * sizeof(int));
        ulong voxelIndicesSize = (probeLevelCounts.w * sizeof(uint));
        ulong primitivesDataSize = (ulong)(cubeDatas.Length * sizeof(Cube));
        ulong primitiveCullSize = (ulong)(sdfCullResolution.x * sdfCullResolution.y * sdfCullResolution.z * sizeof(uint4));
        byte[] saveBytes = new byte[
            headerSize
             //   + probeSize
             + cellDataSize
             + cellIndicesSize
             + voxelIndicesSize
             + primitivesDataSize
             + primitiveCullSize];
        /* float3x3[] cubeParams = new float3x3[skyOcclusionBuffer.count];
         skyOcclusionBuffer.GetData(cubeParams);*/
        //Header
        SurfelSaveHeader header = new SurfelSaveHeader();
        GetSurfelCellBoundingBox(handle, header.boundingCenter.Ptr(), header.boundingExtent.Ptr());
        header.cellIndicesCount = (uint)indCount;
        header.cellDataCount = (uint)cellData_Count;
        header.probeResolution = probeResolution;
        header.voxelMipOffset = probeLevelCounts;
        header.sdfCullResolution = sdfCullResolution;
        header.primitiveCount = (uint)cubeDatas.Length;
        header.originPos = originPos;
        header.worldSize = transform.localScale;
        byte* writingByte = saveBytes.Ptr();
        UnsafeUtility.MemCpy(writingByte, header.Ptr(), (long)headerSize);
        writingByte += headerSize;

        //Probe
        //    UnsafeUtility.MemCpy(writingByte, probePositions.Ptr(), (long)probeSize);
        //  writingByte += probeSize;
        //Surfel Cell
        UnsafeUtility.MemCpy(writingByte, (CellData*)cellData_Ptr, (long)cellDataSize);
        writingByte += cellDataSize;
        //Cell Indices
        UnsafeUtility.MemCpy(writingByte, (int*)indices_Ptr, (long)cellIndicesSize);
        writingByte += cellIndicesSize;
        //Voxel Output
        UnsafeUtility.MemCpy(writingByte, allVoxelIndices.GetUnsafePtr(), (long)voxelIndicesSize);
        writingByte += voxelIndicesSize;
        UnsafeUtility.MemCpy(writingByte, cubeDatas.GetUnsafePtr(), (long)primitivesDataSize);
        writingByte += primitivesDataSize;
        UnsafeUtility.MemCpy(writingByte, (uint4*)sdfCullResults_Ptr, (long)primitiveCullSize);
        //Output
        using (FileStream fsm = new FileStream("Surfel.bytes", FileMode.Create))
        {
            fsm.Write(saveBytes, 0, saveBytes.Length);
        }
        distToCam.Dispose();
        //  skyOcclusionBuffer.Dispose();
        geometrySampleData.Dispose();
        DisposeCellProcessor(handle);
        distToCam.MoveTheWorld(originPos);
        transform.position = originPos;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}

//TODO
//Export Primitives