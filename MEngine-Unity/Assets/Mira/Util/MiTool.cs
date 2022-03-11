using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public unsafe static class MiTool
{
    /// <summary>
    /// 文件写入
    /// </summary>
    /// <param name="filePtr"></param>
    /// <param name="copyPtr"></param>
    /// <param name="size"></param>
    public static void WriteByte(ref byte* filePtr, void* copyPtr, int size)
    {
        UnsafeUtility.MemCpy(filePtr, copyPtr, size);
        filePtr += size;
    }

    [DllImport("DLL_Tool")] public static extern void AutoCreatFolder(char* ptr);
}
