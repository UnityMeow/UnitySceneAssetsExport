using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// AssetsData
// ScenesData
// XXXXSceneData

public struct JsonTransform
{
    public float3 position;
    public float4 rotation;
    public float3 localscale;
}

public unsafe struct JsonAnimClip
{
    public int clipCount;
    public char** clipName;
    public char** clipGUID;
}

public unsafe struct JsonRenderer
{
    public int meshCount;
    public float* meshDis;
    public char** meshGUID;
    public char** matGUID;
    public int mask;
}

public unsafe struct JsonSkinnedRenderer
{
    public int meshCount;
    public float* meshDis;
    public char** meshGUID;
    public char** matGUID;
    public int mask;
}

public struct JsonLight
{
    public int lightType;
    public float intensity;
    public float shadowBias;
    public float shadowNearPlane;
    public float range;
    public float angle;
    public float4 color;
    public float smallAngle;
    public int useShadow;
    public int shadowCache;
}
public unsafe struct JsonReflectionProbe
{
    public float blendDistance;
    public float color;
    public float3 localPosition;
    public int boxProjection;
    public float3 localExtent;
    public char* cubemapGUID;
}

public unsafe class MiJson
{
    private ulong jsonPtr;

    public MiJson()
    {
        Init();
    }
    public MiJson(string str)
    {
        Init(str);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        DLLToJson.ToJsonInit(jsonPtr.Ptr());
    }

    public void Init(string str)
    {
        DLLToJson.ToJsonInit_Path(jsonPtr.Ptr(), str.Ptr());
    }

    /// <summary>
    /// 导出
    /// </summary>
    /// <param name="path"></param>
    public void Exporter(string path)
    {
        DLLToJson.ToJsonExportFile(jsonPtr, path.Ptr());
        // 导出结束释放
        Dispose();
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Dispose()
    {
        DLLToJson.ToJsonDispose(jsonPtr);
    }

    public void AddKey(string key, string value)
    {
        DLLToJson.ToJsonAdd(jsonPtr, key, value);
    }

    public void DeleteKey(string key)
    {
        DLLToJson.DeleteJsonKey(jsonPtr, key);
    }

    public void AddKey(JsonTransform tr)
    {
        DLLToJson.ToJsonAdd(jsonPtr, tr.Ptr());
    }

    public void AddKey(JsonRenderer renderer)
    {
        DLLToJson.ToJsonAdd(jsonPtr, renderer.Ptr());
    }

    public void AddKey(JsonSkinnedRenderer renderer)
    {
        DLLToJson.ToJsonAdd(jsonPtr, renderer.Ptr());
    }

    public void AddKey(JsonAnimClip clip)
    {
        DLLToJson.ToJsonAdd(jsonPtr, clip.Ptr());
    }

    public void AddKey(JsonLight light)
    {
        DLLToJson.ToJsonAdd(jsonPtr, light.Ptr());
    }

    public void AddKey(JsonReflectionProbe rp)
    {
        DLLToJson.ToJsonAdd(jsonPtr, rp.Ptr());
    }

    /// <summary>
    /// 数组形式 添加Json数据
    /// </summary>
    /// <param name="jPtr">json指针</param>
    public void AddJson(ulong jPtr)
    {
        DLLToJson.ToJsonAdd(jsonPtr, jPtr);
    }

    /// <summary>
    /// 获取json指针
    /// </summary>
    /// <returns></returns>
    public ulong GetJsonPtr()
    {
        return DLLToJson.ToJsonGetPtr(jsonPtr);
    }
}
