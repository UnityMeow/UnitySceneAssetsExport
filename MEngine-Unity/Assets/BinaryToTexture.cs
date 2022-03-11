using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public struct TextureHeader
{
    public uint width;
    public uint height;
    public uint pixelSize;
    public uint channelCount;
    public uint isFloat;
};

public unsafe class BinaryToTexture : MonoBehaviour
{
    public Material testMat;
    public Texture2D tex;
    public string path;
    [EasyButtons.Button]
    void GetTex()
    {
        byte[] data;
        using (FileStream fsm = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            data = new byte[fsm.Length];
            fsm.Read(data, 0, (int)fsm.Length);
        }
        TextureHeader th = new TextureHeader();
        UnsafeUtility.MemCpy(th.Ptr(), data.Ptr(), sizeof(TextureHeader));
        Debug.Log(th.width);
        Debug.Log(th.height);
        tex = new Texture2D((int)th.width, (int)th.height, TextureFormat.RGBAFloat, false);
        NativeArray<float4> ntv = new NativeArray<float4>((int)(th.width * th.height), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        float4* bb = (float4*)ntv.Ptr();
        byte* dataPtr = data.Ptr();
        dataPtr += sizeof(TextureHeader);
        if (th.channelCount == 1)
        {
            int count = 0;
            if (th.pixelSize == 1)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        bb[count] = float4(dataPtr[count], 0, 0, 0);
                        count++;
                    }
            }
            else if (th.pixelSize == 2)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        ushort* v = (ushort*)(dataPtr + count);
                        bb[count / 2] = float4(*v, 0, 0, 0);
                        count += 2;
                    }
            }
            else if (th.pixelSize == 4)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        float* v = (float*)(dataPtr + count);
                        bb[count / 4] = float4(*v, 0, 0, 0);
                        count += 4;

                    }
            }
        }
        else if (th.channelCount == 2)
        {
            int count = 0;
            if (th.pixelSize == 2)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        bb[count] = float4(dataPtr[count], dataPtr[count + 1], 0, 0);
                        count += 2;
                    }
            }
            else if (th.pixelSize == 4)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        ushort* v = (ushort*)(dataPtr + count);
                        bb[count] = float4(v[0], v[1], 0, 0);
                        count += 4;
                    }
            }
            else if (th.pixelSize == 8)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        float* v = (float*)(dataPtr + count);
                        bb[count] = float4(v[0], v[1], 0, 0);
                        count += 8;
                    }
            }
        }
        else if (th.channelCount == 4)
        {
            int count = 0;
            if (th.pixelSize == 4)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        bb[count / 4] = float4(dataPtr[count], dataPtr[count + 1], dataPtr[count + 2], dataPtr[count + 3]);
                        count += 4;
                    }
            }
            else if (th.pixelSize == 8)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        ushort* v = (ushort*)(dataPtr + count);
                        bb[count / 8] = float4(v[0], v[1], v[2], v[3]);
                        count += 8;
                    }
            }
            else if (th.pixelSize == 16)
            {
                for (int x = 0; x < th.width; ++x)
                    for (int y = 0; y < th.height; ++y)
                    {
                        float* v = (float*)(dataPtr + count);
                        bb[count / 16] = float4(v[0], v[1], v[2], v[3]);
                        count += 16;
                    }
            }
        }
        tex.SetPixelData<float4>(ntv, 0);
        tex.Apply();
        testMat.SetTexture("_MainTex", tex);
    }
}
