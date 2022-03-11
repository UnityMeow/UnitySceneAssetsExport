using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MPipeline;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;
public struct OrthoCam
{
    public float4x4 worldToCameraMatrix;
    public float4x4 localToWorldMatrix;
    public float3 right;
    public float3 up;
    public float3 forward;
    public float3 position;
    public float size;
    public float nearClipPlane;
    public float farClipPlane;
    public float4x4 projectionMatrix;
    public void UpdateTRSMatrix()
    {
        localToWorldMatrix.c0 = new float4(right, 0);
        localToWorldMatrix.c1 = new float4(up, 0);
        localToWorldMatrix.c2 = new float4(forward, 0);
        localToWorldMatrix.c3 = new float4(position, 1);
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorldMatrix);
        worldToCameraMatrix.c0.z = -worldToCameraMatrix.c0.z;
        worldToCameraMatrix.c1.z = -worldToCameraMatrix.c1.z;
        worldToCameraMatrix.c2.z = -worldToCameraMatrix.c2.z;
        worldToCameraMatrix.c3.z = -worldToCameraMatrix.c3.z;
    }
    public void UpdateProjectionMatrix()
    {
        projectionMatrix = Matrix4x4.Ortho(-size, size, -size, size, nearClipPlane, farClipPlane);
    }
}
public struct PerspCam
{
    public float3 right;
    public float3 up;
    public float3 forward;
    public float3 position;
    public float fov;
    public float nearClipPlane;
    public float farClipPlane;
    public float aspect;
    public float4x4 localToWorldMatrix;
    public float4x4 worldToCameraMatrix;
    public float4x4 projectionMatrix;
    public void UpdateTRSMatrix()
    {
        localToWorldMatrix.c0 = float4(right, 0);
        localToWorldMatrix.c1 = float4(up, 0);
        localToWorldMatrix.c2 = float4(forward, 0);
        localToWorldMatrix.c3 = float4(position, 1);
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorldMatrix);
        float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
        worldToCameraMatrix.c0.z = row2.x;
        worldToCameraMatrix.c1.z = row2.y;
        worldToCameraMatrix.c2.z = row2.z;
        worldToCameraMatrix.c3.z = row2.w;
    }
    public void UpdateViewMatrix(float4x4 localToWorld)
    {
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorld);
        right = localToWorld.c0.xyz;
        up = localToWorld.c1.xyz;
        forward = localToWorld.c2.xyz;
        position = localToWorld.c3.xyz;
        float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
        worldToCameraMatrix.c0.z = row2.x;
        worldToCameraMatrix.c1.z = row2.y;
        worldToCameraMatrix.c2.z = row2.z;
        worldToCameraMatrix.c3.z = row2.w;
    }
    public void UpdateProjectionMatrix()
    {
        projectionMatrix = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
    }
}
public unsafe class DistanceToCam : MonoBehaviour
{
    public struct RendererMaterialData
    {
        public Texture albedoTex;
        public float4 uvTileOffset;
        public Color albedoColor;
    }
    public struct GeometryData
    {
        public float avaliable;
        public float3 position;
        public float3 normal;
        public float3 albedo;
    };
    public const int SIZE = 512;
    public const int SURFEL_COUNT = 512;
    public const float nearClip = 0.1f;
    public const float farClip = 100f;
    //  public RenderTexture cubemap;
    public Shader shader;
    public ComputeShader surfelshader;
    public ComputeShader runtimeDispatchShader;
    private RenderTexture depthCubemap;
    private RenderTexture albedoDepthCubemap;
    private RenderTexture normalCubemap;
    private float4x4[] cubemapFacesVP;
    private Material mat;
    private List<MeshFilter> meshFilteres;
    private RendererMaterialData[] objMatDatas;
    private NativeList<float4x4> matrices;
    private float4x4 proj;
    private Vector4[] samplePoints;

    public Transform allGeometryParent;

    public void MoveTheWorld(float3 poses)
    {
        foreach(var i in meshFilteres)
        {
            i.transform.position += (Vector3)poses;
        }
    }
    void CollectMeshFilteres(Transform tr)
    {
        MeshFilter fl = tr.GetComponent<MeshFilter>();
        if(fl)
        {
            meshFilteres.Add(fl);
        }
        int c = tr.childCount;
        for(int i = 0; i < c; ++i)
        {
            CollectMeshFilteres(tr.GetChild(i));
        }
    }
    public void Init(float3 originPos)
    {
        mat = new Material(shader);
        meshFilteres = new List<MeshFilter>();
        CollectMeshFilteres(allGeometryParent);
        MoveTheWorld(originPos);
        depthCubemap = new RenderTexture(new RenderTextureDescriptor
        {
            width = SIZE,
            height = SIZE,
            volumeDepth = 1,
            dimension = UnityEngine.Rendering.TextureDimension.Cube,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm,
            msaaSamples = 1,
            depthBufferBits = 16
        });
        depthCubemap.Create();
        depthCubemap.filterMode = FilterMode.Point;
        albedoDepthCubemap = new RenderTexture(new RenderTextureDescriptor
        {
            width = SIZE,
            height = SIZE,
            volumeDepth = 1,
            dimension = UnityEngine.Rendering.TextureDimension.Cube,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
            msaaSamples = 1,
            depthBufferBits = 16
        });
        albedoDepthCubemap.filterMode = FilterMode.Point;
        normalCubemap = new RenderTexture(new RenderTextureDescriptor
        {
            width = SIZE,
            height = SIZE,
            volumeDepth = 1,
            dimension = UnityEngine.Rendering.TextureDimension.Cube,
            colorFormat = RenderTextureFormat.ARGB2101010,
            sRGB = false,
            msaaSamples = 1,
            depthBufferBits = 16
        });
        normalCubemap.filterMode = FilterMode.Point;
        albedoDepthCubemap.Create();
        normalCubemap.Create();
        objMatDatas = new RendererMaterialData[meshFilteres.Count];
        for (uint i = 0; i < objMatDatas.Length; ++i)
        {
            ref var o = ref objMatDatas[i];
            Material mat = meshFilteres[(int)i].GetComponent<MeshRenderer>().sharedMaterial;
            o.albedoTex = mat.GetTexture("_MainTex");
            o.albedoColor = mat.GetColor("_Color");
            o.uvTileOffset = mat.GetVector("_TileOffset");
        }
        CullLib.Initialize(90, 1, nearClip, farClip);
        matrices = new NativeList<float4x4>(meshFilteres.Count, meshFilteres.Count, Unity.Collections.Allocator.Persistent);
        CullLib.ClearObjects();
        for (int i = 0; i < matrices.Length; ++i)
        {
            Bounds bd = meshFilteres[i].sharedMesh.bounds;
            Transform tr = meshFilteres[i].transform;
            matrices[i] = tr.localToWorldMatrix;
            CullLib.PushObject(tr.right, tr.up, tr.forward, tr.localScale, tr.position, bd.center, bd.extents);
        }
        proj = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(90, 1, nearClip, farClip), false);
        samplePoints = new Vector4[]
        {
            new Vector4(-1, -1, -1),
            new Vector4(-1, -1, 1),
            new Vector4(-1, 1, -1),
            new Vector4(-1, 1, 1),

            new Vector4(1, -1, -1),
            new Vector4(1, -1, 1),
            new Vector4(1, 1, -1),
            new Vector4(1, 1, 1),

            new Vector4(-1, -1, 1),
            new Vector4(1, -1, 1),
            new Vector4(-1, 1, 1),
            new Vector4(1, 1, 1),

            new Vector4(-1, -1, -1),
            new Vector4(1, -1, -1),
            new Vector4(-1, 1, -1),
            new Vector4(1, 1, -1),

            new Vector4(-1, 1, 1),
            new Vector4(1, 1, 1),
            new Vector4(-1, 1, -1),
            new Vector4(1, 1, -1),

            new Vector4(-1, -1, 1),
            new Vector4(1, -1, 1),
            new Vector4(-1, -1, -1),
            new Vector4(1, -1, -1)
        };
    }
    public void Dispose()
    {
        DestroyImmediate(depthCubemap);
        CullLib.Dispose();
        matrices.Dispose();
    }
    static int _VP = Shader.PropertyToID("_VP");
    static int _Color = Shader.PropertyToID("_Color");
    static int _MainTex = Shader.PropertyToID("_MainTex");
    static int _MetallicIntensity = Shader.PropertyToID("_MetallicIntensity");
    static int _BumpMap = Shader.PropertyToID("_BumpMap");
    static int _SpecularMap = Shader.PropertyToID("_SpecularMap");
    static int _TileOffset = Shader.PropertyToID("_TileOffset");
    static int _TexWeight = Shader.PropertyToID("_TexWeight");
    static int _UVScaleOffset = Shader.PropertyToID("_UVScaleOffset");
    static int _AlbedoValue = Shader.PropertyToID("_AlbedoValue");
    static int _MetallicValue = Shader.PropertyToID("_MetallicValue");
    static int _CamPos = Shader.PropertyToID("_CamPos");
    static int _DepthTex = Shader.PropertyToID("_DepthTex");

    static int _CellStruct = Shader.PropertyToID("_CellStruct");
    static int _Offset = Shader.PropertyToID("_Offset");
    static int _SamplePoints = Shader.PropertyToID("_SamplePoints");
    static int _Count = Shader.PropertyToID("_Count");
    static int _SampleCache = Shader.PropertyToID("_SampleCache");
    static int _Random = Shader.PropertyToID("_Random");
    static int _SampleResults = Shader.PropertyToID("_SampleResults");
    static int _AlbedoDepthTex = Shader.PropertyToID("_AlbedoDepthTex");
    static int _NormalTex = Shader.PropertyToID("_NormalTex");
    static int _SurfelSamplesBuffer = Shader.PropertyToID("_SurfelSamplesBuffer");

    public void DrawAll(float3 startPos, float3 endPos, float3* probePositions, int probePositionsCount, ref ComputeBuffer surfelDataBuffer)
    {
        // ComputeBuffer sampleCache = new ComputeBuffer(SIZE * 6 + 6, sizeof(float) * 9);
        //skyOcclusionData = new ComputeBuffer((int)(size.x * size.y * size.z), sizeof(float) * 9);
        surfelDataBuffer = new ComputeBuffer(probePositionsCount * SURFEL_COUNT, sizeof(GeometryData));
        bool executeLast = false;
        float3 lastPosition = new float3();
        uint** dataPtrs = stackalloc uint*[6];
        uint* dataLength = stackalloc uint[6];
        long* offsets = stackalloc long[6];
        NativeList<uint> allCullResults = new NativeList<uint>(100, Allocator.Temp);

        void CopyDataFromLast()
        {
            if (!executeLast) return;

            long currentSize = 0;
            CullLib.Wait();
            for (uint i = 0; i < 6; ++i)
            {
                CullLib.GetCullResult(i, dataPtrs + i, dataLength + i);
                offsets[i] = currentSize;
                currentSize += (int)dataLength[i];
            }
            allCullResults.Clear();
            allCullResults.AddRange((int)currentSize);
            for (uint i = 0; i < 6; ++i)
            {
                UnsafeUtility.MemCpy(allCullResults.unsafePtr + offsets[i], dataPtrs[i], dataLength[i] * sizeof(uint));
            }
        }
        float4x4* viewMatrices = stackalloc float4x4[6];
        CubemapFace* faces = stackalloc CubemapFace[]
        {
            CubemapFace.NegativeX,
            CubemapFace.PositiveX,
            CubemapFace.NegativeY,
            CubemapFace.PositiveY,
            CubemapFace.NegativeZ,
            CubemapFace.PositiveZ
        };
        void DrawObject(int pass)
        {
            CullLib.GetCubemapViewMarices(lastPosition, viewMatrices);
            Shader.SetGlobalVector(_CamPos, float4(lastPosition, 1));
            for (uint i = 0; i < 6; ++i)
            {
                float4x4 viewProj = mul(proj, viewMatrices[i]);
                Shader.SetGlobalMatrix(_VP, viewProj);
                Graphics.SetRenderTarget(depthCubemap, 0, faces[i]);
                GL.Clear(true, true, Color.white);
                mat.SetPass(pass);
                for (uint j = 0; j < dataLength[i]; ++j)
                {
                    int index = (int)allCullResults[(uint)offsets[i] + j];
                    Graphics.DrawMeshNow(meshFilteres[index].sharedMesh, matrices[index]);
                }
            }
        }

        void DrawObject_GBuffer()
        {
            CullLib.GetCubemapViewMarices(lastPosition, viewMatrices);
            Shader.SetGlobalVector(_CamPos, float4(lastPosition, 1));
            for (uint i = 0; i < 6; ++i)
            {
                float4x4 viewProj = mul(proj, viewMatrices[i]);
                Shader.SetGlobalMatrix(_VP, viewProj);
                Graphics.SetRenderTarget(albedoDepthCubemap, 0, faces[i]);
                GL.Clear(true, true, new Color(1000, 1000, 1000, 1000));

                for (uint j = 0; j < dataLength[i]; ++j)
                {
                    uint index = allCullResults[(uint)offsets[i] + j];
                    ref var matObj = ref objMatDatas[index];
                    Shader.SetGlobalVector(_TileOffset, matObj.uvTileOffset);
                    Shader.SetGlobalTexture(_MainTex, matObj.albedoTex);
                    Color c = matObj.albedoColor;
                    c.a = matObj.albedoTex ? 1 : 0;
                    Shader.SetGlobalColor(_Color, c);
                    mat.SetPass(2);
                    Graphics.DrawMeshNow(meshFilteres[(int)index].sharedMesh, matrices[index]);
                }
            }
            for (uint i = 0; i < 6; ++i)
            {
                float4x4 viewProj = mul(proj, viewMatrices[i]);
                Shader.SetGlobalMatrix(_VP, viewProj);
                Graphics.SetRenderTarget(normalCubemap, 0, faces[i]);
                GL.Clear(true, true, Color.white);
                mat.SetPass(3);
                for (uint j = 0; j < dataLength[i]; ++j)
                {
                    uint index = allCullResults[(uint)offsets[i] + j];
                    Graphics.DrawMeshNow(meshFilteres[(int)index].sharedMesh, matrices[index]);
                }
            }
        }
        int count = 0;
        /*  void ExecuteLast(ComputeBuffer skyResult, int pass)
          {
              if (!executeLast) return;
              executeLast = false;
              DrawObject(pass);
              surfelshader.SetInt(_Count, count);
              surfelshader.SetBuffer(0, _SampleCache, sampleCache);
              surfelshader.SetBuffer(1, _SampleCache, sampleCache);
              surfelshader.SetBuffer(2, _SampleCache, sampleCache);
              surfelshader.SetBuffer(2, _SampleResults, skyResult);
              surfelshader.SetTexture(0, _DepthTex, depthCubemap);
              surfelshader.Dispatch(0, 1, SIZE, 6);
              surfelshader.Dispatch(1, 1, 6, 1);
              surfelshader.Dispatch(2, 1, 1, 1);
              count++;
          }
        */
        void ExecuteLast_GBuffer(ComputeBuffer surfelData)
        {
            if (!executeLast) return;
            executeLast = false;
            DrawObject_GBuffer();
            surfelshader.SetInt(_Count, count * SURFEL_COUNT);
            surfelshader.SetTexture(3, _AlbedoDepthTex, albedoDepthCubemap);
            surfelshader.SetTexture(3, _NormalTex, normalCubemap);
            surfelshader.SetBuffer(3, _SurfelSamplesBuffer, surfelData);
            surfelshader.Dispatch(3, SURFEL_COUNT / 64, 1, 1);
            count++;
        }
        /*  var id = new RenderTextureDescriptor
         {
             width = (int)size.x,
             height = (int)size.y,
             volumeDepth = (int)size.z,
             dimension = TextureDimension.Tex3D,
             graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
             enableRandomWrite = true,
             msaaSamples = 1
         };
        RenderTexture skyOcc0;
         RenderTexture skyOcc1;
         RenderTexture skyOcc2;
         skyOcc0 = new RenderTexture(id);
         skyOcc1 = new RenderTexture(id);
         id.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
         skyOcc2 = new RenderTexture(id);
         skyOcc0.Create();
         skyOcc1.Create();
         skyOcc2.Create();

         count = 0;
         for (int z = 0; z < size.z; ++z)
             for (int y = 0; y < size.y; ++y)
                 for (int x = 0; x < size.x; ++x)
                 {
                     CopyDataFromLast();
                     float3 pos = lerp(startPos, endPos, (0.5f + float3(x, y, z)) / size);
                     CullLib.StartNextCull(pos);
                     ExecuteLast(skyOcclusionData, 0);
                     executeLast = true;
                     lastIndex = int3(x, y, z);
                     lastPosition = pos;

                 }
         ExecuteLast(skyOcclusionData, 0);
         Shader.SetGlobalTexture("_SkyOcclusion0", skyOcc0);
         Shader.SetGlobalTexture("_SkyOcclusion1", skyOcc1);
         Shader.SetGlobalTexture("_SkyOcclusion2", skyOcc2);
         Shader.SetGlobalVector("_CubeStartPos", transform.position - 0.5f * transform.localScale);
         Shader.SetGlobalVector("_CubeSize", transform.localScale);
         runtimeDispatchShader.SetBuffer(0, "_DataBuffer", skyOcclusionData);
         runtimeDispatchShader.SetTexture(0, "_TargetTex0", skyOcc0);
         runtimeDispatchShader.SetTexture(0, "_TargetTex1", skyOcc1);
         runtimeDispatchShader.SetTexture(0, "_TargetTex2", skyOcc2);
         runtimeDispatchShader.SetVector("_ResolutionFloat", float4((float3)size + 0.5f, 1));
         runtimeDispatchShader.Dispatch(0, (int)size.x / 4, (int)size.y / 4, (int)size.z / 4);
         count = 0;
         for (int z = 0; z < size.z; ++z)
             for (int y = 0; y < size.y; ++y)
                 for (int x = 0; x < size.x; ++x)
                 {
                     CopyDataFromLast();
                     float3 pos = lerp(startPos, endPos, (0.5f + float3(x, y, z)) / size);
                     CullLib.StartNextCull(pos);
                     ExecuteLast(skyOcclusionData, 1);
                     executeLast = true;
                     lastIndex = int3(x, y, z);
                     lastPosition = pos;

                 }
         ExecuteLast(skyOcclusionData, 1);
         */
        count = 0;
        for(int i = 0; i < probePositionsCount; ++i)
        {
            var pos = probePositions[i];
            CopyDataFromLast();
            CullLib.StartNextCull(pos);
            ExecuteLast_GBuffer(surfelDataBuffer);
            executeLast = true;
            lastPosition = pos;
        }
        ExecuteLast_GBuffer(surfelDataBuffer);
        //Shader.SetGlobalVector("_LeftPos", mul(transform.localToWorldMatrix, float4(-0.5f, -0.5f, -0.5f, 1)));
        // Shader.SetGlobalVector("_RightPos", mul(transform.localToWorldMatrix, float4(0.5f, 0.5f, 0.5f, 1)));
        //sampleCache.Dispose();
    }

}
