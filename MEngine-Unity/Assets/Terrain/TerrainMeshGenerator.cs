using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public unsafe class TerrainMeshGenerator : MonoBehaviour
{
    [HideInInspector]
    public float holeSize;
    [HideInInspector]
    public int vertexCount = 0;
    [HideInInspector]
    public int indicesCount = 0;
    public int clusterSeparate = 5;
    [HideInInspector]
    public List<GameObject> goes = new List<GameObject>();
    [System.Serializable]
    public struct CenterChunk
    {
        public double size;
        public int chunkRate;
    }
    [System.Serializable]
    public struct HoleChunkData
    {
        public int chunkRate;
        public int chunkLayer;
    }
    public struct TriangleFilterCommand
    {
        public int startIndex;
        public int endIndex;
        public double2 minPos;
        public double2 maxPos;
    }
    public struct Point
    {
        public double2 position;
        //TODO
        //Add Local Coord Here
    }
    public struct Triangle
    {
        public Point a, b, c;
    }
    public struct long2
    {
        public long x, y;
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
        public static bool operator ==(long2 a, long2 b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(long2 a, long2 b)
        {
            return a.x != b.x && a.y != b.y;
        }
        public override bool Equals(object obj)
        {
            long2 value = (long2)obj;
            return x == value.x && y == value.y;
        }
    }
    public static void LinkPoints(
        ref NativeList<Point> morePoints,
        ref NativeList<Point> lessPoints,
        ref NativeList<Triangle> triangle)
    {
        int perPointTriangle = morePoints.Length / lessPoints.Length;
        int leftedPoints = morePoints.Length % lessPoints.Length;
        int moreCount = 0;
        int collectedPoints = 1;
        float officialRate = (float)(morePoints.Length) / (float)(lessPoints.Length);
        for (int i = 0; i < lessPoints.Length; ++i)
        {
            float currentRate = (float)(perPointTriangle + collectedPoints) / (float)(i + 1);

            int points = perPointTriangle;
            if (leftedPoints > 0)
            {
                if (currentRate < officialRate)
                {
                    points++;
                    leftedPoints--;
                }
                else if (i >= lessPoints.Length - leftedPoints)
                {
                    points++;
                    leftedPoints--;
                }
            }
            for (int j = 0; j < points; ++j)
            {
                Triangle tri;
                tri.a = morePoints[moreCount];
                tri.c = lessPoints[i];
                moreCount++;
                if (moreCount < morePoints.Length)
                {
                    tri.b = morePoints[moreCount];
                    triangle.Add(ref tri);
                }
            }
            if (i < lessPoints.Length - 1)
            {
                Triangle tri;
                tri.a = morePoints[moreCount];
                tri.c = lessPoints[i];
                tri.b = lessPoints[i + 1];
                triangle.Add(ref tri);
            }
            collectedPoints += points;
        }
    }
    private static void AddPoints(
       double chunkSize,
             double2 localStartPos,
             ref NativeList<Triangle> localTriangles)
    {
        Point a = new Point
        {
            position = localStartPos
        };
        Point b = new Point
        {
            position = localStartPos + new double2(chunkSize, 0)
        };
        Point c = new Point
        {
            position = localStartPos + new double2(0, chunkSize)
        };
        Point d = new Point
        {
            position = localStartPos + chunkSize
        };
        localTriangles.Add(
            new Triangle
            {
                a = a,
                b = c,
                c = b
            });
        localTriangles.Add(
            new Triangle
            {
                a = c,
                b = d,
                c = b
            });
    }
    public static double GenerateHoleMesh(
        double holeSize,
        int chunkCountInHole,
        int chunkLayer,
        ref NativeList<Triangle> triangles)
    {
        double chunkSize = (holeSize / chunkCountInHole) * 2;
        double layerSize = chunkSize * chunkLayer;

        double2 startPos = -holeSize - double2(layerSize, 0);
        for (int x = 0; x < chunkLayer; ++x)
            for (int y = 0; y < chunkCountInHole; ++y)
            {
                AddPoints(
                    chunkSize,
                    startPos + double2(x, y) * chunkSize,
                    ref triangles);
            }
        startPos = double2(holeSize, -holeSize);
        for (int x = 0; x < chunkLayer; ++x)
            for (int y = 0; y < chunkCountInHole; ++y)
            {
                AddPoints(
                    chunkSize,
                    startPos + double2(x, y) * chunkSize,
                    ref triangles);
            }
        startPos = double2(-holeSize, holeSize) - double2(layerSize, 0);
        for (int x = 0; x < chunkLayer * 2 + chunkCountInHole; ++x)
            for (int y = 0; y < chunkLayer; ++y)
            {
                AddPoints(
                    chunkSize,
                    startPos + double2(x, y) * chunkSize,
                    ref triangles);
            }
        startPos = -holeSize - layerSize;
        for (int x = 0; x < chunkLayer * 2 + chunkCountInHole; ++x)
            for (int y = 0; y < chunkLayer; ++y)
            {
                AddPoints(
                    chunkSize,
                    startPos + double2(x, y) * chunkSize,
                    ref triangles);
            }
        return chunkSize;
    }
    public static void GenerateQuadMesh(
        ref NativeList<Triangle> triangles,
        double size,
        int chunkCount)
    {
        double2 start = -size * 0.5;
        double perChunkSize = size / (double)chunkCount;
        for (int x = 0; x < chunkCount; ++x)
            for (int y = 0; y < chunkCount; ++y)
            {
                AddPoints(
                    perChunkSize,
                    start + double2(x * perChunkSize, y * perChunkSize),
                    ref triangles);
            }
    }
    public static void TriangleToLink(
        ref NativeList<Triangle> triangles,
        ref NativeList<Vector3> vertices,
        ref NativeList<int> indices)
    {
        Dictionary<long2, int> verticesDict = new Dictionary<long2, int>(triangles.Length * 3);
        indices.Clear();
        vertices.Clear();
        int GetIndices(long2 value, double2 coord, ref NativeList<Vector3> verticesLocal)
        {
            int localIndices = 0;
            if (verticesDict.TryGetValue(value, out localIndices))
            {
                return localIndices;
            }
            localIndices = verticesLocal.Length;
            verticesDict.Add(value, localIndices);
            verticesLocal.Add(new Vector3((float)coord.x, 0, (float)coord.y));
            return localIndices;
        }
        for (int i = 0; i < triangles.Length; ++i)
        {
            Triangle tri = triangles[i];
            long2 a, b, c;
            a.x = (long)(tri.a.position.x * 1000);
            a.y = (long)(tri.a.position.y * 1000);
            b.x = (long)(tri.b.position.x * 1000);
            b.y = (long)(tri.b.position.y * 1000);
            c.x = (long)(tri.c.position.x * 1000);
            c.y = (long)(tri.c.position.y * 1000);

            indices.Add(GetIndices(a, tri.a.position, ref vertices));
            indices.Add(GetIndices(b, tri.b.position, ref vertices));
            indices.Add(GetIndices(c, tri.c.position, ref vertices));
        }
    }
    public MeshFilter meshFilter;
    public struct MeshGenerateJob : IJob
    {
        public CenterChunk centerChunk;
        public NativeList<HoleChunkData> holeChunks;
        public NativeList<Triangle> triangles;
        public NativeList<TriangleFilterCommand> filterCommand;
        [NativeDisableUnsafePtrRestriction]
        public float* holeSizePtr;
        public int clusterCount;
        public void Execute()
        {
            double holeSize = centerChunk.size;
            double currentChunkSize = centerChunk.size / centerChunk.chunkRate;
            int lastChunkRate = centerChunk.chunkRate;
            NativeList<Point> more = new NativeList<Point>(100, Allocator.Persistent);
            NativeList<Point> less = new NativeList<Point>(100, Allocator.Persistent);
            int startIndex = triangles.Length;
            GenerateQuadMesh(ref triangles, centerChunk.size, centerChunk.chunkRate);
            int endIndex = triangles.Length;
            double2 startBoundingPos = -centerChunk.size * 0.5;
            double2 boundingSize = centerChunk.size / (double)clusterCount;
            for (int x = 0; x < clusterCount; ++x)
                for (int y = 0; y < clusterCount; ++y)
                {
                    TriangleFilterCommand fcmd = new TriangleFilterCommand
                    {
                        endIndex = endIndex,
                        startIndex = startIndex,
                        minPos = startBoundingPos + double2(x, y) * boundingSize,
                        maxPos = startBoundingPos + double2(x + 1, y + 1) * boundingSize
                    };
                    filterCommand.Add(ref fcmd);
                }
            if (holeChunks.isCreated)
            {
                for (int i = 0; i < holeChunks.Length; ++i)
                {
                    startIndex = triangles.Length;
                    var ite = holeChunks[i];
                    ite.chunkRate = min(ite.chunkRate, lastChunkRate);

                    void AddLinkerPoints(
                        ref NativeList<Point> list,
                        double2 start,
                        double2 end,
                        int linkCount)
                    {
                        start *= 0.5;
                        end *= 0.5;
                        for (int x = 0; x <= linkCount; ++x)
                        {
                            list.Add(new Point
                            {
                                position = lerp(start, end, (double)x / (double)linkCount)
                            });
                        }
                    }
                    currentChunkSize *= 2;
                    //Top Link
                    more.Clear();
                    less.Clear();
                    AddLinkerPoints(
                        ref more,
                        double2(holeSize, -holeSize),
                        double2(holeSize, holeSize),
                        lastChunkRate);
                    AddLinkerPoints(
                        ref less,
                        double2(holeSize + currentChunkSize, -holeSize - currentChunkSize),
                        double2(holeSize + currentChunkSize, holeSize + currentChunkSize),
                        ite.chunkRate);
                    LinkPoints(
                        ref more,
                        ref less,
                        ref triangles);
                    //Down Link
                    more.Clear();
                    less.Clear();
                    AddLinkerPoints(
                        ref more,
                        double2(-holeSize, -holeSize),
                        double2(holeSize, -holeSize),
                        lastChunkRate);
                    AddLinkerPoints(
                        ref less,
                        double2(-holeSize - currentChunkSize, -holeSize - currentChunkSize),
                        double2(holeSize + currentChunkSize, -holeSize - currentChunkSize),
                        ite.chunkRate);

                    LinkPoints(
                        ref more,
                        ref less,
                        ref triangles);
                    //Left Link
                    more.Clear();
                    less.Clear();
                    AddLinkerPoints(
                        ref more,
                        double2(-holeSize, holeSize),
                          double2(-holeSize, -holeSize),
                        lastChunkRate);
                    AddLinkerPoints(
                        ref less,
                        double2(-holeSize - currentChunkSize, holeSize + currentChunkSize),
                        double2(-holeSize - currentChunkSize, -holeSize - currentChunkSize),
                        ite.chunkRate);

                    LinkPoints(
                        ref more,
                        ref less,
                        ref triangles);
                    //Right Link
                    more.Clear();
                    less.Clear();
                    AddLinkerPoints(
                        ref more,
                        double2(holeSize, holeSize),
                        double2(-holeSize, holeSize),
                        lastChunkRate);
                    AddLinkerPoints(
                        ref less,
                        double2(holeSize + currentChunkSize, holeSize + currentChunkSize),
                         double2(-holeSize - currentChunkSize, holeSize + currentChunkSize),
                        ite.chunkRate);

                    LinkPoints(
                        ref more,
                        ref less,
                        ref triangles);

                    double newChunkSize = GenerateHoleMesh(
                        (holeSize + currentChunkSize) * 0.5,
                        ite.chunkRate,
                        ite.chunkLayer,
                        ref triangles);
                    endIndex = triangles.Length;

                    holeSize += newChunkSize * ite.chunkLayer * 2 + currentChunkSize;
                    lastChunkRate = ite.chunkRate + ite.chunkLayer * 2;
                    currentChunkSize = newChunkSize;

                    startBoundingPos = -holeSize * 0.5;
                    boundingSize = holeSize / (double)clusterCount;
                    for (int x = 0; x < clusterCount; ++x)
                        for (int y = 0; y < clusterCount; ++y)
                        {
                            TriangleFilterCommand fcmd = new TriangleFilterCommand
                            {
                                endIndex = endIndex,
                                startIndex = startIndex,
                                minPos = startBoundingPos + double2(x, y) * boundingSize,
                                maxPos = startBoundingPos + double2(x + 1, y + 1) * boundingSize
                            };
                            filterCommand.Add(ref fcmd);
                        }

                }
            }
            *holeSizePtr = (float)(holeSize * 0.5);
            more.Dispose();
            less.Dispose();
            holeChunks.Dispose();
        }
    }
    public struct MeshResult
    {
        public NativeList<Vector3> vertices;
        public NativeList<int> indices;
    }
    private List<MeshResult> commands;
    JobHandle handle;
    public CenterChunk centerChunk;
    public List<HoleChunkData> holeChunks;

    public struct ClusterCullJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public void* generatorPtr;
        public NativeList<TriangleFilterCommand> commands;
        public NativeList<Triangle> triangle;
        public void Execute(int index)
        {
            TerrainMeshGenerator obj = MUnsafeUtility.GetObject<TerrainMeshGenerator>(generatorPtr);
            TriangleFilterCommand cmd = commands[index];
            NativeList<Triangle> tri = new NativeList<Triangle>(1000, Allocator.Persistent);
            for (int i = cmd.startIndex; i < cmd.endIndex; ++i)
            {
                ref Triangle tr = ref triangle[i];
                double2 position = (tr.a.position + tr.b.position + tr.c.position) * 0.33333333333333333333333333333333333;
                bool2 isIn = (position >= cmd.minPos) & (position <= cmd.maxPos);
                if (isIn.x && isIn.y)
                {
                    tri.Add(ref tr);
                }
            }

            if (tri.Length > 0)
            {
                NativeList<int> indices = new NativeList<int>(tri.Length * 3, Allocator.Persistent);
                NativeList<Vector3> vertices = new NativeList<Vector3>(tri.Length * 2, Allocator.Persistent);
                TriangleToLink(
                    ref tri,
                    ref vertices,
                    ref indices);
                lock (obj)
                {
                    obj.commands.Add(new MeshResult
                    {
                        vertices = vertices,
                        indices = indices
                    });
                }
            }
            else
            {
                tri.Dispose();
            }
        }
    }


    public void Run()
    {
        commands = new List<MeshResult>();
        MeshGenerateJob generateJob = new MeshGenerateJob
        {
            centerChunk = centerChunk,
            holeSizePtr = holeSize.Ptr(),
            triangles = new NativeList<Triangle>(10000, Allocator.Persistent),
            clusterCount = clusterSeparate,
            filterCommand = new NativeList<TriangleFilterCommand>(clusterSeparate * clusterSeparate * (holeChunks.Count + 1), Allocator.Persistent)
        };
        if (holeChunks.Count > 0)
        {
            generateJob.holeChunks = new NativeList<HoleChunkData>(holeChunks.Count, Allocator.Persistent);
            foreach (var i in holeChunks)
            {
                generateJob.holeChunks.Add(i);
            }
        }
        generateJob.Execute();
        ClusterCullJob cullJob = new ClusterCullJob
        {
            commands = generateJob.filterCommand,
            generatorPtr = MUnsafeUtility.GetManagedPtr(this),
            triangle = generateJob.triangles
        };

        JobHandle handle = cullJob.Schedule(generateJob.filterCommand.Length, 1);
        handle.Complete();
        foreach (var i in goes)
        {
            if (i)
                DestroyImmediate(i);
        }
        goes.Clear();
        vertexCount = 0;
        indicesCount = 0;
        uint nameID = 0;
        foreach (var i in commands)
        {
            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            NativeArray<Vector3> vert = new NativeArray<Vector3>(i.vertices.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(vert.Ptr(), i.vertices.unsafePtr, sizeof(Vector3) * vert.Length);
            m.SetVertices<Vector3>(vert, 0, i.vertices.Length);
            vert.Dispose();
            NativeArray<int> ind = new NativeArray<int>(i.indices.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(ind.Ptr(), i.indices.unsafePtr, sizeof(int) * i.indices.Length);
            m.SetIndices<int>(ind, MeshTopology.Triangles, 0);
            ind.Dispose();
            m.RecalculateBounds();
            m.name = "TerrainMesh_" + nameID;
            nameID++;
            vertexCount += i.vertices.Length;
            indicesCount += i.indices.Length / 3;
            i.vertices.Dispose();
            i.indices.Dispose();
            GameObject newGo = Instantiate<GameObject>(meshFilter.gameObject);
            newGo.GetComponent<MeshFilter>().sharedMesh = m;
            goes.Add(newGo);
        }
        generateJob.triangles.Dispose();
        generateJob.filterCommand.Dispose();
    }
    public void Output()
    {
        MeshExporter meshExp = new MeshExporter();
        meshExp.meshes = new List<Mesh>();
        foreach (var i in goes)
        {
            meshExp.meshes.Add(i.GetComponent<MeshFilter>().sharedMesh);
        }
        meshExp.Export();
        string json = MJsonUtility.MakeJsonObject(
            MJsonUtility.GetKeyValue("Name", "TerrainMesh_", false),
            MJsonUtility.GetKeyValue("Count", goes.Count));
        using (StreamWriter sw = new StreamWriter("TerrainData.json", false, System.Text.Encoding.ASCII))
        {
            sw.Write(json);
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(TerrainMeshGenerator))]
public class TerrainMeshGenerator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TerrainMeshGenerator target = serializedObject.targetObject as TerrainMeshGenerator;
        if (GUILayout.Button("Generate"))
        {
            target.Run();
        }
        EditorGUILayout.LabelField("Hole Size: " + target.holeSize);
        EditorGUILayout.LabelField("Vertex Count: " + target.vertexCount);
        EditorGUILayout.LabelField("Triangle Count: " + target.indicesCount);
        target.clusterSeparate = max(0, target.clusterSeparate);
        EditorGUILayout.LabelField("Maximum Drawcall: " + target.goes.Count);
        if (GUILayout.Button("Output"))
        {
            target.Output();
        }
    }
}
#endif