using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Collections;
using UnityEditor;
using static Unity.Mathematics.math;
using System.IO;
using VEngine;
using UnityEngine.Rendering.PostProcessing;
using Unity.Collections.LowLevel.Unsafe;
using System.IO;
using System.Reflection;
using static Unity.Mathematics.math;
using MPipeline;

public unsafe class Test : MonoBehaviour
{
    public Mesh m;
    private MeshExporter exporter = new MeshExporter();

    public TextureExporter texExporter;
    [EasyButtons.Button]
    void ExportMesh()
    {
        exporter.meshes = new List<Mesh>();
        exporter.meshes.Clear();
        exporter.meshes.Add(m);
        exporter.Export();
    }

    [EasyButtons.Button]
    void ExportTex()
    {
        if (texExporter == null)
            texExporter = new TextureExporter();
        texExporter.Print();
    }
}
