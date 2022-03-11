using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEditor;
using UnityEngine;

public static class MiExtension
{
    public static string GetGUID(this Object obj)
    {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        return AssetDatabase.AssetPathToGUID(assetPath);
    }

    public static string GetAssetPath(this Object obj)
    { 
        return AssetDatabase.GetAssetPath(obj);
    }

    public static float4x3 LocalToWorldFloat4x3(this Transform tr)
    {
        float3 pos = tr.position;
        float4x3 result = new float4x3() 
        {
            c0 = float4(tr.right, pos.x),
            c1 = float4(tr.up, pos.y),
            c2 = float4(tr.forward, pos.z),
        };
        return result;
    }
}
