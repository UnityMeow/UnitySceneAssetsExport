using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;

enum MeshDataType
{
    MeshDataType_Vertex = 0,
    MeshDataType_Index = 1,
    MeshDataType_Normal = 2,
    MeshDataType_Tangent = 3,
    MeshDataType_UV = 4,
    MeshDataType_UV2 = 5,
    MeshDataType_UV3 = 6,
    MeshDataType_UV4 = 7,
    MeshDataType_Color = 8,
    MeshDataType_BoneIndex = 9,
    MeshDataType_BoneWeight = 10,
    MeshDataType_BoundingBox = 11,
    MeshDataType_BindPoses = 12,
    MeshDataType_SubMeshes = 13,
    MeshDataType_Num = 14
};
struct IndexSettings
{
    public enum IndexFormat
    {
        IndexFormat_16Bit = 0,
        IndexFormat_32Bit = 1
    };
    public IndexFormat indexFormat;
    public uint indexCount;
};
struct SubMesh
{
    public float3 boundingCenter;
    public float3 boundingExtent;
    public uint materialIndex;
    public uint vertexOffset;
    public uint indexOffset;
    public uint indexCount;
};

public unsafe class MeshExporter
{
    public List<Mesh> meshes;
    public static bool useNormal = true;
    public static bool useTangent = true;
    public static bool useUV = true;
    public static bool useVertexColor = true;
    public static bool useBone = true;

    public void Export()
    {
        List<string> str = new List<string>(meshes.Count);
        foreach (var i in meshes)
        {
            str.Add(i.name);
        }
        ExportAllMeshes(meshes, str);
    }

    /// <summary>
    /// 多个mesh导出
    /// </summary>
    public void ExportAllMeshes(List<Mesh> meshes, List<string> names)
    {
        for (int i = 0; i < meshes.Count; ++i)
        {
            ExportMesh(meshes[i], names[i]);
        }
    }

    /// <summary>
    /// 单个Mesh导出
    /// </summary>
    public void ExportMesh(Mesh mesh, string path)
    {
        List<byte> bytes = new List<byte>(1000);
        void InputInteger(uint inte)
        {
            byte* ptr = (byte*)inte.Ptr();
            bytes.Add(ptr[0]);
            bytes.Add(ptr[1]);
            bytes.Add(ptr[2]);
            bytes.Add(ptr[3]);
        }

        void InputHeader(uint3 header1)
        {
            InputInteger(header1.x);
            InputInteger(header1.y);
            InputInteger(header1.z);
        }

        void InputFloat(float flt)
        {
            byte* ptr = (byte*)flt.Ptr();
            bytes.Add(ptr[0]);
            bytes.Add(ptr[1]);
            bytes.Add(ptr[2]);
            bytes.Add(ptr[3]);
        }

        void InputVec2(Vector2 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            //InputFloat(vec.z);
        }

        void InputVec3(Vector3 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            InputFloat(vec.z);
        }

        void InputVec4(Vector4 vec)
        {
            InputFloat(vec.x);
            InputFloat(vec.y);
            InputFloat(vec.z);
            InputFloat(vec.w);
        }

        List<Vector3> vec3List = new List<Vector3>();
        List<Vector4> vec4List = new List<Vector4>();
        List<Vector2> vec2List = new List<Vector2>();
        void InputVec2List(MeshDataType type)
        {
            uint3 header1 = 0;
            header1.x = (uint)type;
            header1.y = (uint)vec2List.Count;
            InputHeader(header1);
            foreach (var j in vec2List)
            {
                InputVec2(j);
            }
        }
        void InputMatrix(Matrix4x4 mat)
        {
            byte* ptr = (byte*)mat.Ptr();
            for (int ii = 0; ii < sizeof(Matrix4x4); ++ii)
            {
                bytes.Add(ptr[ii]);
            }
        }
        void InputVec3List(MeshDataType type)
        {
            uint3 header1 = 0;
            header1.x = (uint)type;
            header1.y = (uint)vec3List.Count;
            InputHeader(header1);
            foreach (var j in vec3List)
            {
                InputVec3(j);
            }
        }
        void InputVec4List(MeshDataType type)
        {
            uint3 header1 = 0;
            header1.x = (uint)type;
            header1.y = (uint)vec4List.Count;
            InputHeader(header1);
            foreach (var j in vec4List)
            {
                InputVec4(j);
            }
        }

        var i = mesh;
        uint typeNum = 0;
        bytes.Clear();
        uint3 h = 0;
        if (useBone)
        {
            BoneWeight[] weight = mesh.boneWeights;
            if (weight.Length > 0)
            {
                typeNum++;
                h.x = (uint)MeshDataType.MeshDataType_BoneIndex;
                h.y = (uint)weight.Length;
                InputHeader(h);
                foreach (var j in weight)
                {
                    InputInteger((uint)j.boneIndex0);
                    InputInteger((uint)j.boneIndex1);
                    InputInteger((uint)j.boneIndex2);
                    InputInteger((uint)j.boneIndex3);
                }
                typeNum++;
                h.x = (uint)MeshDataType.MeshDataType_BoneWeight;
                InputHeader(h);
                foreach (var j in weight)
                {
                    InputFloat(j.weight0);
                    InputFloat(j.weight1);
                    InputFloat(j.weight2);
                    InputFloat(j.weight3);
                }
            }
            Matrix4x4[] bindPoses = mesh.bindposes;
            if (bindPoses.Length > 0)
            {
                typeNum++;
                h.x = (uint)MeshDataType.MeshDataType_BindPoses;
                h.y = (uint)bindPoses.Length;
                InputHeader(h);
                foreach (var j in bindPoses)
                {
                    InputMatrix(j);
                }
            }
        }
        typeNum++;
        h.x = (uint)MeshDataType.MeshDataType_BoundingBox;
        h.y = 0;
        InputHeader(h);
        InputVec3(mesh.bounds.center);
        InputVec3(mesh.bounds.extents);
        typeNum++;
        int subMeshCount = mesh.subMeshCount;
        h.x = (uint)MeshDataType.MeshDataType_SubMeshes;
        h.y = (uint)subMeshCount;
        InputHeader(h);
        typeNum++;
        for (int sub = 0; sub < subMeshCount; sub++)
        {
            var s = mesh.GetSubMesh(sub);
            SubMesh m = new SubMesh
            {
                boundingCenter = s.bounds.center,
                boundingExtent = s.bounds.extents,
                indexCount = (uint)s.indexCount,
                indexOffset = (uint)s.indexStart,
                materialIndex = (uint)sub,
                vertexOffset = (uint)s.baseVertex
            };
            byte* b = (byte*)m.Ptr();
            for(uint aa = 0; aa < sizeof(SubMesh); ++aa)
            {
                bytes.Add(b[aa]);
            }
        }

        int[] triangles = mesh.triangles;
        h.x = (uint)MeshDataType.MeshDataType_Index;
        h.y = (uint)IndexSettings.IndexFormat.IndexFormat_32Bit;
        h.z = (uint)triangles.Length;
        InputHeader(h);
        if (h.y == (uint)IndexSettings.IndexFormat.IndexFormat_32Bit)
        {
            for (int j = 0; j < h.z; ++j)
            {
                int value = triangles[j];
                byte* ptr = (byte*)value.Ptr();
                bytes.Add(ptr[0]);
                bytes.Add(ptr[1]);
                bytes.Add(ptr[2]);
                bytes.Add(ptr[3]);
            }
        }
        else
        {
            for (int j = 0; j < h.z; ++j)
            {
                ushort value = (ushort)triangles[j];
                byte* ptr = (byte*)value.Ptr();
                bytes.Add(ptr[0]);
                bytes.Add(ptr[1]);
            }
        }
        mesh.GetVertices(vec3List);
        if (vec3List.Count > 0)
        {
            typeNum++;
            InputVec3List(MeshDataType.MeshDataType_Vertex);
        }
        if (useNormal)
        {
            mesh.GetNormals(vec3List);
            if (vec3List.Count > 0)
            {
                typeNum++;
                InputVec3List(MeshDataType.MeshDataType_Normal);
            }
        }
        if (useTangent)
        {
            mesh.GetTangents(vec4List);
            if (vec4List.Count > 0)
            {
                typeNum++;
                InputVec4List(MeshDataType.MeshDataType_Tangent);
            }
        }
        if (useUV)
        {
            mesh.GetUVs(0, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV);
            }
            mesh.GetUVs(1, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV2);
            }
            mesh.GetUVs(2, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV3);
            }
            mesh.GetUVs(3, vec2List);
            if (vec2List.Count > 0)
            {
                typeNum++;
                InputVec2List(MeshDataType.MeshDataType_UV4);
            }
        }

        byte[] typeNumByte = new byte[4];
        *(uint*)typeNumByte.Ptr() = typeNum;
        DLLMeshExporter.MeshExportFile(typeNumByte.Ptr(), 4, bytes.ToArray().Ptr(), bytes.Count, path.Ptr());
    }
}
