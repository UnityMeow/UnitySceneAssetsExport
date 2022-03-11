using System.Runtime.InteropServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public unsafe static class DLLTextureExporter
{
    public enum DataType
    {
        Bit8 = 0,
        Bit16 = 1,
        Bit32 = 2,
        Half = 3,
    };

    // 初始化
    [DllImport("DLL_Tool")] public static extern void TextureEptInit(ulong* ptr);
    // 释放
    [DllImport("DLL_Tool")] public static extern void TextureEptDispose(ulong ptr);
    // 数据初始化
    [DllImport("DLL_Tool")] public static extern void DataInit(ulong handle, ulong header, ulong size, byte* dataPtr);
    // 数据传递
    [DllImport("DLL_Tool")] public static extern void DataTransfer(ulong handle, uint2 blocks, byte* datas, DataType type, uint passCount);
    // 数据传递
    [DllImport("DLL_Tool")] public static extern void DataTransferUINT(ulong handle, uint size, byte* datas, DataType type);
    // 导出文件
    [DllImport("DLL_Tool")] public static extern void ExportFile(ulong handle, char* path);
}

public unsafe static class DLLMeshExporter
{
    [DllImport("DLL_Tool")] public static extern void MeshExportFile(byte* head, long headCount, byte* data, long dataCount, char* path);
}