using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public class TerrainMaterialPack : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialPack
    {
        public Texture albedo;
        public Texture normal;
        public Texture smo;
    }
    [System.Serializable]
    public struct SplatLayer
    {
        public Texture splatTexture;
        public uint layer;
    }
    [Header("Material Settings: ")]
    public List<MaterialPack> materialPack;
    public uint baseLayer = 0;
    public string outputJsonString = "Material.json";
    public string outputPath = "Resource/";
    [Header("Splat & Index Settings: ")]
    public List<SplatLayer> splatLayers;
    private RenderTexture albedoTexture;
    private RenderTexture normalTexture;
    private RenderTexture smoTexture;
    public ComputeShader cs;
    public Material testMaterial;
    public int showResolution = 16384;
    public float tileScale = 128;
    public int splatResolution = 2048;

    Texture GetAlbedoTexure(uint layer)
    {
        layer = math.clamp(layer, 0, (uint)materialPack.Count);
        return materialPack[(int)layer].albedo;
    }

    Texture GetNormalTexture(uint layer)
    {
        layer = math.clamp(layer, 0, (uint)materialPack.Count);
        return materialPack[(int)layer].normal;
    }
    Texture GetSMOTexture(uint layer)
    {
        layer = math.clamp(layer, 0, (uint)materialPack.Count);
        return materialPack[(int)layer].smo;
    }
    private void Dispatch(int kernel)
    {
        cs.Dispatch(kernel, (showResolution + 15) / 16, (showResolution + 15) / 16, 1);
    }
    [EasyButtons.Button]
    void UpdateTexture()
    {


        if (materialPack.Count == 0)
        {
            Debug.Log("No Texture Loaded!");
            return;
        }
        if (!albedoTexture
         || (albedoTexture.width != showResolution || albedoTexture.height != showResolution))
        {
            albedoTexture = new RenderTexture(
                showResolution, showResolution, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 0);
            albedoTexture.enableRandomWrite = true;
            albedoTexture.Create();
        }
        if (!normalTexture
            || (normalTexture.width != showResolution || normalTexture.height != showResolution))
        {
            normalTexture = new RenderTexture(
                showResolution, showResolution, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SNorm, 0);
            normalTexture.enableRandomWrite = true;
            normalTexture.Create();
        }
        if (!smoTexture
            || (smoTexture.width != showResolution || smoTexture.height != showResolution))
        {
            smoTexture = new RenderTexture(
                showResolution, showResolution, 1, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 0);
            smoTexture.enableRandomWrite = true;
            smoTexture.Create();
        }
        cs.SetTexture(0, "_AlbedoResult", albedoTexture);
        cs.SetTexture(0, "_NormalResult", normalTexture);
        cs.SetTexture(0, "_SMOResult", smoTexture);
        cs.SetTexture(1, "_AlbedoResult", albedoTexture);
        cs.SetTexture(1, "_NormalResult", normalTexture);
        cs.SetTexture(1, "_SMOResult", smoTexture);
        cs.SetVector("_TileScale", new Vector4(tileScale, tileScale, tileScale, tileScale));
        cs.SetInt("_Count", showResolution);

        cs.SetTexture(0, "_AlbedoTexture", GetAlbedoTexure(baseLayer));
        cs.SetTexture(0, "_NormalTexture", GetNormalTexture(baseLayer));
        cs.SetTexture(0, "_SMOTexture", GetSMOTexture(baseLayer));
        Dispatch(0);
        for (int i = 0; i < splatLayers.Count; ++i)
        {
            var a = splatLayers[i];
            if (!a.splatTexture)
            {
                splatLayers[i] = splatLayers[splatLayers.Count - 1];
                i--;
                splatLayers.RemoveAt(splatLayers.Count - 1);
                break;
            }
            a.layer = math.clamp(a.layer, 0, (uint)materialPack.Count);
            splatLayers[i] = a;
            cs.SetTexture(1, "_AlbedoTexture", materialPack[(int)a.layer].albedo);
            cs.SetTexture(1, "_NormalTexture", materialPack[(int)a.layer].normal);
            cs.SetTexture(1, "_SMOTexture", materialPack[(int)a.layer].smo);
            cs.SetTexture(1, "_SplatTexture", a.splatTexture);
            Dispatch(1);
        }
        if (testMaterial)
        {
            testMaterial.SetTexture("_MainTex", albedoTexture);
            testMaterial.SetTexture("_NormalTex", normalTexture);
            testMaterial.SetTexture("_SMOTex", smoTexture);
        }
    }
    private RenderTexture splatTexture;
    private RenderTexture indexTexture;
    public string splatSavePath = "TestSplatTex.vtex";
    public string indexSavePath = "TestIndexTex.vtex";
    public TextureExporter texExp;
    [EasyButtons.Button]
    void ExportSplatIndexTexture()
    {
        if (texExp == null)
        {
            Debug.Log("No Texture Exporter!");
            return;
        }
        if (albedoTexture) DestroyImmediate(albedoTexture);
        if (normalTexture) DestroyImmediate(normalTexture);
        if (smoTexture) DestroyImmediate(smoTexture);
        albedoTexture = null;
        normalTexture = null;
        smoTexture = null;
        if (splatTexture) DestroyImmediate(splatTexture);
        if (indexTexture) DestroyImmediate(indexTexture);
        splatTexture = new RenderTexture(
                splatResolution,
                splatResolution,
                1,
                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                1);
        splatTexture.enableRandomWrite = true;
        indexTexture = new RenderTexture(
             splatResolution,
            splatResolution,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UInt,
            1);
        indexTexture.enableRandomWrite = true;
        splatTexture.Create();
        indexTexture.Create();

        cs.SetTexture(2, "_IndexTexResult", indexTexture);
        cs.SetTexture(2, "_SplatResult", splatTexture);
        cs.SetTexture(3, "_IndexTexResult", indexTexture);
        cs.SetTexture(3, "_SplatResult", splatTexture);
        cs.SetInt("_CurrentIndex", (int)baseLayer);
        cs.SetInt("_Count", splatResolution);
        cs.Dispatch(2, splatResolution / 16, splatResolution / 16, 1);
        for (int i = 0; i < splatLayers.Count; ++i)
        {
            var a = splatLayers[i];
            if (!a.splatTexture)
            {
                splatLayers[i] = splatLayers[splatLayers.Count - 1];
                i--;
                splatLayers.RemoveAt(splatLayers.Count - 1);
                break;
            }
            a.layer = math.clamp(a.layer, 0, (uint)materialPack.Count);
            splatLayers[i] = a;
            cs.SetTexture(3, "_SplatTexture", a.splatTexture);
            cs.SetInt("_CurrentIndex", (int)a.layer);
            cs.Dispatch(3, splatResolution / 16, splatResolution / 16, 1);
        }
        texExp.isNormal = false;
        texExp.useAlphaInCompress = true;
        texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
        texExp.useMipMap = false;
        texExp.path = splatSavePath;
        texExp.texture = splatTexture;
        texExp.Print();
        texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_R8G8B8A8_UINT;
        texExp.path = indexSavePath;
        texExp.texture = indexTexture;
        texExp.Print();
    }
    [EasyButtons.Button]
    void ExportMaterialTexture()
    {
        int printNormalPass = cs.FindKernel("PrintNormal");
        texExp.useAlphaInCompress = false;
        texExp.useMipMap = true;
        Dictionary<string, bool> b = new Dictionary<string, bool>(100);
        int check = 0;
        foreach (var i in materialPack)
        {
            if (b.ContainsKey(i.albedo.name) ||
               b.ContainsKey(i.normal.name) ||
               b.ContainsKey(i.smo.name))
            {
                Debug.LogError("Same name cannot be packed! Index at: " + check);
                return;
            }
            check++;
            b[i.albedo.name] = true;
            b[i.normal.name] = true;
            b[i.smo.name] = true;
        }
        foreach (var i in materialPack)
        {
            texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            texExp.isNormal = false;
            texExp.texture = i.albedo;
            texExp.path = outputPath + i.albedo.name + ".vtex";
            texExp.Print();
            texExp.isNormal = true;
            texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC5U;
            texExp.texture = i.normal;
            texExp.path = outputPath + i.normal.name + ".vtex";
            texExp.Print();
            texExp.isNormal = false;
            texExp.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
            texExp.texture = i.smo;
            texExp.path = outputPath + i.smo.name + ".vtex";
            texExp.Print();
        }
        List<string> str = new List<string>();
        for (int i = 0; i < materialPack.Count; ++i)
        {
            str.Add(MJsonUtility.GetKeyValue(i.ToString(), MJsonUtility.MakeJsonObject(
                MJsonUtility.GetKeyValue("albedo", materialPack[i].albedo.name + ".vtex", false),
                MJsonUtility.GetKeyValue("normal", materialPack[i].normal.name + ".vtex", false),
                MJsonUtility.GetKeyValue("smo", materialPack[i].smo.name + ".vtex", false)), true));
        }
        File.WriteAllText(outputPath + outputJsonString, MJsonUtility.MakeJsonObject(str));
    }
}
