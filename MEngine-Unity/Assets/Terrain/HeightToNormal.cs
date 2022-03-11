using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HeightToNormal : MonoBehaviour
{
    public ComputeShader cs;
    public double worldSize = 2048;
    public double heightSize = 100;
    public Texture heightTex;
    public string path = "Resource/terrainTest.vtex";
    public RenderTexture GetNormalFromHeight(RenderTexture normalTex, RenderTexture normalTexDownSampled, Texture heightTex)
    {
        if (!normalTex || normalTex.width != heightTex.width * 2 || normalTex.height != heightTex.height * 2)
            normalTex = new RenderTexture(new RenderTextureDescriptor
            {
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_UNorm,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                width = heightTex.width * 2,
                height = heightTex.height * 2,
                volumeDepth = 1,
                msaaSamples = 1,
                enableRandomWrite = true
            });
        if (!normalTexDownSampled || normalTexDownSampled.width != heightTex.width || normalTexDownSampled.height != heightTex.height)
            normalTexDownSampled = new RenderTexture(new RenderTextureDescriptor
            {
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_UNorm,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                width = heightTex.width,
                height = heightTex.height,
                volumeDepth = 1,
                msaaSamples = 1,
                enableRandomWrite = true
            });
        normalTex.Create();
        normalTexDownSampled.Create();
        cs.SetTexture(0, "_Heightmap", heightTex);
        cs.SetTexture(0, "_OutputNormal", normalTex);
        cs.SetVector("_TexelSize", new Vector4(normalTex.width, normalTex.height, 1, 1));
        double pixelSize = worldSize / (double)normalTex.width;
        double heightScale = heightSize / pixelSize;
        heightScale /= 8;
        cs.SetFloat("_HeightScale", (float)heightScale);
        cs.Dispatch(0, normalTex.width / 8, normalTex.height / 8, 1);
        cs.SetTexture(1, "_SourceTex", normalTex, 0);
        cs.SetTexture(1, "_DestTex", normalTexDownSampled, 0);
        cs.Dispatch(1, normalTexDownSampled.width / 8, normalTexDownSampled.height / 8, 1);
        if (normalTex) DestroyImmediate(normalTex);
        return normalTexDownSampled;
    }
    public RenderTexture GetNormalFromHeight(RenderTexture normalTexDownSampled, Texture heightTex, CommandBuffer commandBuffer)
    {
        int _NormalTex = Shader.PropertyToID("_NormalTex");
        commandBuffer.GetTemporaryRT(_NormalTex, new RenderTextureDescriptor
        {
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_UNorm,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            width = heightTex.width * 2,
            height = heightTex.height * 2,
            volumeDepth = 1,
            msaaSamples = 1,
            enableRandomWrite = true
        });
        if (!normalTexDownSampled || normalTexDownSampled.width != heightTex.width || normalTexDownSampled.height != heightTex.height)
            normalTexDownSampled = new RenderTexture(new RenderTextureDescriptor
            {
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_UNorm,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                width = heightTex.width,
                height = heightTex.height,
                volumeDepth = 1,
                msaaSamples = 1,
                enableRandomWrite = true
            });
        normalTexDownSampled.Create();
        commandBuffer.SetComputeTextureParam(cs, 0, "_Heightmap", heightTex);
        commandBuffer.SetComputeTextureParam(cs, 0, "_OutputNormal", _NormalTex);
        commandBuffer.SetComputeVectorParam(cs, "_TexelSize", new Vector4(heightTex.width * 2, (heightTex.height * 2), 1, 1));
        double pixelSize = worldSize / (double)(heightTex.width * 2);
        double heightScale = heightSize / pixelSize;
        heightScale /= 8;
        commandBuffer.SetComputeFloatParam(cs, "_HeightScale", (float)heightScale);
        commandBuffer.DispatchCompute(cs, 0, (heightTex.width * 2) / 8, (heightTex.height * 2) / 8, 1);
        commandBuffer.SetComputeTextureParam(cs, 1, "_SourceTex", _NormalTex, 0);
        commandBuffer.SetComputeTextureParam(cs, 1, "_DestTex", normalTexDownSampled, 0);
        commandBuffer.DispatchCompute(cs, 1, normalTexDownSampled.width / 8, normalTexDownSampled.height / 8, 1);
        commandBuffer.ReleaseTemporaryRT(_NormalTex);
        return normalTexDownSampled;
    }
    [EasyButtons.Button]
    void Run()
    {
        if (!heightTex) return;
        RenderTexture normalTex = null;
        RenderTexture normalTexDownSampled = null;
        normalTexDownSampled = GetNormalFromHeight(normalTex, normalTexDownSampled, heightTex);
        TextureExporter tex = new TextureExporter();
        tex.texture = normalTexDownSampled;
        tex.useMipMap = false;
        tex.path = path;
        tex.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC5U;
        tex.Print();
        if (normalTexDownSampled) DestroyImmediate(normalTexDownSampled);
    }
}
