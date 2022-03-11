using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.IO;
using Unity.Collections;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using UnityEngine.Experimental.AI;
public struct Tet
{
    //Point
    public float3 point0;
    public float3 point1;
    public float3 point2;
    public float3 point3;
    //Plane
    public float4 plane0;
    public float4 plane1;
    public float4 plane2;
    public float4 plane3;
    //Plane Size
    public float4 planeSizes;
}
public unsafe class SurfelRender : MonoBehaviour
{
    public struct ProbeRenderElement
    {
        public Tet tet;
        public uint2 indices; //uint16 * 4
    }
    public Shader shadowShader;
    public string loadPath = "Surfel.bytes";
    public Light sunLight;
    public ComputeShader surfelCompute;
    /* private RenderTexture skyOcc0;
     private RenderTexture skyOcc1;
     private RenderTexture skyOcc2;*/

    private ComputeBuffer surfelCellBuffer;     //Upload Buffer
    private ComputeBuffer surfelIndicesBuffer;  //Upload Buffer
    private ComputeBuffer surfelColorBuffer;    //UAV Buffer
    private ComputeBuffer shResultBuffer;       //UAV Buffer
    private ComputeBuffer voxelIndicesBuffer;   //Upload Buffer
    private ComputeBuffer primitiveBuffer;      //Upload Buffer
    private float3 boundingCenter;
    private float3 boundingExtent;
    public Camera shadowCam;
    private RenderTexture shadowMap;
    private uint3 voxelSize;
    private uint4 voxelMipOffset;
    private RenderTexture[] projectionTexs;
    private RenderTexture[] backupProjectionTexs;
    private RenderTexture sdfPrimitiveTex;
    private double3 originPos;
    //Debug
    public GameObject testSphere;
    private float3 sdfSampleOffset;
    private int usedSize = 0;
    void CollectBuffer(ComputeBuffer b)
    {
        usedSize += b.stride * b.count;
    }
    void CollectRT(RenderTexture rt, int pixelsize)
    {
        usedSize += rt.width * rt.height * rt.volumeDepth * pixelsize;
    }
    private void Start()
    {
        Vector3Int v;
        byte[] bs = null;
        using (FileStream fsm = new FileStream(loadPath, FileMode.Open))
        {
            bs = new byte[fsm.Length];
            fsm.Read(bs, 0, (int)fsm.Length);
        }


        SurfelBaker.SurfelSaveHeader header = new SurfelBaker.SurfelSaveHeader();
        byte* btPtr = bs.Ptr();
        //Header
        UnsafeUtility.MemCpy(header.Ptr(), btPtr, sizeof(SurfelBaker.SurfelSaveHeader));
        btPtr += sizeof(SurfelBaker.SurfelSaveHeader);
        boundingCenter = header.boundingCenter;
        boundingExtent = header.boundingExtent;
        voxelMipOffset = header.voxelMipOffset;
        originPos = header.originPos;
        //Probe
        /* NativeArray<float3> probePositions = new NativeArray<float3>((int)(header.probeResolution.x * header.probeResolution.y * header.probeResolution.z), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
         UnsafeUtility.MemCpy(probePositions.GetUnsafePtr(), btPtr, sizeof(float3) * probePositions.Length);
         foreach (var i in probePositions)
         {
             GameObject.Instantiate(testSphere, i, Quaternion.identity);
         }
         btPtr += sizeof(float3) * probePositions.Length;*/

        //Surfel Cell
        NativeArray<SurfelBaker.CellData> cellDatas = new NativeArray<SurfelBaker.CellData>((int)header.cellDataCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        NativeArray<int> cellIndices = new NativeArray<int>((int)header.cellIndicesCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        UnsafeUtility.MemCpy(cellDatas.GetUnsafePtr(), btPtr, sizeof(SurfelBaker.CellData) * cellDatas.Length);
        btPtr += sizeof(SurfelBaker.CellData) * cellDatas.Length;
        //Cell Indices
        UnsafeUtility.MemCpy(cellIndices.GetUnsafePtr(), btPtr, sizeof(int) * cellIndices.Length);
        btPtr += sizeof(int) * cellIndices.Length;


        voxelIndicesBuffer = new ComputeBuffer((int)voxelMipOffset.w, sizeof(uint));
        CollectBuffer(voxelIndicesBuffer);
        NativeArray<uint> voxelIndicesArray = new NativeArray<uint>((int)voxelMipOffset.w, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        UnsafeUtility.MemCpy(voxelIndicesArray.GetUnsafePtr(), btPtr, sizeof(uint) * voxelIndicesArray.Length);
        btPtr += sizeof(uint) * voxelIndicesArray.Length;
        voxelIndicesBuffer.SetData(voxelIndicesArray);
        //Primitive Data
        NativeArray<SurfelBaker.Cube> cubeDatas = new NativeArray<SurfelBaker.Cube>((int)header.primitiveCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        UnsafeUtility.MemCpy(cubeDatas.GetUnsafePtr(), btPtr, sizeof(SurfelBaker.Cube) * cubeDatas.Length);
        btPtr += sizeof(SurfelBaker.Cube) * cubeDatas.Length;
        NativeArray<uint4> cubeCullIndices = new NativeArray<uint4>((int)(header.sdfCullResolution.x * header.sdfCullResolution.y * header.sdfCullResolution.z), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        UnsafeUtility.MemCpy(cubeCullIndices.GetUnsafePtr(), btPtr, sizeof(uint4) * cubeCullIndices.Length);
        primitiveBuffer = new ComputeBuffer(cubeDatas.Length, sizeof(SurfelBaker.Cube));
        CollectBuffer(primitiveBuffer);

        primitiveBuffer.SetData(cubeDatas);
        ComputeBuffer primitiveCullData = new ComputeBuffer(cubeCullIndices.Length, sizeof(uint4));
        CollectBuffer(primitiveCullData);

        primitiveCullData.SetData(cubeCullIndices);
        sdfPrimitiveTex = new RenderTexture(new RenderTextureDescriptor
        {
            width = (int)header.sdfCullResolution.x,
            height = (int)header.sdfCullResolution.y,
            volumeDepth = (int)header.sdfCullResolution.z,
            enableRandomWrite = true,
            msaaSamples = 1,
            dimension = TextureDimension.Tex3D,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_UInt
        });
        CollectRT(sdfPrimitiveTex, 16);
        sdfPrimitiveTex.Create();
        surfelCompute.SetTexture(8, "_UAV_SDFPrimitiveIndices", sdfPrimitiveTex);
        surfelCompute.SetBuffer(8, "_SDFPrimitiveBuffer", primitiveCullData);
        surfelCompute.SetVector("_SDFPrimitiveResolution", float4(float3(header.sdfCullResolution), 1) + 0.5f);
        surfelCompute.Dispatch(8, (int)header.sdfCullResolution.x / 4, (int)header.sdfCullResolution.y / 4, (int)header.sdfCullResolution.z / 4);
        primitiveCullData.Dispose();

        surfelCellBuffer = new ComputeBuffer(cellDatas.Length, sizeof(SurfelBaker.CellData));
        CollectBuffer(surfelCellBuffer);

        surfelColorBuffer = new ComputeBuffer(cellDatas.Length, sizeof(float3));
        CollectBuffer(surfelColorBuffer);

        surfelCellBuffer.SetData(cellDatas);
        surfelIndicesBuffer = new ComputeBuffer(cellIndices.Length, sizeof(int));
        CollectBuffer(surfelIndicesBuffer);

        surfelIndicesBuffer.SetData(cellIndices);



        shadowMap = new RenderTexture(new RenderTextureDescriptor
        {
            width = 2048,
            height = 2048,
            volumeDepth = 1,
            msaaSamples = 1,
            depthBufferBits = 16,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D
        });
        CollectRT(shadowMap, 4);
        shadowMap.Create();
        voxelSize = header.probeResolution;
        shResultBuffer = new ComputeBuffer((int)(voxelMipOffset.w), sizeof(float3) * 9);
        projectionTexs = new RenderTexture[7];
        backupProjectionTexs = new RenderTexture[7];
        for (int i = 0; i < 7; ++i)
        {
            backupProjectionTexs[i] = new RenderTexture(new RenderTextureDescriptor
            {
                width = (int)voxelSize.x,
                height = (int)voxelSize.y,
                volumeDepth = (int)voxelSize.z,
                enableRandomWrite = true,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                msaaSamples = 1,
                dimension = TextureDimension.Tex3D
            });
            CollectRT(backupProjectionTexs[i], 8);

            projectionTexs[i] = new RenderTexture(new RenderTexture(new RenderTextureDescriptor
            {
                width = (int)voxelSize.x,
                height = (int)voxelSize.y,
                volumeDepth = (int)voxelSize.z,
                enableRandomWrite = true,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                msaaSamples = 1,
                dimension = TextureDimension.Tex3D
            }));
            CollectRT(projectionTexs[i], 8);

            projectionTexs[i].Create();
            backupProjectionTexs[i].Create();
        }
        StartCoroutine(ExecuteRendering());
        Debug.Log(usedSize / 1024 / 1024);
    }
    float3 cameraWorldPos;
    private void DrawShadow()
    {
        float4x4 camTRS = 0;
        camTRS.c0 = float4(sunLight.transform.right, 0);
        camTRS.c1 = float4(sunLight.transform.up, 0);
        camTRS.c2 = float4(sunLight.transform.forward, 0);
        camTRS.c3 = float4(0, 0, 0, 1);
        camTRS = transpose(camTRS);
        float3* localExtent = stackalloc float3[]
        {
            float3(0.5f, 0.5f, 0.5f),
            float3(0.5f, 0.5f, -0.5f),
            float3(0.5f, -0.5f, 0.5f),
            float3(0.5f, -0.5f, -0.5f),
            float3(-0.5f, 0.5f, 0.5f),
            float3(-0.5f, 0.5f, -0.5f),
            float3(-0.5f, -0.5f, 0.5f),
            float3(-0.5f, -0.5f, -0.5f)
        };
        float3 maxPos = float3(float.MinValue, float.MinValue, float.MinValue);
        float3 minPos = float3(float.MaxValue, float.MaxValue, float.MaxValue);
        for (int i = 0; i < 8; ++i)
        {
            float3 camPos = mul(camTRS, float4((float3)boundingCenter + localExtent[i] * boundingExtent, 1)).xyz;
            maxPos = max(maxPos, camPos);
            minPos = min(minPos, camPos);
        }
        cameraWorldPos = mul(transpose(camTRS), float4(maxPos * 0.5f + minPos * 0.5f, 1)).xyz;
        float3 camSize = maxPos - minPos;
        shadowCam.orthographic = true;
        shadowCam.enabled = false;
        shadowCam.SetReplacementShader(shadowShader, "RenderType");
        shadowCam.orthographicSize = camSize.y * 0.5f;
        shadowCam.farClipPlane = camSize.z * 0.5f + 500;
        shadowCam.nearClipPlane = -shadowCam.farClipPlane - 500;
        shadowCam.aspect = camSize.x / camSize.y;
        shadowCam.transform.position = (Vector3)cameraWorldPos + transform.position;
        shadowCam.transform.rotation = sunLight.transform.rotation;
        shadowCam.targetTexture = shadowMap;
        shadowCam.clearFlags = CameraClearFlags.Color;
        shadowCam.backgroundColor = Color.black;
        shadowCam.Render();

        surfelCompute.SetMatrix("_ShadowVP", mul(GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, false), shadowCam.worldToCameraMatrix));

    }
    void SetUAVTextures(int pass, RenderTexture[] projectionTexs)
    {
        surfelCompute.SetTexture(pass, "_SurfelSHTex0", projectionTexs[0]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex1", projectionTexs[1]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex2", projectionTexs[2]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex3", projectionTexs[3]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex4", projectionTexs[4]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex5", projectionTexs[5]);
        surfelCompute.SetTexture(pass, "_SurfelSHTex6", projectionTexs[6]);
    }

    void SetSRVTextures(int pass, RenderTexture[] projectionTexs)
    {
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex0", projectionTexs[0]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex1", projectionTexs[1]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex2", projectionTexs[2]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex3", projectionTexs[3]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex4", projectionTexs[4]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex5", projectionTexs[5]);
        surfelCompute.SetTexture(pass, "_SRV_SurfelSHTex6", projectionTexs[6]);
    }
    void FirstBounceCell()
    {
        Color c = sunLight.color;
        c *= sunLight.intensity;
        surfelCompute.SetInt("_Count", surfelCellBuffer.count); //header.celldatacount
        surfelCompute.SetVector("_SunLightColor", *(Vector4*)c.Ptr());
        surfelCompute.SetBuffer(0, "_SRV_InputCellGeometry", surfelCellBuffer);
        surfelCompute.SetBuffer(0, "_UAV_OutputCellColor", surfelColorBuffer);
        surfelCompute.SetTexture(0, "_ShadowMap", shadowMap);
        surfelCompute.Dispatch(0, (63 + surfelCellBuffer.count) / 64, 1, 1);

    }
    void FirstBounceProbe()
    {
        surfelCompute.SetBuffer(1, "_SRV_InputCellColor", surfelColorBuffer);
        surfelCompute.SetBuffer(1, "_SurfelIndices", surfelIndicesBuffer);
        surfelCompute.SetBuffer(1, "_UAV_SHResultBuffer", shResultBuffer);
        surfelCompute.Dispatch(1, (int)(voxelMipOffset.w), 1, 1);
    }
    void SecondBounceCell()
    {
        //TODO
        //Set Textures
        SetSRVTextures(2, backupProjectionTexs);

        surfelCompute.SetVector("_SDFPrimitiveResolution", float4(sdfPrimitiveTex.width, sdfPrimitiveTex.height, sdfPrimitiveTex.volumeDepth, 1) + 0.5f);
        surfelCompute.SetVector("_SDFUVOffsetIntensity", float4(sdfSampleOffset, 1));
        surfelCompute.SetBuffer(2, "_SRV_PrimitiveBuffer", primitiveBuffer);
        surfelCompute.SetTexture(2, "_SRV_SDFPrimitiveIndices", sdfPrimitiveTex);

        surfelCompute.SetInt("_Count", surfelCellBuffer.count);
        surfelCompute.SetBuffer(2, "_SRV_InputCellGeometry", surfelCellBuffer);
        surfelCompute.SetBuffer(2, "_UAV_OutputCellColor", surfelColorBuffer);
        surfelCompute.Dispatch(2, (63 + surfelCellBuffer.count) / 64, 1, 1);

    }
    void SecondBounceProbe()
    {
        surfelCompute.SetBuffer(3, "_SRV_InputCellColor", surfelColorBuffer);
        surfelCompute.SetBuffer(3, "_SurfelIndices", surfelIndicesBuffer);
        surfelCompute.SetBuffer(3, "_UAV_SHResultBuffer", shResultBuffer);
        surfelCompute.Dispatch(3, (int)(voxelMipOffset.w), 1, 1);
    }

    void BufferInjectToTexture(RenderTexture[] projectionTexs)
    {

        surfelCompute.SetInt("_Mip0BufferCount", (int)voxelMipOffset.x);
        surfelCompute.SetInt("_Mip1BufferCount", (int)voxelMipOffset.y);
        surfelCompute.SetInt("_Mip2BufferCount", (int)voxelMipOffset.z);
        if (voxelMipOffset.x > 0)
        {
            SetUAVTextures(4, projectionTexs);
            surfelCompute.SetBuffer(4, "_SRV_SHResultBuffer", shResultBuffer);
            surfelCompute.SetBuffer(4, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(4, (int)(voxelMipOffset.x + 63) / 64, 1, 1);
        }
        if (voxelMipOffset.y - voxelMipOffset.x > 0)
        {
            SetUAVTextures(5, projectionTexs);
            surfelCompute.SetBuffer(5, "_SRV_SHResultBuffer", shResultBuffer);

            surfelCompute.SetBuffer(5, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(5, (int)(voxelMipOffset.y - voxelMipOffset.x + 7) / 8, 1, 1);
        }
        if (voxelMipOffset.z - voxelMipOffset.y > 0)
        {
            SetUAVTextures(6, projectionTexs);
            surfelCompute.SetBuffer(6, "_SRV_SHResultBuffer", shResultBuffer);

            surfelCompute.SetBuffer(6, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(6, (int)(voxelMipOffset.z - voxelMipOffset.y), 1, 1);
        }
        if (voxelMipOffset.w - voxelMipOffset.z > 0)
        {
            SetUAVTextures(7, projectionTexs);
            surfelCompute.SetBuffer(7, "_SRV_SHResultBuffer", shResultBuffer);
            surfelCompute.SetBuffer(7, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(7, (int)(voxelMipOffset.w - voxelMipOffset.z), 1, 1);
        }
    }
    const int GenerateTex3DMip = 8;
    const int TextureMip1ToMip0 = 9;
    const int TextureMip2ToMip0 = 10;
    const int TextureMip3ToMip0 = 11;
    const int CopyTex3D = 12;

    void TextureMipmapGenerate(RenderTexture[] backupProjection, RenderTexture[] projection)
    {
        for (uint i = 0; i < 7; ++i)
        {
            surfelCompute.SetTexture(GenerateTex3DMip, "_TexMip0", backupProjection[i]);
            surfelCompute.SetTexture(GenerateTex3DMip, "_TexMip1", projection[i], 1);
            surfelCompute.SetTexture(GenerateTex3DMip, "_TexMip2", projection[i], 2);
            surfelCompute.Dispatch(GenerateTex3DMip, (int)voxelSize.x / 8, (int)voxelSize.y / 8, (int)voxelSize.z / 8);
        }
        SetUAVTextures(CopyTex3D, projection);
        SetSRVTextures(CopyTex3D, backupProjection);
        surfelCompute.Dispatch(CopyTex3D, (int)voxelSize.x / 4, (int)voxelSize.y / 4, (int)voxelSize.z / 4);

        if (voxelMipOffset.y - voxelMipOffset.x > 0)
        {
            SetUAVTextures(TextureMip1ToMip0, backupProjection);
            SetSRVTextures(TextureMip1ToMip0, projection);
            surfelCompute.SetBuffer(TextureMip1ToMip0, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(TextureMip1ToMip0, (int)(voxelMipOffset.y - voxelMipOffset.x + 7) / 8, 1, 1);
           
        }
        if (voxelMipOffset.z - voxelMipOffset.y > 0)
        {
            SetUAVTextures(TextureMip2ToMip0, backupProjection);
            SetSRVTextures(TextureMip2ToMip0, projection);
            surfelCompute.SetBuffer(TextureMip2ToMip0, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(TextureMip2ToMip0, (int)(voxelMipOffset.z - voxelMipOffset.y), 1, 1);
        }
        if (voxelMipOffset.w - voxelMipOffset.z > 0)
        {
            SetUAVTextures(TextureMip3ToMip0, backupProjection);
            SetSRVTextures(TextureMip3ToMip0, projection);
            surfelCompute.SetBuffer(TextureMip3ToMip0, "_VoxelIndexBuffer", voxelIndicesBuffer);
            surfelCompute.Dispatch(TextureMip3ToMip0, (int)(voxelMipOffset.w - voxelMipOffset.z), 1, 1);
        }
        SetUAVTextures(CopyTex3D, projection);
        SetSRVTextures(CopyTex3D, backupProjection);
        surfelCompute.Dispatch(CopyTex3D, (int)voxelSize.x / 4, (int)voxelSize.y / 4, (int)voxelSize.z / 4);
    }
    void IndirectBounce()
    {
        BufferInjectToTexture(backupProjectionTexs);
        SecondBounceCell();
        SecondBounceProbe();
    }
    IEnumerator ExecuteRendering()
    {
        while (true)
        {
            yield return null;
            surfelCompute.SetVector("_VoxelResolution", float4(voxelSize.x, voxelSize.y, voxelSize.z, 1) + 0.5f);
          
            DrawShadow();
            Shader.SetGlobalVector("_CubeStartPos", -0.5f * transform.localScale);
            Shader.SetGlobalVector("_VoxelResolution", float4(voxelSize.x, voxelSize.y, voxelSize.z, 1) + 0.5f);
            Shader.SetGlobalVector("_CubeSize", transform.localScale);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex0", projectionTexs[0]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex1", projectionTexs[1]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex2", projectionTexs[2]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex3", projectionTexs[3]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex4", projectionTexs[4]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex5", projectionTexs[5]);
            Shader.SetGlobalTexture("_SRV_SurfelSHTex6", projectionTexs[6]);
            Shader.SetGlobalVector("_OriginPos", float4(transform.position, 1));
            Shader.SetGlobalVector("_SDFPrimitiveResolution", float4(sdfPrimitiveTex.width, sdfPrimitiveTex.height, sdfPrimitiveTex.volumeDepth, 1) + 0.5f);
            Shader.SetGlobalVector("_SDFUVOffsetIntensity", float4(sdfSampleOffset, 1));
            Shader.SetGlobalBuffer("_SRV_PrimitiveBuffer", primitiveBuffer);
            Shader.SetGlobalTexture("_SRV_SDFPrimitiveIndices", sdfPrimitiveTex);
            //TODO
            //Add Textures
            surfelCompute.SetVector("_SunLightDir", -sunLight.transform.forward);
            yield return null;
            FirstBounceCell();
            yield return null;
            FirstBounceProbe();
            yield return null;
            IndirectBounce();
            yield return null;
            IndirectBounce();
            yield return null;
            IndirectBounce();
            yield return null;
            IndirectBounce();

            yield return null;
            BufferInjectToTexture(projectionTexs);
            //    TextureMipmapGenerate(backupProjectionTexs, projectionTexs);
            //    yield return null;
        }
    }
    [Range(0f, 1f)]
    public float leakTest = 0;
    private void Update()
    {
        float3 voxelLength = 1.0f / float3(voxelSize);
        float3 physicalVoxelLength = voxelLength * transform.localScale;
        sdfSampleOffset = physicalVoxelLength * leakTest;
    }

    private void OnDestroy()
    {
        //DestroyImmediate(skyOcc0);
    }
}
