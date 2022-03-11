using System.Runtime.InteropServices;
using Unity.Mathematics;

public unsafe static class DLLToJson
{
    [DllImport("DLL_Tool")] public static extern void ToJsonInit(ulong* ptr);
    [DllImport("DLL_Tool")] public static extern void ToJsonInit_Path(ulong* ptr, char* str);
    [DllImport("DLL_Tool")] public static extern void ToJsonDispose(ulong ptr);
    [DllImport("DLL_Tool")] public static extern void ToJsonExportFile(ulong handle, char* path);
    [DllImport("DLL_Tool")] public static extern void ToJsonExportSerializedFile(ulong handle, char* path);

    [DllImport("DLL_Tool")] private static extern void ToJsonAddInt(ulong handle, char* key, int* value, uint valueCount);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddFloat(ulong handle, char* key, float* value, uint valueCount);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddDouble(ulong handle, char* key, double* value, uint valueCount);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddString(ulong handle, char* key, char* value);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddJsonKeyValue(ulong handle, char* key, ulong value);

    [DllImport("DLL_Tool")] public static extern ulong ToJsonGetPtr(ulong handle);

    [DllImport("DLL_Tool")] private static extern void ToJsonAddJson(ulong handle, ulong jsonPtr);

    [DllImport("DLL_Tool")] private static extern void DeleteJsonKey(ulong handle, char* key);


    [DllImport("DLL_Tool")] private static extern void ToJsonAddTransform(ulong handle, JsonTransform* tr);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddRenderer(ulong handle, JsonRenderer* renderer);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddSkinnedRenderer(ulong handle, JsonSkinnedRenderer* renderer);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddAnimClip(ulong handle, JsonAnimClip* clip);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddLight(ulong handle, JsonLight* light);
    [DllImport("DLL_Tool")] private static extern void ToJsonAddReflectionProbe(ulong handle, JsonReflectionProbe* rp);

    public static void ToJsonAdd(ulong handle, string key, int* value, uint valueCount = 1)
    {
        ToJsonAddInt(handle, key.Ptr(), value, valueCount);
    }

    public static void ToJsonAdd(ulong handle, string key, float* value, uint valueCount = 1)
    {
        ToJsonAddFloat(handle, key.Ptr(), value, valueCount);
    }

    public static void ToJsonAdd(ulong handle, string key, double* value, uint valueCount = 1)
    {
        ToJsonAddDouble(handle, key.Ptr(), value, valueCount);
    }

    public static void ToJsonAdd(ulong handle, string key, string value)
    {
        ToJsonAddString(handle, key.Ptr(), value.Ptr());
    }
    public static void ToJsonAddKeyValue(ulong handle, string key, ulong cjson)
    {
        ToJsonAddJsonKeyValue(handle, key.Ptr(), cjson);
    }
    public static void ToJsonAdd(ulong handle, JsonTransform* tr)
    {
        ToJsonAddTransform(handle, tr);
    }

    public static void ToJsonAdd(ulong handle, JsonRenderer* renderer)
    {
        ToJsonAddRenderer(handle, renderer);
    }

    public static void ToJsonAdd(ulong handle, JsonSkinnedRenderer* renderer)
    {
        ToJsonAddSkinnedRenderer(handle, renderer);
    }

    public static void ToJsonAdd(ulong handle, JsonAnimClip* clip)
    {
        ToJsonAddAnimClip(handle, clip);
    }

    public static void ToJsonAdd(ulong handle, JsonLight* light)
    {
        ToJsonAddLight(handle, light);
    }

    public static void ToJsonAdd(ulong handle, JsonReflectionProbe* rp)
    {
        ToJsonAddReflectionProbe(handle, rp);
    }

    public static void DeleteJsonKey(ulong handle, string str)
    {
        DeleteJsonKey(handle, str.Ptr());
    }

    public static void ToJsonAdd(ulong handle, ulong jsonPtr)
    {
        ToJsonAddJson(handle, jsonPtr);
    }
}
