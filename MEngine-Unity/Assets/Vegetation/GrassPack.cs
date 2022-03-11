using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.IO;
public unsafe class GrassPack : MonoBehaviour
{
    public struct VegetationClusterData
    {
        public float3 boundingCenter;
        public float3 boundingExtent;
        public uint instanceStartPos;
        public uint instanceCount;
    };
    public struct Grass
    {
        public float3x4 matrix;
        public uint index;
    }
    public struct Header
    {
        public double3 globalPos;
        public uint clusterCount;
        public uint instanceCount;
    };
    public List<Transform> grassPoints;
    public int3 separateCluster = int3(5, 5, 5);
    public string path = "Test.vege";
    public Dictionary<int3, NativeList<Grass>> dict = new Dictionary<int3, NativeList<Grass>>();
    public float3 startPos;
    public float3 scale;
    void PushToDict()
    {
        float3 globalPos = transform.position;
        foreach (var i in grassPoints)
        {
            float3 pos = i.position;
            float3 localPos = saturate((pos - startPos) / scale);
            int3 index = (int3)(localPos * separateCluster);
            NativeList<Grass> grassList;
            if (!dict.TryGetValue(index, out grassList))
            {
                grassList = new NativeList<Grass>(128, Allocator.Temp);
                dict.Add(index, grassList);
            }
            float3x4 matrix = new float3x4(
                i.right,
                i.up,
                i.forward,
                pos
                );
            Grass g = new Grass()
            {
                matrix = matrix,
                index = 0   //TODO: Index can be changed
            };
            grassList.Add(ref g);
        }
    }
    void OutputData(ref Header header, ref NativeList<Grass> grass, ref NativeList<VegetationClusterData> cluster)
    {
        header.globalPos = (float3)transform.position;
        header.clusterCount = (uint)dict.Count;
        header.instanceCount = (uint)grassPoints.Count;
        int counter = 0;
        foreach (var i in dict)
        {
            VegetationClusterData data;
            data.instanceStartPos = (uint)counter;
            data.instanceCount = (uint)i.Value.Length;
            counter += i.Value.Length;
            float3 minPos = float.MaxValue;
            float3 maxPos = float.MinValue;

            foreach (var g in i.Value)
            {
                grass.Add(g);
                minPos = min(minPos, g.matrix.c3);
                maxPos = max(maxPos, g.matrix.c3);
            }
            data.boundingExtent = (maxPos - minPos) * 0.5f;
            data.boundingCenter = (minPos * 0.5f + maxPos * 0.5f);
            cluster.Add(data);
        }
    }
    void Print(ref Header header, ref NativeList<Grass> grass, ref NativeList<VegetationClusterData> cluster)
    {
        byte[] arr = new byte[sizeof(Header) + sizeof(Grass) * grass.Length + sizeof(VegetationClusterData) * cluster.Length];
        byte* ptr = arr.Ptr();
        void Memcpy(void* p, int size)
        {
            UnsafeUtility.MemCpy(ptr, p, size);
            ptr += size;
        }
        Memcpy(header.Ptr(), sizeof(Header));
        Memcpy(grass.unsafePtr, grass.Length * sizeof(Grass));
        Memcpy(cluster.unsafePtr, sizeof(VegetationClusterData) * cluster.Length);
        using (FileStream fsm = new FileStream(path, FileMode.OpenOrCreate))
        {
            fsm.Write(arr, 0, arr.Length);
        }
    }
    void ClearData()
    {
        dict.Clear();
    }
    [EasyButtons.Button]
    void OutputGrass()
    {
        startPos = transform.position - transform.localScale * 0.5f;
        scale = transform.localScale;
        Header h = new Header();
        NativeList<Grass> grass = new NativeList<Grass>(128, Allocator.Temp);
        NativeList<VegetationClusterData> cluster = new NativeList<VegetationClusterData>(128, Allocator.Temp);
        PushToDict();
        OutputData(ref h, ref grass, ref cluster);
        Debug.Log(h.globalPos);
        Debug.Log(h.instanceCount);
        Debug.Log(h.clusterCount);
        Print(ref h, ref grass, ref cluster);
        ClearData();
    }
}
