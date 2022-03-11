using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;

public enum TextureExportSetting
{
    RGB,
    RGBA,
    Normal,
    RG,
    SingleChannel,
    HDR
}
public unsafe class MaterialExporter
{
    public Material testMat;
    public string Path;

    private ulong jsonPtr;
    private ulong outsideJsonPtr;

    private void BaseMaterialExport(Action<Texture, TextureExportSetting> loadtexture)
    {
        Vector4 tileOffset = testMat.GetVector("_TileOffset");
        Vector4 albedo = testMat.GetVector("_Color");
        float metallic = testMat.GetFloat("_MetallicIntensity");
        Vector4 emissionColor = testMat.GetVector("_EmissionColor");
        float emissionIntensity = testMat.GetFloat("_EmissionMultiplier");
        float smoothness = testMat.GetFloat("_Glossiness");
        float occlusion = testMat.GetFloat("_Occlusion");
        float cutoff = testMat.GetFloat("_Cutoff");
        Texture albedoTex = testMat.GetTexture("_MainTex");
        Texture normalTex = testMat.GetTexture("_BumpMap");
        Texture specTex = testMat.GetTexture("_SpecularMap");
        Texture clipTex = testMat.GetTexture("_ClipMap");
        Texture emissionTex = testMat.GetTexture("_EmissionMap");
        loadtexture(albedoTex, TextureExportSetting.RGB);
        loadtexture(normalTex, TextureExportSetting.Normal);
        loadtexture(specTex, TextureExportSetting.RGB);
        loadtexture(clipTex, TextureExportSetting.SingleChannel);
        loadtexture(emissionTex, TextureExportSetting.HDR);
        emissionColor *= emissionIntensity;
        DLLToJson.ToJsonAdd(jsonPtr, "uvScale", (float*)tileOffset.Ptr(), 2);
        DLLToJson.ToJsonAdd(jsonPtr, "uvOffset", ((float*)tileOffset.Ptr()) + 2, 2);
        DLLToJson.ToJsonAdd(jsonPtr, "albedo", (float*)albedo.Ptr(), 4);
        float useclip = 0;
        if (clipTex)
        {
            useclip = 1;
            DLLToJson.ToJsonAdd(jsonPtr, "clipalpha", cutoff.Ptr(), 1);
        }
        DLLToJson.ToJsonAdd(jsonPtr, "useclip", useclip.Ptr(), 1);
        DLLToJson.ToJsonAdd(jsonPtr, "metallic", metallic.Ptr(), 1);
        DLLToJson.ToJsonAdd(jsonPtr, "emission", (float*)emissionColor.Ptr(), 4);
        DLLToJson.ToJsonAdd(jsonPtr, "smoothness", smoothness.Ptr(), 1);
        DLLToJson.ToJsonAdd(jsonPtr, "occlusion", occlusion.Ptr(), 1);
        DLLToJson.ToJsonAdd(jsonPtr, "albedoTexIndex", (albedoTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(albedoTex)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "specularTexIndex", (specTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(specTex)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "normalTexIndex", (normalTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(normalTex)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "emissionTexIndex", (emissionTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(emissionTex)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "clipTexIndex", (clipTex ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clipTex)) : "0"));
        DLLToJson.ToJsonAddKeyValue(outsideJsonPtr, "BaseShader", jsonPtr);
        DLLToJson.ToJsonExportFile(outsideJsonPtr, Path.Ptr());
    }

    private void RockMaterialExport(Action<Texture, TextureExportSetting> loadtexture)
    {
        float _DetailNormalUVScale = testMat.GetFloat("_DetailNormalUVScale");
        float _DetailNormalIntesity = testMat.GetFloat("_DetailNormalIntesity");
        float _LUTSelect = testMat.GetFloat("_LUTSelect");
        float _LUTColorPower = testMat.GetFloat("_LUTColorPower");
        float _AOMaskIntesity = testMat.GetFloat("_AOMaskIntesity");
        float _AOLerp = testMat.GetFloat("_AOLerp");
        float _NormalMaskIntesity = testMat.GetFloat("_NormalMaskIntesity");
        float _DirtLerp = testMat.GetFloat("_DirtLerp");
        float _DirMaskPowerIntesity = testMat.GetFloat("_DirMaskPowerIntesity");
        float _RoughnessMaskPowerIntesity = testMat.GetFloat("_RoughnessMaskPowerIntesity");
        float _RoughnessMin = testMat.GetFloat("_RoughnessMin");
        float _RoughnessMax = testMat.GetFloat("_RoughnessMax");
        Texture _Normal = testMat.GetTexture("_Normal");
        Texture _DetailNormal = testMat.GetTexture("_DetailNormal");
        Texture _LUTColor = testMat.GetTexture("_LUTColor");
        Texture _Noise = testMat.GetTexture("_Noise");
        Texture _UV_OC = testMat.GetTexture("_UV_OC");
        Texture _Tilling_CV = testMat.GetTexture("_Tilling_CV");

        loadtexture(_Normal, TextureExportSetting.RG);
        loadtexture(_DetailNormal, TextureExportSetting.RG);
        loadtexture(_LUTColor, TextureExportSetting.RGB);
        loadtexture(_Noise, TextureExportSetting.RGBA);
        loadtexture(_UV_OC, TextureExportSetting.Normal);
        loadtexture(_Tilling_CV, TextureExportSetting.SingleChannel);


        Vector4 _DirColorTint = testMat.GetVector("_DirColorTint");
        DLLToJson.ToJsonAdd(jsonPtr, "DetailNormal_uv_scale", _DetailNormalUVScale.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "Detailnormal_intesity", _DetailNormalIntesity.Ptr());
        float maskIntensity = 1;
        DLLToJson.ToJsonAdd(jsonPtr, "DetailNormal_mask_intesity", &maskIntensity); //TODO
        DLLToJson.ToJsonAdd(jsonPtr, "Normal_mask_intesity", _NormalMaskIntesity.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "LUTcolorpower", _LUTColorPower.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "LUTselect", _LUTSelect.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "dirtlerp", _DirtLerp.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "roughness_max", _RoughnessMax.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "roughness_min", _RoughnessMin.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "roughness_mask_power_intesity", _RoughnessMaskPowerIntesity.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "AOlerp", _AOLerp.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "dirt_mask_power_intesity", _DirMaskPowerIntesity.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "AO_mask_intesity", _AOMaskIntesity.Ptr());
        DLLToJson.ToJsonAdd(jsonPtr, "dirtColor", (float*)_DirColorTint.Ptr(), 4);
        DLLToJson.ToJsonAdd(jsonPtr, "NormalTexIndex", (_Normal ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_Normal)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "DetailNormalTexIndex", (_DetailNormal ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_DetailNormal)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "LUTColorTexIndex", (_LUTColor ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_LUTColor)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "NoiseTexIndex", (_Noise ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_Noise)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "UVOCTexIndex", (_UV_OC ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_UV_OC)) : "0"));
        DLLToJson.ToJsonAdd(jsonPtr, "TCVTexIndex", (_Tilling_CV ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_Tilling_CV)) : "0"));
        DLLToJson.ToJsonAddKeyValue(outsideJsonPtr, "RockShader", jsonPtr);
        DLLToJson.ToJsonExportFile(outsideJsonPtr, Path.Ptr());
    }

    public void Export(Action<Texture, TextureExportSetting> loadtexture)
    {
        DLLToJson.ToJsonInit(jsonPtr.Ptr());
        DLLToJson.ToJsonInit(outsideJsonPtr.Ptr());
        if (testMat.shader.name == "ShouShouPBR")
        {
            BaseMaterialExport(loadtexture);
        }
        else
        {
            RockMaterialExport(loadtexture);
        }
        DLLToJson.ToJsonDispose(jsonPtr);
        DLLToJson.ToJsonDispose(outsideJsonPtr);
    }
}
