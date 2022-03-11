using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using MPipeline;
using Unity.Jobs;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using UnityEngine.Jobs;
using System.IO;
using UnityEditor;
public unsafe class TerrainHeightEditor : MonoBehaviour
{
    struct TerrainChunkCounter
    {
        public TerrainChunk chunk;
        public double2 centerPos;
        public bool enabled;
    }
    [System.Serializable]
    public struct TerrainTexture
    {
        public Texture albedoTex;
        public Texture normalTex;
        public Texture specTex;
    }
    [Header("Output Settings: ")]
    public string outputPath = "TerrainHeightmap/";
    public string texName = "terrainTex";
    public List<double> lodDistances = new List<double>();

    [Header("Texture Chunk Settings:")]
    public Mesh quadMesh;
    public static TerrainHeightEditor current = null;
    public ComputeShader heightmapShader;
    public HeightToNormal componentObj;
    public TextureExporter texExp;
    public int clusterChunk = 2;
    public double chunkSize = 1024;
    public double heightRate = 0.1;
    public int meshDensity = 2048;
    public float cameraSpeed = 50;
    public float cameraAccelaration = 5;
    public float mouseSensity = 5;
    public Texture testHeight;
    public double avaliableDist = 10000;
    public TransformAccessArray accArray;
    public double maxHeight { get; private set; }
    private List<TerrainChunkCounter> chunks;
    private float cameraRealSpeed = 0;
    private float3 eulerAngle = 0;
    [System.NonSerialized]
    public int3 chunk = int3(0, 0, 0);
    private Material copyMat;
    private CommandBuffer cb;
    [HideInInspector]
    public int renderingChunk = 0;
    [Header("Material Textures: ")]
    public ComputeShader layerCS;
    public List<TerrainTexture> textures = new List<TerrainTexture>(20);
    private RenderTexture albedoTexArray;
    private RenderTexture normalTexArray;
    private RenderTexture specTexArray;
    private RenderTexture indexTex;
    private RenderTexture splatTex;

    private void Awake()
    {
        current = this;
        accArray = new TransformAccessArray(1000);
        maxHeight = chunkSize * clusterChunk * heightRate;
        componentObj.worldSize = chunkSize;
        componentObj.heightSize = maxHeight;
        Bounds bd = new Bounds();
        bd.center = new Vector3(0, 0.5f, 0);
        bd.extents = new Vector3(0.5f, 0.5f, 0.5f);
        quadMesh.bounds = bd;
        current = this;
        chunks = new List<TerrainChunkCounter>();
        double2 leftPos = -chunkSize * (0.5 * double2(clusterChunk, clusterChunk));
        double2 rightPos = -leftPos;
        copyMat = new Material(Shader.Find("Hidden/CopyTex"));
        cb = new CommandBuffer();

        for (int y = 0; y < clusterChunk; ++y)
            for (int x = 0; x < clusterChunk; ++x)
            {
                double2 pos = lerp(leftPos, rightPos, (double2(x, y) + 0.5) / double2(clusterChunk, clusterChunk));
                TerrainChunkCounter cc = new TerrainChunkCounter
                {
                    chunk = GetTerrainGo(int2(x, y), new Vector3((float)pos.x, 0, -(float)pos.y), new Vector3((float)chunkSize, (float)maxHeight, (float)chunkSize)).GetComponent<TerrainChunk>(),
                    enabled = false,
                    centerPos = pos * double2(1, -1)
                };
                cc.chunk.EndChunk();
                chunks.Add(cc);

            }
        albedoTexArray = new RenderTexture(new RenderTextureDescriptor
        {
            width = 2048,
            height = 2048,
            volumeDepth = max(1, textures.Count),
            enableRandomWrite = true,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2DArray,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm
        });
        normalTexArray = new RenderTexture(new RenderTextureDescriptor
        {
            width = 2048,
            height = 2048,
            volumeDepth = max(1, textures.Count),
            enableRandomWrite = true,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2DArray,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SNorm
        });
        specTexArray = new RenderTexture(new RenderTextureDescriptor
        {
            width = 2048,
            height = 2048,
            volumeDepth = max(1, textures.Count),
            enableRandomWrite = true,
            msaaSamples = 1,
            dimension = TextureDimension.Tex2DArray,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm
        });
        albedoTexArray.Create();
        normalTexArray.Create();
        specTexArray.Create();
        for (int i = 0; i < textures.Count; ++i)
        {
            Graphics.Blit(textures[i].albedoTex, albedoTexArray, 0, i);
            Graphics.Blit(textures[i].normalTex, normalTexArray, copyMat, 5, i);
            Graphics.Blit(textures[i].specTex, specTexArray, 0, i);
        }
    }
    private Mesh GetQuadMesh()
    {
        NativeList<TerrainMeshGenerator.Triangle> triangles = new NativeList<TerrainMeshGenerator.Triangle>(meshDensity * meshDensity * 2, Allocator.Temp);
        TerrainMeshGenerator.GenerateQuadMesh(ref triangles, 1, meshDensity);
        NativeList<Vector3> vertices = new NativeList<Vector3>(triangles.Length * 2, Allocator.Temp);
        NativeList<int> indices = new NativeList<int>(triangles.Length * 3, Allocator.Temp);
        TerrainMeshGenerator.TriangleToLink(ref triangles, ref vertices, ref indices);
        Mesh m = new Mesh();
        NativeArray<Vector3> arr = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
        int[] tris = new int[indices.Length];
        UnsafeUtility.MemCpy(arr.Ptr(), vertices.unsafePtr, sizeof(Vector3) * vertices.Length);
        UnsafeUtility.MemCpy(tris.Ptr(), indices.unsafePtr, sizeof(int) * indices.Length);
        Vector3[] normals = new Vector3[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        for (uint i = 0; i < normals.Length; ++i)
        {
            normals[i] = new Vector3(0, 1, 0);
            tangents[i] = new Vector4(1, 0, 0, 1);
            uvs[i] = float2(vertices[i].x, vertices[i].z);
        }
        m.indexFormat = IndexFormat.UInt32;
        m.SetVertices<Vector3>(arr);
        m.triangles = tris;
        m.tangents = tangents;
        m.normals = normals;
        m.uv = uvs;
        Bounds bd = new Bounds();
        bd.center = new Vector3(0, 0.5f, 0);
        bd.extents = new Vector3(0.5f, 0.5f, 0.5f);
        return m;
    }

    private GameObject GetTerrainGo(int2 chunkCount, Vector3 pos, Vector3 localScale)
    {
        GameObject go = new GameObject("Terrain Chunk_" + chunkCount.x + "_" + chunkCount.y, typeof(MeshFilter), typeof(MeshRenderer), typeof(TerrainChunk));
        MeshFilter filter = go.GetComponent<MeshFilter>();
        MeshRenderer rend = go.GetComponent<MeshRenderer>();
        filter.sharedMesh = quadMesh;
        rend.sharedMaterial = null;
        Transform tr = go.transform;
        tr.rotation = Quaternion.identity;
        tr.position = pos;
        tr.localScale = localScale;
        return go;
    }
    private void UpdateDecal(JobHandle handle)
    {
        CheckDecals cd = new CheckDecals
        {
            chunk = chunk
        };
        double3* offsets = stackalloc double3[]
{
            double3(0.5, 0.5, 0.5),
            double3(0.5, 0.5, -0.5),
            double3(0.5, -0.5, 0.5),
            double3(0.5, -0.5, -0.5),
            double3(-0.5, 0.5, 0.5),
            double3(-0.5, 0.5, -0.5),
            double3(-0.5, -0.5, 0.5),
            double3(-0.5, -0.5, -0.5)
        };
        foreach (var d in TerrainDecal.updateDecalList)
        {
            if (d.enabled)
            {
                double3 minValue, maxValue;
                double4 worldPos = mul(d.localToWorldMatrix, double4(offsets[0], 1));
                minValue = worldPos.xyz;
                maxValue = worldPos.xyz;
                for (uint a = 1; a < 8; ++a)
                {
                    worldPos = mul(d.localToWorldMatrix, double4(offsets[a], 1));
                    minValue = min(minValue, worldPos.xyz);
                    maxValue = max(maxValue, worldPos.xyz);
                }
                cd.minValue = minValue;
                cd.maxValue = maxValue;
                cd.terrainDecal = MUnsafeUtility.GetManagedPtr(d);
                handle = cd.Schedule(chunks.Count, max(1, chunks.Count / 16));
                SetLastPos slp = new SetLastPos
                {
                    terrainDecal = cd.terrainDecal,
                    maxValue = cd.maxValue,
                    minValue = cd.minValue
                };
                handle = slp.Schedule(handle);
            }
        }
        handle.Complete();
        TerrainDecal.updateDecalList.Clear();
    }

    public void ExporteAllTexture()
    {
        int mipLevel = lodDistances.Count;
        TerrainDecal.updateDecalList.Clear();
        TerrainDecal.updateDecalList.AddRange(TerrainDecal.decalList);
        UpdateDecal(default);
        double2 camPos = double2(transform.position.x, transform.position.z) + double2(chunk.x, chunk.z) * 100;
        double2 leftPos = -chunkSize * (0.5 * double2(clusterChunk, clusterChunk));
        double2 rightPos = -leftPos;
        int i = 0;
        Texture[] heightTexs = new Texture[mipLevel];
        Texture[] normalTexs = new Texture[mipLevel];

        for (int y = 0; y < clusterChunk; ++y)
            for (int x = 0; x < clusterChunk; ++x)
            {
                var a = chunks[i];
                if (!a.enabled)
                {
                    double2 pos = lerp(leftPos, rightPos, (double2(x, y) + 0.5) / double2(clusterChunk, clusterChunk));
                    a.chunk.BeginChunk(pos, double3(chunkSize, (float)maxHeight, chunkSize), new Vector2(x, y), new Vector2(clusterChunk, clusterChunk));
                    a.chunk.UpdatePhysicsPosition(chunk);
                }
                a.chunk.UpdateChunk(testHeight, componentObj, copyMat, heightmapShader, cb);
                int2 res = int2(a.chunk.heightRT.width, a.chunk.heightRT.height);
                RenderTexture lastH = a.chunk.heightRT, lastN = a.chunk.normalTex;
                for (int j = 0; j < mipLevel; ++j)
                {
                    RenderTexture heightT = RenderTexture.GetTemporary(res.x, res.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm);
                    RenderTexture normalT = RenderTexture.GetTemporary(res.x, res.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_UNorm);
                    res /= 2;
                    cb.Blit(lastH, heightT);
                    cb.Blit(lastN, normalT);
                    lastH = heightT;
                    lastN = normalT;
                    heightTexs[j] = heightT;
                    normalTexs[j] = normalT;
                }
                Graphics.ExecuteCommandBuffer(cb);
                cb.Clear();
                TerrainHeightExporter.OutputAllTex(new TerrainTex
                {
                    heightTexs = heightTexs,
                    normalTexs = normalTexs
                }, int2(x, clusterChunk - y - 1), outputPath, texName, texExp);
                for (int j = 0; j < mipLevel; ++j)
                {
                    RenderTexture.ReleaseTemporary(heightTexs[j] as RenderTexture);
                    RenderTexture.ReleaseTemporary(normalTexs[j] as RenderTexture);
                }

                if (!a.enabled)
                {
                    a.chunk.EndChunk();
                }
                chunks[i] = a;
                i++;
            }
    }
    public void ExportJson()
    {
        double height = chunkSize * 0.5 * clusterChunk * heightRate;
        string jsonStr = TerrainHeightExporter.GetTerrainJson(
            0,
            chunkSize * clusterChunk,
            double2(-height, height),
            clusterChunk, lodDistances, texName);
        using (StreamWriter fsm = new StreamWriter(outputPath + "Data.json"))
        {
            fsm.Write(jsonStr);
        }
    }
    private void Update()
    {

        //Camera Control
        if (Input.GetMouseButton(1))
        {
            float2 mouseMovement = float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensity;
            eulerAngle.y += mouseMovement.x;
            eulerAngle.x -= mouseMovement.y;
            eulerAngle.x = clamp(eulerAngle.x, -85, 85);
            transform.localEulerAngles = eulerAngle;

            float3 moveDir = 0;
            if (Input.GetKey(KeyCode.W))
            {
                moveDir.z += 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveDir.z -= 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                moveDir.x -= 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveDir.x += 1;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                moveDir.y -= 1;
            }
            if (Input.GetKey(KeyCode.E))
            {
                moveDir.y += 1;
            }
            if (lengthsq(moveDir) < 0.01) cameraRealSpeed = cameraSpeed;
            else cameraRealSpeed += cameraAccelaration * Time.deltaTime;
            float4x4 camMat = transform.localToWorldMatrix;
            float3 nextLocalPos = moveDir * Time.deltaTime * cameraRealSpeed * (Input.GetKey(KeyCode.LeftShift) ? 2 : 1);
            transform.position = mul(camMat, float4(nextLocalPos, 1)).xyz;
        }
        int3 moveChunk = 0;
        void MoveChunk(float axis, ref int chunk)
        {
            if (axis > 100)
                chunk += 1;
            else if (axis < -100)
                chunk -= 1;
        }
        MoveChunk(transform.position.x, ref moveChunk.x);
        MoveChunk(transform.position.y, ref moveChunk.y);
        MoveChunk(transform.position.z, ref moveChunk.z);
        JobHandle handle = default;
        if (dot(abs(moveChunk), 1) > 0.01)
        {
            transform.position -= (Vector3)((float3)(moveChunk * 100));
            chunk += moveChunk;
            int renderingChunk = 0;
            foreach (var a in chunks)
            {
                if (a.enabled)
                {
                    a.chunk.UpdatePhysicsPosition(chunk);
                    renderingChunk++;
                }
            }
            this.renderingChunk = renderingChunk;
            MoveDecals md = new MoveDecals
            {
                chunk = chunk
            };
            handle = md.Schedule(accArray, handle);
        }
        double2 camPos = double2(transform.position.x, transform.position.z) + double2(chunk.x, chunk.z) * 100;
        double2 leftPos = -chunkSize * (0.5 * double2(clusterChunk, clusterChunk));
        double2 rightPos = -leftPos;
        UpdateDecal(handle);
        int i = 0;
        for (int y = 0; y < clusterChunk; ++y)
            for (int x = 0; x < clusterChunk; ++x)
            {
                var a = chunks[i];
                bool enable = distancesq(a.centerPos, camPos) < (avaliableDist * avaliableDist);
                if (enable != a.enabled)
                {
                    a.enabled = enable;
                    if (enable)
                    {
                        double2 pos = lerp(leftPos, rightPos, (double2(x, y) + 0.5) / double2(clusterChunk, clusterChunk));
                        a.chunk.BeginChunk(pos, double3(chunkSize, (float)maxHeight, chunkSize), new Vector2(x, y), new Vector2(clusterChunk, clusterChunk));
                        a.chunk.UpdatePhysicsPosition(chunk);
                        a.chunk.UpdateChunk(testHeight, componentObj, copyMat, heightmapShader, cb);
                        renderingChunk++;
                    }
                    else
                    {
                        a.chunk.EndChunk();
                        renderingChunk--;
                    }
                }
                else if (enable)
                {
                    if (a.chunk.block.Count > 0)

                        a.chunk.UpdateChunk(testHeight, componentObj, copyMat, heightmapShader, cb);

                }
                chunks[i] = a;
                i++;
            }
        cb.SetGlobalTexture("_AlbedoTextureArray", albedoTexArray);
        cb.SetGlobalTexture("_NormalTextureArray", normalTexArray);
        cb.SetGlobalTexture("_SpecTextureArray", specTexArray);
        Graphics.ExecuteCommandBuffer(cb);
        cb.Clear();
    }
    public struct MoveDecals : IJobParallelForTransform
    {
        public double3 chunk;
        public void Execute(int index, TransformAccess acc)
        {
            acc.position = (float3)(TerrainDecal.decalList[index].absolutePos - chunk * 100.0);
        }
    }
    public static bool BoxContactWithBox(double3 min0, double3 max0, double3 min1, double3 max1)
    {
        bool vx, vy, vz;
        vx = min0.x > max1.x;
        vy = min0.y > max1.y;
        vz = min0.z > max1.z;
        if (vx || vy || vz) return false;
        vx = min1.x > max0.x;
        vy = min1.y > max0.y;
        vz = min1.z > max0.z;
        if (vx || vy || vz) return false;
        return true;
    }
    public struct SetLastPos : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public void* terrainDecal;
        public double3 minValue, maxValue;
        public void Execute()
        {
            TerrainDecal tDecal = MUnsafeUtility.GetObject<TerrainDecal>(terrainDecal);
            tDecal.lastMin = minValue;
            tDecal.lastMax = maxValue;
        }
    }
    public struct CheckDecals : IJobParallelFor
    {
        public double3 chunk;
        [NativeDisableUnsafePtrRestriction]
        public void* terrainDecal;
        public double3 minValue, maxValue;
        public void Execute(int index)
        {
            var chunk = current.chunks[index];
            if (!chunk.enabled) return;

            double2 size = current.chunkSize * 0.5;

            double2 maxXZ = chunk.centerPos + size;
            double2 minXZ = chunk.centerPos - size;
            TerrainDecal tDecal = MUnsafeUtility.GetObject<TerrainDecal>(terrainDecal);
            if (BoxContactWithBox(double3(minXZ.x, 0, minXZ.y), double3(maxXZ.x, current.maxHeight, maxXZ.y), minValue, maxValue) ||
            BoxContactWithBox(double3(minXZ.x, 0, minXZ.y), double3(maxXZ.x, current.maxHeight, maxXZ.y), tDecal.lastMin, tDecal.lastMax))
            {
                chunk.chunk.block.Add(tDecal.GetData());
            }
        }
    }
}

[CustomEditor(typeof(TerrainHeightEditor))]
public class TerrainHeightEditorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TerrainHeightEditor obj = serializedObject.targetObject as TerrainHeightEditor;
        EditorGUILayout.LabelField("Rendering Chunk: " + obj.renderingChunk);
        ulong size = (2048 * 2048 * (4 + 2) );
        size *= (ulong)obj.renderingChunk;
        size += (2048 * 2048 * 12) * (ulong)max(1,obj.textures.Count);
        EditorGUILayout.LabelField("GPU Memory Cost: " + (size / 1024 / 1024) + "M");
        if (GUILayout.Button("Output") && Application.isPlaying)
        {
            if (obj.lodDistances.Count < 1)
            {
                obj.lodDistances.Add(1024);
            }
            for (int i = 1; i < obj.lodDistances.Count; ++i)
            {
                obj.lodDistances[i] = max(obj.lodDistances[i - 1] + 0.1, obj.lodDistances[i]);
            }
            obj.ExporteAllTexture();
            obj.ExportJson();
        }
    }
}