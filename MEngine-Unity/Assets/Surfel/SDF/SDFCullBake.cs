using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using Unity.Collections;
using static Unity.Mathematics.math;

public unsafe class SDFCullBake : MonoBehaviour
{
    public struct Cube
    {
        public float4x4 worldToLocalMatrix;
        public float3 localExtent;
        public uint type;
    };

    ComputeBuffer primBuffer;
    ComputeBuffer sphereBuffer;
    public Transform[] cubes;
    public Material postMat;
    private Camera cam;
    [Range(0, 512)]
    public int iteCount = 256;
    // Start is called before the first frame update
    void Start()
    {
        NativeArray<Cube> cubeDatas = new NativeArray<Cube>(cubes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for(int i = 0; i < cubeDatas.Length; ++i)
        {
            Cube c;
            Transform tr = cubes[i];
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
        primBuffer = new ComputeBuffer(cubeDatas.Length, sizeof(Cube));
        primBuffer.SetData(cubeDatas);
        sphereBuffer = new ComputeBuffer(1, sizeof(float4));
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        postMat.SetInt("_IterateCount", iteCount);
        postMat.SetInt("_SphereCount", 0);
        postMat.SetInt("_PrimitiveCount", primBuffer.count);
        postMat.SetBuffer("_PrimitiveBuffer", primBuffer);
        postMat.SetBuffer("_SphereBuffer", sphereBuffer);
        postMat.SetMatrix("_InvVP", (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix).inverse);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, postMat, 0);
    }
}
