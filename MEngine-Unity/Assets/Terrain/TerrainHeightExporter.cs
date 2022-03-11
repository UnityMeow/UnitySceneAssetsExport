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
using UnityEditor;
public struct TerrainTex
{
    public Texture[] heightTexs;
    public Texture[] normalTexs;
}
public static class TerrainHeightExporter
{
    public static string GetTerrainJson(
        double2 terrainCenter,
        double2 terrainSize,
        double2 heightBounding,
        int chunkCount,
        List<double> lodDistances,
        string texName)
    {
        string[] GetLODDistances()
        {
            string[] strs = new string[lodDistances.Count];
            for (int i = 0; i < lodDistances.Count; ++i)
            {
                strs[i] = MJsonUtility.GetKeyValue(i.ToString(), lodDistances[i]);
            }
            return strs;
        }
        string s = MJsonUtility.MakeJsonObject(
            MJsonUtility.GetKeyValue("terrainCenterX", terrainCenter.x),
            MJsonUtility.GetKeyValue("terrainCenterY", terrainCenter.y),
            MJsonUtility.GetKeyValue("terrainSizeX", terrainSize.x),
            MJsonUtility.GetKeyValue("terrainSizeY", terrainSize.y),
            MJsonUtility.GetKeyValue("heightMin", heightBounding.x),
            MJsonUtility.GetKeyValue("heightMax", heightBounding.y),
            MJsonUtility.GetKeyValue("chunk", chunkCount),
            MJsonUtility.GetKeyValue("mip_distance", MJsonUtility.MakeJsonObject(
                GetLODDistances()), true),
            MJsonUtility.GetKeyValue("name", texName, false));
        return s;
    }
    public static unsafe void OutputAllTex(
        TerrainTex tex,
        int2 chunkIndex,
        string folderPath,
        string texName,
        TextureExporter texExp)
    {
        int i = 0;
        texExp.path = folderPath + texName + "_0_0_0_h.vtex";
        texExp.useMipMap = false;
        char* c = texExp.path.Ptr();

        for (int j = 0; j < tex.heightTexs.Length; ++j)
        {
            c[folderPath.Length + texName.Length + 1] = (char)(chunkIndex.x + 48);
            c[folderPath.Length + texName.Length + 3] = (char)(chunkIndex.y + 48);
            c[folderPath.Length + texName.Length + 5] = (char)(j + 48);
            c[folderPath.Length + texName.Length + 7] = 'h';
            texExp.texture = tex.heightTexs[i];
            texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_R16_UNorm;
            texExp.Print();
            c[folderPath.Length + texName.Length + 7] = 'n';
            texExp.texture = tex.normalTexs[i];
            texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC5U;
            texExp.Print();
        }
        i++;

    }
}
