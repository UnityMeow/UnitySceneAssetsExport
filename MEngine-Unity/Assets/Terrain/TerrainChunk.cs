using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using MPipeline;
[System.Serializable]
public struct Block
{
    public enum Operator
    {
        Add = 0,
        Multiply = 1,
        Divide = 2,
        Blend = 3
    };
    public enum MaskStrategy
    {
        Circle = 1,
        Rectangle = 2,
        CircleMask = 3,
        RectangleMask = 4,
        Mask = 5
    };
    public Operator ope;
    public MaskStrategy strategy;
    public Texture height;
    public Texture blendMask;
    public float2 rectOffset;
    public float power;
    public float rectSize;
    public float scale;
    public float offset;
}
public struct BlockTotalData
{
    public Block block;
    public double4x4 worldToLocal;
}

[RequireComponent(typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    public List<BlockTotalData> block = new List<BlockTotalData>();
    [System.NonSerialized]
    public RenderTexture heightRT;
    [System.NonSerialized]
    public RenderTexture normalTex;
    private MeshRenderer mr;
    double2 centerPos;
    double3 size;
    Vector2 currentChunkIndex;
    Vector2 chunkCount;
    public void BeginChunk(double2 centerPos,
        double3 size,
        Vector2 currentChunkIndex,
        Vector2 chunkCount)
    {
        this.currentChunkIndex = currentChunkIndex;
        this.chunkCount = chunkCount;
        this.centerPos = centerPos;
        this.size = size;
        mr = GetComponent<MeshRenderer>();
        Shader s = Shader.Find("Custom/TerrainShader");
        if (!mr.sharedMaterial || mr.sharedMaterial.shader != s)
        {
            mr.sharedMaterial = new Material(s);
        }
        mr.enabled = true;
    }
    public void UpdatePhysicsPosition(
        int3 block)
    {
        float3 xzPos = (float3)(double3(centerPos.x, 0, -centerPos.y) - (double3)block * 100.0);
        transform.position = xzPos;
    }
    public void UpdateChunk(
        Texture heightTex,
        HeightToNormal h2n,
        Material copyMat,
        ComputeShader cs,
        CommandBuffer cb)
    {
        if (heightTex)
        {
            if (heightRT && (heightRT.width != heightTex.width || heightRT.height != heightTex.height))
            {
                DestroyImmediate(heightRT);
                heightRT = null;
            }
            if (!heightRT)
            {
                heightRT = new RenderTexture(new RenderTextureDescriptor
                {
                    width = heightTex.width,
                    height = heightTex.height,
                    volumeDepth = 1,
                    graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    msaaSamples = 1,
                    enableRandomWrite = true
                });
                heightRT.Create();
            }
            //Blit Base Height
            Vector4 tofst = new Vector4(1f / chunkCount.x, 1f / chunkCount.y, currentChunkIndex.x / chunkCount.x, currentChunkIndex.y / chunkCount.y);
            cb.SetGlobalVector("_TileOffset", tofst);
            cb.Blit(heightTex, heightRT, copyMat, 0);
            //Render Block's
            cb.SetComputeTextureParam(cs, 0, "_MainTex", heightRT);
            cb.SetComputeVectorParam(cs, "_TexelSize", new Vector4(1.0f / heightRT.width, 1.0f / heightRT.height, heightRT.width, heightRT.height));
            double4x4 localToWorld = new double4x4(
                new double4(size.x, 0, 0, 0),
                new double4(0, size.y, 0, 0),
                new double4(0, 0, size.z, 0),
                new double4(centerPos.x, 0, -centerPos.y, 1));
            foreach (var i in block)
            {
                int _MaskRT = Shader.PropertyToID("_MaskRT");
                RenderTargetIdentifier maskRT;
                if (i.block.strategy == Block.MaskStrategy.Mask)
                {
                    maskRT = i.block.blendMask;
                }
                else
                {
                    
                    maskRT = _MaskRT;
                    cb.GetTemporaryRT(_MaskRT, new RenderTextureDescriptor
                    {
                        width = 2048,
                        height = 2048,
                        volumeDepth = 1,
                        graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm,
                        dimension = TextureDimension.Tex2D,
                        enableRandomWrite = true,
                        msaaSamples = 1
                    });
                    cb.SetGlobalVector("_TileOffset", float4(i.block.rectOffset, i.block.rectSize, i.block.power));
                    cb.Blit(BuiltinRenderTextureType.None, _MaskRT, copyMat, (int)i.block.strategy);
                }
                float4x4 chunkToBlock = (float4x4)mul(i.worldToLocal, localToWorld);
                cb.SetComputeMatrixParam(cs, "_DecalMatrix", chunkToBlock);
                cb.SetComputeIntParam(cs, "_Operator", (int)i.block.ope);
                cb.SetComputeVectorParam(cs, "_ScaleOffset", new Vector4(i.block.scale, i.block.offset));
                cb.SetComputeTextureParam(cs, 0, "_HeightTex", i.block.height);
                cb.SetComputeTextureParam(cs, 0, "_MaskTex", maskRT);
                cb.DispatchCompute(cs, 0, heightRT.width / 8, heightRT.height / 8, 1);
                cb.ReleaseTemporaryRT(_MaskRT);
            }
            normalTex = h2n.GetNormalFromHeight(normalTex, heightRT, cb);
        }
        Material mat = mr.sharedMaterial;
        mat.SetTexture("_HeightMap", heightRT);
        mat.SetTexture("_NormalMap", normalTex);
        block.Clear();
    }
    public void EndChunk()
    {
        if (heightRT) Destroy(heightRT);
        if (normalTex) Destroy(normalTex);
        heightRT = null;
        normalTex = null;
        mr = GetComponent<MeshRenderer>();
        mr.enabled = false;
    }
}
