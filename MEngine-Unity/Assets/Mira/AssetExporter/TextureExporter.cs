using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.IO;
using static Unity.Mathematics.math;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

/// <summary>
/// 压缩工作
/// 原BC5UCompressJob，BC5SCompressJob，BC6UHCompressJob合并
/// </summary>
public unsafe struct CompressJob : IJobParallelFor
{
    /// <summary>
    /// 压缩类型
    /// </summary>
    public TextureData.LoadFormat compressType;
    [NativeDisableUnsafePtrRestriction]
    public uint2* dest;
    [NativeDisableUnsafePtrRestriction]
    public float4* source;
    public int width;

    public void Execute(int thread)
    {
        uint GetIndex(uint2 uv, uint width)
        {
            return uv.y * width + uv.x;
        }
        float4* b = stackalloc float4[4 * 4];
        int y = thread / (width / 4);
        int x = thread % (width / 4);
        for (uint i = 0, yy = 0; yy < 4; ++yy)
        {
            for (uint xx = 0; xx < 4; ++xx)
            {
                b[i] = source[GetIndex(uint2((uint)(x * 4 + xx), (uint)(y * 4 + yy)), (uint)width)];
                i++;
            }
        }
        switch (compressType)
        {
            case TextureData.LoadFormat.LoadFormat_BC5U:
                DXTCompress.D3DXEncodeBC5U((byte*)(dest + thread * 2), b, 0);
                break;
            case TextureData.LoadFormat.LoadFormat_BC5S:
                DXTCompress.D3DXEncodeBC5S((byte*)(dest + thread * 2), b, 0);
                break;
            case TextureData.LoadFormat.LoadFormat_BC6H:
                DXTCompress.D3DXEncodeBC6HU((byte*)(dest + thread * 2), b, 0);
                break;
            case TextureData.LoadFormat.LoadFormat_BC4U:
                DXTCompress.D3DXEncodeBC4U((byte*)(dest + thread), b, 0);
                break;
            case TextureData.LoadFormat.LoadFormat_BC4S:
                DXTCompress.D3DXEncodeBC4S((byte*)(dest + thread), b, 0);
                break;
        }
    }
}

/// <summary>
/// 纹理类型
/// </summary>
public enum TextureType
{
    /// <summary>
    /// 2D纹理
    /// </summary>
    Tex2D = 0,
    /// <summary>
    /// 3D纹理
    /// </summary>
    Tex3D = 1,
    /// <summary>
    /// 立方体贴图
    /// </summary>
    Cubemap = 2,

    Num = 3
};

/// <summary>
/// 纹理相关数据
/// </summary>
public struct TextureData
{
    public uint width;
    public uint height;
    /// <summary>
    /// 纹理深度
    /// </summary>
    public uint depth;
    /// <summary>
    /// 纹理类型
    /// </summary>
    public TextureType textureType;
    /// <summary>
    /// 输出层数
    /// </summary>
    public uint mipCount;
    /// <summary>
    /// 2D纹理加载格式
    /// </summary>
    public enum LoadFormat
    {
        LoadFormat_R8G8B8A8_UNorm = 0,
        LoadFormat_R16G16B16A16_UNorm = 1,
        LoadFormat_R16G16B16A16_SFloat = 2,
        LoadFormat_R32G32B32A32_SFloat = 3,
        LoadFormat_R16G16_SFloat = 4,
        LoadFormat_R16G16_UNorm = 5,
        LoadFormat_BC7 = 6,
        LoadFormat_BC6H = 7,
        LoadFormat_R32_UINT = 8,
        LoadFormat_R32G32_UINT = 9,
        LoadFormat_R32G32B32A32_UINT = 10,
        LoadFormat_R16_UNorm = 11,
        LoadFormat_BC5U = 12,
        LoadFormat_BC5S = 13,
        LoadFormat_R16_UINT = 14,
        LoadFormat_R16G16_UINT = 15,
        LoadFormat_R16G16B16A16_UINT = 16,
        LoadFormat_R8_UINT = 17,
        LoadFormat_R8G8_UINT = 18,
        LoadFormat_R8G8B8A8_UINT = 19,
        LoadFormat_R32_SFloat = 20,
        LoadFormat_BC4U = 21,
        LoadFormat_BC4S = 22,
    };
    /// <summary>
    /// 3D纹理加载格式
    /// </summary>
    public enum Tex3DLoadFormat
    {
        LoadFormat_R8G8B8A8_UNorm = LoadFormat.LoadFormat_R8G8B8A8_UNorm,
        LoadFormat_R16G16B16A16_UNorm = LoadFormat.LoadFormat_R16G16B16A16_UNorm,
        LoadFormat_R16G16B16A16_SFloat = LoadFormat.LoadFormat_R16G16B16A16_SFloat,
        LoadFormat_R32G32B32A32_SFloat = LoadFormat.LoadFormat_R32G32B32A32_SFloat,
        LoadFormat_R16G16_SFloat = LoadFormat.LoadFormat_R16G16_SFloat,
        LoadFormat_R16G16_UNorm = LoadFormat.LoadFormat_R16G16_UNorm,
        LoadFormat_R16_UNorm = LoadFormat.LoadFormat_R16_UNorm,
        LoadFormat_R32_SFloat = LoadFormat.LoadFormat_R32_SFloat
    }

    // TODO:
    // Should Have Compress Type here

    /// <summary>
    /// 纹理加载格式
    /// </summary>
    public LoadFormat format;
};

/// <summary>
/// 纹理导出至VirtualEngine
/// </summary>
[System.Serializable]
public unsafe class TextureExporter
{
    /// <summary>
    /// 读取ComputeShader
    /// </summary>
    public ComputeShader readCS;
    /// <summary>
    /// bc6压缩ComputeShader
    /// </summary>
    private ComputeShader bc6Compress;
    /// <summary>
    /// bc7压缩ComputeShader
    /// </summary>
    private ComputeShader bc7Compress;
    /// <summary>
    /// 压缩是否包涵透明通道
    /// </summary>
    public bool useAlphaInCompress = true;
    /// <summary>
    /// 要输出的纹理贴图
    /// </summary>
    public Texture texture;
    /// <summary>
    /// 立方体贴图存放路径
    /// </summary>
    public string path = "Cubemap.vtex";
    /// <summary>
    /// 是否开启MipMap选项
    /// </summary>
    public bool useMipMap = true;
    /// <summary>
    /// 是否将法线转为正的
    /// </summary>
    public bool isNormal = false;
    /// <summary>
    /// 2D纹理加载方式
    /// </summary>
    public TextureData.LoadFormat tex2DFormat = TextureData.LoadFormat.LoadFormat_R16G16B16A16_SFloat;
    /// <summary>
    /// 3D纹理加载方式
    /// </summary>
    public TextureData.Tex3DLoadFormat tex3DFormat = TextureData.Tex3DLoadFormat.LoadFormat_R16G16B16A16_SFloat;
    /// <summary>
    /// 立方体贴图是否压缩
    /// </summary>
    public bool isCubemapCompress = true;
    /// <summary>
    /// DLL指针地址
    /// </summary>
    private ulong textureEpt;

    public TextureExporter()
    {
        readCS = Resources.Load<ComputeShader>("ComputeShader/ReadCubemap");
        bc6Compress = Resources.Load<ComputeShader>("ComputeShader/BC6Compress");
        bc7Compress = Resources.Load<ComputeShader>("ComputeShader/BC7Compress");
    }
    /// <summary>
    /// 输出立方体贴图
    /// </summary>
    void PrintCubemap()
    {
        #region 初始化立方体贴图数据
        float3[][] forwd = new float3[6][];
        for (int i = 0; i < 6; ++i)
            forwd[i] = new float3[4];
        //Forward
        forwd[4][0] = normalize(float3(-1, 1, 1));
        forwd[4][1] = normalize(float3(1, 1, 1));
        forwd[4][2] = normalize(float3(-1, -1, 1));
        forwd[4][3] = normalize(float3(1, -1, 1));
        //Left
        forwd[1][0] = normalize(float3(-1, 1, -1));
        forwd[1][1] = normalize(float3(-1, 1, 1));
        forwd[1][2] = normalize(float3(-1, -1, -1));
        forwd[1][3] = normalize(float3(-1, -1, 1));
        //Back
        forwd[5][0] = normalize(float3(1, 1, -1));
        forwd[5][1] = normalize(float3(-1, 1, -1));
        forwd[5][2] = normalize(float3(1, -1, -1));
        forwd[5][3] = normalize(float3(-1, -1, -1));

        //Right
        forwd[0][0] = normalize(float3(1, 1, 1));
        forwd[0][1] = normalize(float3(1, 1, -1));
        forwd[0][2] = normalize(float3(1, -1, 1));
        forwd[0][3] = normalize(float3(1, -1, -1));

        //up
        forwd[2][0] = normalize(float3(-1, 1, -1));
        forwd[2][1] = normalize(float3(1, 1, -1));
        forwd[2][2] = normalize(float3(-1, 1, 1));
        forwd[2][3] = normalize(float3(1, 1, 1));

        //down
        forwd[3][0] = normalize(float3(-1, -1, 1));
        forwd[3][1] = normalize(float3(1, -1, 1));
        forwd[3][2] = normalize(float3(-1, -1, -1));
        forwd[3][3] = normalize(float3(1, -1, -1));
        #endregion

        // 立方体贴图最大size (限制为最小1024*1024)
        uint2 size = uint2(max((uint)texture.width, 1024), max((uint)texture.height, 1024));
        
        // 立方体贴图压缩
        if (isCubemapCompress)
        {
            // 命令缓冲区
            CommandBuffer cb = new CommandBuffer();
            int mipCount = useMipMap ? (int)(log2(size.x / 8) + 0.1) : 1;
            // 限制最大层为7层
            mipCount = min(mipCount, 7);
            // 设置计算纹理参数
            cb.SetComputeTextureParam(readCS, 5, "_MainTex", texture);
            #region 纹理贴图数据初始化
            TextureData data = new TextureData
            {
                depth = 6,
                width = size.x,
                height = size.y,
                mipCount = (uint)mipCount,
                format = TextureData.LoadFormat.LoadFormat_BC6H,
                textureType = TextureType.Cubemap
            };
            #endregion

            DLLTextureExporter.DataInit(textureEpt, (ulong)sizeof(TextureData), (uint)(size.x * size.y * 16), (byte*)data.Ptr());
            // GPU压缩
            GPUCompress[] gpuCompress = new GPUCompress[mipCount];
            RenderTexture[] rts = new RenderTexture[mipCount];
            // int2 = Vector2Int
            int2 curSize = (int2)size;
            for (int i = 0; i < mipCount; ++i)
            {
                gpuCompress[i] = new GPUCompress(bc7Compress, bc6Compress, curSize.x, curSize.y, 0x80000, useAlphaInCompress ? 1 : 0);
                rts[i] = new RenderTexture(new RenderTextureDescriptor
                {
                    width = curSize.x,
                    height = curSize.y,
                    volumeDepth = 1,
                    dimension = TextureDimension.Tex2D,
                    enableRandomWrite = true,
                    graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                    msaaSamples = 1
                });
                rts[i].Create();
                curSize /= 2;
            }
            Vector4[] setterArray = new Vector4[4];
            uint4[] datas = new uint4[size.x * size.y];

            for (int face = 0; face < 6; ++face)
            {
                size = uint2(max((uint)texture.width, 1024), max((uint)texture.height, 1024));
                for (int i = 0; i < mipCount; ++i)
                {
                    cb.SetComputeIntParam(readCS, "_TargetMipLevel", i);
                    cb.SetComputeIntParam(readCS, "_Count", (int)size.x);
                    cb.SetComputeTextureParam(readCS, 5, "_DestTex", rts[i]);
                    for (int j = 0; j < 4; ++j)
                    {
                        setterArray[j] = (Vector3)forwd[face][j];
                    }
                    cb.SetComputeVectorArrayParam(readCS, "_Directions", setterArray);
                    cb.DispatchCompute(readCS, 5, max(1, Mathf.CeilToInt(size.x / 8f)), max(1, Mathf.CeilToInt(size.y / 8f)), 1);
                    gpuCompress[i].Compress(rts[i], rts[i].width, rts[i].height, 0, cb, true);
                    Graphics.ExecuteCommandBuffer(cb);
                    cb.Clear();
                    int len = gpuCompress[i].GetData(datas);
                    ulong xblocks = (ulong)max(1, (size.x + 3) >> 2);
                    ulong yblocks = (ulong)max(1, (size.y + 3) >> 2);
                    DLLTextureExporter.DataTransfer(
                        textureEpt,
                        new uint2((uint)xblocks, (uint)yblocks),
                        (byte*)datas.Ptr(),
                        DLLTextureExporter.DataType.Bit32,
                        4);
                    size /= 2;
                }
            }

            for (int i = 0; i < mipCount; ++i)
            {
                gpuCompress[i].Dispose();
                UnityEngine.Object.DestroyImmediate(rts[i]);
            }
            // 销毁命令缓冲区
            cb.Dispose();
        }
        else // 立方体贴图不压缩
        {
            ComputeBuffer cb = new ComputeBuffer((int)size.x * (int)size.y, sizeof(float4), ComputeBufferType.Default);
            int mipCount = useMipMap ? (int)(log2(size.x / 4) + 0.1) : 1;
            mipCount = min(mipCount, 7);
            #region 纹理贴图数据初始化
            TextureData data = new TextureData
            {
                depth = 6,
                width = size.x,
                height = size.y,
                mipCount = (uint)mipCount,
                format = TextureData.LoadFormat.LoadFormat_R16G16B16A16_SFloat,
                textureType = TextureType.Cubemap
            };
            #endregion
            readCS.SetTexture(0, "_MainTex", texture);
            readCS.SetBuffer(0, "_ResultBuffer", cb);
            float4[] readbackValues = new float4[size.x * size.y];
            NativeList<byte> lst = new NativeList<byte>((int)(size.x * size.y * 1.4), Unity.Collections.Allocator.Temp);
            byte* headerPtr = (byte*)data.Ptr();
            lst.AddRange(headerPtr, sizeof(TextureData));
            Vector4[] setterArray = new Vector4[4];
            for (int face = 0; face < 6; ++face)
            {
                size = uint2(max((uint)texture.width, 1024), max((uint)texture.height, 1024));
                for (int i = 0; i < mipCount; ++i)
                {
                    readCS.SetInt("_TargetMipLevel", i);
                    readCS.SetInt("_Count", (int)size.x);
                    for (int j = 0; j < 4; ++j)
                    {
                        setterArray[j] = (Vector3)forwd[face][j];
                    }
                    readCS.SetVectorArray("_Directions", setterArray);
                    readCS.Dispatch(0, max(1, (7 + (int)size.x) / 8), max(1, (7 + (int)size.y) / 8), 1);
                    int cum = (int)(size.x * size.y);
                    cb.GetData(readbackValues, 0, 0, cum);
                    DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Half,
                                4);
                    size /= 2;
                    size = max(size, 1);
                }
            }
        }
        ExportFinalFile();
    }

    /// <summary>
    /// 获取每个像素所占大小
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private uint GetPixelSize(TextureData.LoadFormat type, uint width, uint height)
    {
        uint pixelSize = width * height;
        switch (type)
        {
            case TextureData.LoadFormat.LoadFormat_R8_UINT:
            case TextureData.LoadFormat.LoadFormat_BC5S:
            case TextureData.LoadFormat.LoadFormat_BC5U:
            case TextureData.LoadFormat.LoadFormat_BC6H:
            case TextureData.LoadFormat.LoadFormat_BC7:
                return pixelSize;
            case TextureData.LoadFormat.LoadFormat_BC4S:
            case TextureData.LoadFormat.LoadFormat_BC4U:
                return pixelSize / 2;
            case TextureData.LoadFormat.LoadFormat_R16_UNorm:
            case TextureData.LoadFormat.LoadFormat_R16_UINT:
            case TextureData.LoadFormat.LoadFormat_R8G8_UINT:
                return pixelSize * 2;
            case TextureData.LoadFormat.LoadFormat_R32_SFloat:
            case TextureData.LoadFormat.LoadFormat_R8G8B8A8_UINT:
            case TextureData.LoadFormat.LoadFormat_R16G16_UINT:
            case TextureData.LoadFormat.LoadFormat_R32_UINT:
            case TextureData.LoadFormat.LoadFormat_R16G16_SFloat:
            case TextureData.LoadFormat.LoadFormat_R16G16_UNorm:
            case TextureData.LoadFormat.LoadFormat_R8G8B8A8_UNorm:
                return pixelSize * 4;
            case TextureData.LoadFormat.LoadFormat_R16G16B16A16_UNorm:
            case TextureData.LoadFormat.LoadFormat_R16G16B16A16_SFloat:
            case TextureData.LoadFormat.LoadFormat_R32G32_UINT:
            case TextureData.LoadFormat.LoadFormat_R16G16B16A16_UINT:
                return pixelSize * 8;
            case TextureData.LoadFormat.LoadFormat_R32G32B32A32_UINT:
            case TextureData.LoadFormat.LoadFormat_R32G32B32A32_SFloat:
                return pixelSize * 16;
        }
        return 0;
    }

    /// <summary>
    /// 限制mipCount最小为1
    /// </summary>
    /// <param name="size"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private int GetMipCount(uint2 size, TextureData.LoadFormat type)
    {
        int mipCount = 0;
        long fileSize = 0;
        while (true)
        {
            fileSize = GetPixelSize(type, size.x, size.y);
            if (fileSize < 65536)
                return max(mipCount,1);
            mipCount++;
            size /= 2;
            size = max(size, 1);
        }
    }

    /// <summary>
    /// 输出2D纹理
    /// </summary>
    void PrintTex2D()
    {
        #region 纹理贴图数据初始化
        TextureData data;
        uint2 size = uint2((uint)texture.width, (uint)texture.height);
        //int mipCount = useMipMap ? min(min((int)(log2(size.x / 2) + 0.1), (int)(log2(size.y / 2) + 0.1)), 10) : 1;
        int mipCount = useMipMap ? GetMipCount(size, tex2DFormat) : 1;
        data.width = size.x;
        data.height = size.y;
        data.depth = 1;
        data.textureType = TextureType.Tex2D;
        data.mipCount = (uint)mipCount;
        data.format = tex2DFormat;
        #endregion

        // 数据初始化导入
        DLLTextureExporter.DataInit(textureEpt, (ulong)sizeof(TextureData), (uint)(size.x * size.y * 16), (byte*)data.Ptr());
        uint2 finalSize = new uint2();
        if (tex2DFormat == TextureData.LoadFormat.LoadFormat_BC6H)
        {
            uint4[] datas = new uint4[size.x * size.y];
            CommandBuffer cbuffer = new CommandBuffer();
            GPUCompress compress = new GPUCompress(bc7Compress, bc6Compress, (int)size.x, (int)size.y, 0x80000, useAlphaInCompress ? 1 : 0);
            for (int i = 0; i < mipCount; ++i)
            {
                compress.Compress(texture, (int)size.x, (int)size.y, i, cbuffer, true);
                Graphics.ExecuteCommandBuffer(cbuffer);
                compress.GetData(datas);
                finalSize.x = max(1, (size.x + 3) >> 2);
                finalSize.y = max(1, (size.y + 3) >> 2);

                // 数据传输
                DLLTextureExporter.DataTransfer(
                        textureEpt,
                        finalSize,
                        (byte*)datas.Ptr(),
                        DLLTextureExporter.DataType.Bit32,
                        4);

                size /= 2;
                size = max(size, 1);
            }
            compress.Dispose();
            cbuffer.Dispose();
        }
        else if (tex2DFormat == TextureData.LoadFormat.LoadFormat_BC7)
        {
            uint4[] datas = new uint4[size.x * size.y];
            CommandBuffer cbuffer = new CommandBuffer();
            GPUCompress compress = new GPUCompress(bc7Compress, bc6Compress, (int)size.x, (int)size.y, 0x80000, 0);
            for (int i = 0; i < mipCount; ++i)
            {
                compress.Compress(texture, (int)size.x, (int)size.y, i, cbuffer, false);
                Graphics.ExecuteCommandBuffer(cbuffer);

                finalSize.x = max(1, (size.x + 3) >> 2);
                finalSize.y = max(1, (size.y + 3) >> 2);
                compress.GetData(datas);

                // 数据传输
                DLLTextureExporter.DataTransfer(
                        textureEpt,
                        finalSize,
                        (byte*)datas.Ptr(),
                        DLLTextureExporter.DataType.Bit32,
                        4);

                size /= 2;
                size = max(size, 1);
            }
            compress.Dispose();
            cbuffer.Dispose();
        }
        else if (((int)tex2DFormat >= 8 && (int)tex2DFormat <= 10) || ((int)tex2DFormat >= 14 && (int)tex2DFormat <= 19))
        {
            //integer texture
            data.mipCount = 1;
            mipCount = 1;
            int pass = 0;
            uint[] dataArray = null;
            readCS.SetVector("_TextureSize", float4(size.x - 0.5f, size.y - 0.5f, size.x, size.y));
            readCS.SetInt("_Count", (int)size.x);
            ComputeBuffer cb = null;
            switch (tex2DFormat)
            {
                case TextureData.LoadFormat.LoadFormat_R8_UINT:
                case TextureData.LoadFormat.LoadFormat_R16_UINT:
                case TextureData.LoadFormat.LoadFormat_R32_UINT:
                    pass = 2;
                    dataArray = new uint[size.x * size.y];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UIntTexture", texture);
                    readCS.SetBuffer(pass, "_ResultInt1Buffer", cb);
                    break;
                case TextureData.LoadFormat.LoadFormat_R8G8_UINT:
                case TextureData.LoadFormat.LoadFormat_R16G16_UINT:
                case TextureData.LoadFormat.LoadFormat_R32G32_UINT:
                    pass = 3;
                    dataArray = new uint[size.x * size.y * 2];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UInt2Texture", texture);
                    readCS.SetBuffer(pass, "_ResultInt2Buffer", cb);
                    break;
                case TextureData.LoadFormat.LoadFormat_R8G8B8A8_UINT:
                case TextureData.LoadFormat.LoadFormat_R16G16B16A16_UINT:
                case TextureData.LoadFormat.LoadFormat_R32G32B32A32_UINT:
                    pass = 4;
                    dataArray = new uint[size.x * size.y * 4];
                    cb = new ComputeBuffer(dataArray.Length, sizeof(int));
                    readCS.SetTexture(pass, "_UInt4Texture", texture);
                    readCS.SetBuffer(pass, "_ResultInt4Buffer", cb);
                    break;
            }
            readCS.Dispatch(pass, (int)size.x / 8, (int)size.y / 8, 1);
            cb.GetData(dataArray);
            //32 bit
            if ((int)tex2DFormat >= 8 && (int)tex2DFormat <= 10)
            {
                // 数据传输
                DLLTextureExporter.DataTransferUINT(textureEpt, (uint)dataArray.Length, (byte*)dataArray.Ptr(), DLLTextureExporter.DataType.Bit32);
            }
            else if ((int)tex2DFormat >= 14 && (int)tex2DFormat <= 16)//16 Bit
            {
                // 数据传输
                DLLTextureExporter.DataTransferUINT(textureEpt, (uint)dataArray.Length, (byte*)dataArray.Ptr(), DLLTextureExporter.DataType.Bit16);
            }
            else //8 Bit
            {
                // 数据传输
                DLLTextureExporter.DataTransferUINT(textureEpt, (uint)dataArray.Length, (byte*)dataArray.Ptr(), DLLTextureExporter.DataType.Bit8);
            }
            cb.Dispose();
        }
        else
        {
            ComputeBuffer cb = new ComputeBuffer((int)size.x * (int)size.y, sizeof(float4), ComputeBufferType.Default);
            int pass = isNormal ? 6 : 1;
            readCS.SetTexture(pass, "_MainTex2D", texture);
            readCS.SetBuffer(pass, "_ResultBuffer", cb);
            float4[] readbackValues = new float4[size.x * size.y];
            float* byteArray = stackalloc float[4];
            for (int i = 0; i < mipCount; ++i)
            {
                readCS.SetVector("_TextureSize", float4(size.x - 0.5f, size.y - 0.5f, size.x, size.y));
                readCS.SetInt("_Count", (int)size.x);
                readCS.SetInt("_TargetMipLevel", i);
                readCS.Dispatch(pass, max(1, Mathf.CeilToInt(size.x / 8f)), max(1, Mathf.CeilToInt(size.y / 8f)), 1);
                int cum = (int)(size.x * size.y);
                cb.GetData(readbackValues, 0, 0, cum);
                switch (tex2DFormat)
                {
                    case TextureData.LoadFormat.LoadFormat_R16G16B16A16_SFloat:
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Half,
                                4);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R16G16_SFloat:
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Half,
                                2);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R32G32B32A32_SFloat:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit32,
                                4);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R16G16B16A16_UNorm:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit16,
                                4);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R16G16_UNorm:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit16,
                                2);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R8G8B8A8_UNorm:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit8,
                                4);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R16_UNorm:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit16,
                                1);
                        break;
                    case TextureData.LoadFormat.LoadFormat_R32_SFloat:
                        // 数据传输
                        DLLTextureExporter.DataTransfer(
                                textureEpt,
                                size,
                                (byte*)readbackValues.Ptr(),
                                DLLTextureExporter.DataType.Bit32,
                                1);
                        break;
                    case TextureData.LoadFormat.LoadFormat_BC5U:
                    case TextureData.LoadFormat.LoadFormat_BC5S:
                        {
                            NativeArray<byte> compressedData = new NativeArray<byte>(cum, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            CompressJob job;
                            job.compressType = tex2DFormat;
                            job.dest = (uint2*)compressedData.GetUnsafePtr();
                            job.source = readbackValues.Ptr();
                            job.width = (int)size.x;
                            JobHandle handle = job.Schedule((cum / 16), max((cum / 16) / 20, 1));
                            handle.Complete();
                            finalSize.x = max(1, (size.x + 3) >> 2);
                            finalSize.y = max(1, (size.y + 3) >> 2);
                            // 数据传输
                            DLLTextureExporter.DataTransfer(
                                    textureEpt,
                                    finalSize,
                                    (byte*)job.dest,
                                    DLLTextureExporter.DataType.Bit32,
                                    4);
                        }
                        break;
                    case TextureData.LoadFormat.LoadFormat_BC4S:
                    case TextureData.LoadFormat.LoadFormat_BC4U:
                        {
                            NativeArray<byte> compressedData = new NativeArray<byte>(cum / 2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                            CompressJob job;
                            job.compressType = tex2DFormat;
                            job.dest = (uint2*)compressedData.GetUnsafePtr();
                            job.source = readbackValues.Ptr();
                            job.width = (int)size.x;
                            JobHandle handle = job.Schedule((cum / 16), max((cum / 16) / 20, 1));
                            handle.Complete();
                            finalSize.x = max(1, (size.x + 3) >> 2);
                            finalSize.y = max(1, (size.y + 3) >> 2);
                            DLLTextureExporter.DataTransfer(
                                    textureEpt,
                                    finalSize,
                                    (byte*)job.dest,
                                    DLLTextureExporter.DataType.Bit32,
                                    2);
                        }
                        break;
                }

                size /= 2;
                size = max(size, 1);
            }
            cb.Dispose();
        }
        ExportFinalFile();
    }

    /// <summary>
    /// 输出3D纹理
    /// </summary>
    void PrintTex3D()
    {
        #region 纹理贴图数据初始化
        TextureData texData;
        RenderTexture tex3D = texture as RenderTexture;
        texData.format = (TextureData.LoadFormat)tex3DFormat;
        texData.depth = (uint)tex3D.volumeDepth;
        texData.height = (uint)tex3D.height;
        texData.width = (uint)tex3D.width;
        texData.textureType = TextureType.Tex3D;
        texData.mipCount = useMipMap ? (uint)(tex3D.mipmapCount + 1) : 1;
        #endregion

        // 3D纹理读取的运算单元
        const int tex3dReadKernel = 7;
        ComputeBuffer resultBuffer = new ComputeBuffer(tex3D.volumeDepth * tex3D.width * tex3D.height, sizeof(float4));
        readCS.SetBuffer(tex3dReadKernel, "_ResultBuffer", resultBuffer);
        float4[] data = new float4[resultBuffer.count];

        // ============================
        NativeList<byte> lst = new NativeList<byte>((int)(tex3D.width * tex3D.height * tex3D.volumeDepth * sizeof(float4) + sizeof(TextureData)), Unity.Collections.Allocator.Temp);
        lst.AddRange((byte*)texData.Ptr(), sizeof(TextureData));
        // ============================

        int3 size = int3(tex3D.width, tex3D.height, tex3D.volumeDepth);

        for (int mip = 0; mip < texData.mipCount; ++mip)
        {
            readCS.SetTexture(tex3dReadKernel, "_VoxelTex", tex3D);
            readCS.SetVector("_TextureSize", new Vector4(size.x, size.y, size.z, 1));
            readCS.Dispatch(tex3dReadKernel, (size.x + 3) / 4, (size.y + 3) / 4, (size.z + 3) / 4);
            int currentLen = size.x * size.y * size.z;
            resultBuffer.GetData(data, 0, 0, currentLen);
            float* byteArray = stackalloc float[4];

            // TODO: 整合到C++
            switch (tex3DFormat)
            {
                case TextureData.Tex3DLoadFormat.LoadFormat_R16G16B16A16_SFloat:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    half4 value = (half4)data[i];
                                    lst.AddRange((byte*)value.Ptr(), sizeof(half4));
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(half4));
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R16G16B16A16_UNorm:
                    {

                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    ushort* value = (ushort*)byteArray;
                                    value[0] = (ushort)(data[i].x * 65535);
                                    value[1] = (ushort)(data[i].y * 65535);
                                    value[2] = (ushort)(data[i].z * 65535);
                                    value[3] = (ushort)(data[i].w * 65535);
                                    lst.AddRange((byte*)value, sizeof(ushort) * 4);
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(ushort) * 4);
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R16G16_SFloat:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    half2 value = (half2)data[i].xy;
                                    lst.AddRange((byte*)value.Ptr(), sizeof(half2));
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(half2));
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R16G16_UNorm:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    ushort* value = (ushort*)byteArray;
                                    value[0] = (ushort)(data[i].x * 65535);
                                    value[1] = (ushort)(data[i].y * 65535);
                                    lst.AddRange((byte*)value, sizeof(ushort) * 2);
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(ushort) * 2);
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R16_UNorm:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    ushort* value = (ushort*)byteArray;
                                    value[0] = (ushort)(data[i].x * 65535);
                                    lst.AddRange((byte*)value, sizeof(ushort));
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(ushort));
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R32G32B32A32_SFloat:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    ref float4 value = ref data[i];
                                    lst.AddRange((byte*)value.Ptr(), sizeof(float4));
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(float4));
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R8G8B8A8_UNorm:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    byte* value = (byte*)byteArray;
                                    value[0] = (byte)(data[i].x * 255);
                                    lst.AddRange(value, 1);
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x);
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
                case TextureData.Tex3DLoadFormat.LoadFormat_R32_SFloat:
                    {
                        for (int z = 0, i = 0; z < size.z; ++z)
                            for (int y = 0; y < size.y; ++y)
                            {
                                for (int x = 0; x < size.x; ++x)
                                {
                                    ref float value = ref data[i].x;
                                    lst.AddRange((byte*)value.Ptr(), sizeof(float));
                                    i++;
                                }
                                int alignSize = max(0, 256 - size.x * sizeof(float));
                                lst.AddRange(alignSize);
                            }
                    }
                    break;
            }
            size /= 2;
        }

        byte[] finalArray = new byte[lst.Length];
        UnsafeUtility.MemCpy(finalArray.Ptr(), lst.unsafePtr, lst.Length);
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            fs.Write(finalArray, 0, lst.Length);
        }
        resultBuffer.Dispose();
    }

    /// <summary>
    /// 输出最终文件
    /// </summary>
    private void ExportFinalFile()
    {
        DLLTextureExporter.ExportFile(textureEpt, path.Ptr());
    }

    public void Print()
    {
        DLLTextureExporter.TextureEptInit(textureEpt.Ptr());
        if (texture.dimension == TextureDimension.Cube)
            PrintCubemap();
        if (texture.dimension == TextureDimension.Tex2D)
            PrintTex2D();
        if (texture.dimension == TextureDimension.Tex3D)
            PrintTex3D();
        DLLTextureExporter.TextureEptDispose(textureEpt);
    }
}
