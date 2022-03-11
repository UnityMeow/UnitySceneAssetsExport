using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public enum CullingMask
{
    None = 0,
    ALL = -1,
    GEOMETRY = 1,
    CASCADE_SHADOWMAP = 2,
    SPOT_SHADOWMAP = 4,
    CUBE_SHADOWMAP = 8
};

/// <summary>
/// 导出场景所需的组件类型
/// </summary>
public struct ComponentInfo
{
    public Transform tr;
    public MeshFilter filter;
    public MeshRenderer renderer;
    public Light lit;
    public ReflectionProbe rp;
    public SkinnedMeshRenderer skinned;
    public Animator animator;
    public Animation animation;
}


public unsafe class MiSceneExporter : EditorWindow
{
   
    /// <summary>
    /// 引擎根目录
    /// </summary>
    public static string EnginePath;
    /// <summary>
    /// 资源导出路径
    /// </summary>
    public static string ExportPath = "Resource/Assets";
    /// <summary>
    /// JSON导出路径
    /// </summary>
    public static string JSONPath = "Resource";
    /// <summary>
    /// 要导出资源的Gameobject
    /// </summary>
    public static Dictionary<int, GameObject> ExporterGameObject = new Dictionary<int, GameObject>();

    /// <summary>
    /// 最终要导出的资源
    /// </summary>
    private static Dictionary<string, string> ExporterAsset = new Dictionary<string, string>();
    /// <summary>
    /// 层级窗口回调
    /// </summary>
    private static readonly EditorApplication.HierarchyWindowItemCallback hiearchyItemCallback;
    // private static readonly EditorApplication.ProjectWindowItemCallback projectItemCallback;

    /// <summary>
    /// 图标
    /// </summary>
    private static Texture2D ExporterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Res/ToolMeow/icon_01.png");

    private static MaterialExporter matEx = new MaterialExporter();
    private static MeshExporter meshEx = new MeshExporter();
    private static TextureExporter texEx = new TextureExporter();
    private static AnimClipExporter clipEx = new AnimClipExporter();

    private static MiJson assetsDataJson;
    private static MiJson scenesDataJson;
    private static MiJson xXSceneDataJson;

    private static bool isExporter;

    /// <summary>
    /// 静态构造
    /// </summary>
    static MiSceneExporter()
    {
        //初始化层级窗口项回调
        hiearchyItemCallback = new EditorApplication.HierarchyWindowItemCallback(IconsOnGUIHie);
        // projectItemCallback = new EditorApplication.ProjectWindowItemCallback(IconsOnGUIPro);
        //加到委托列表
        EditorApplication.hierarchyWindowItemOnGUI = (EditorApplication.HierarchyWindowItemCallback)Delegate.Combine(
            EditorApplication.hierarchyWindowItemOnGUI,
            hiearchyItemCallback);
        //EditorApplication.projectWindowItemOnGUI = (EditorApplication.ProjectWindowItemCallback)Delegate.Combine(
        //    EditorApplication.projectWindowItemOnGUI,
        //    projectItemCallback);
    }

    internal static void IconsOnGUIHie(int instanceId, Rect selectionRect)
    {
        // 绘制图标
        if (ExporterGameObject.ContainsKey(instanceId))
        {
            //生成放图片的矩形
            Rect r = new Rect(selectionRect);
            //矩形赋值
            r.x = 250;
            r.width = 20;
            //GUI绘制
            GUI.Label(r, ExporterIcon);
        }
    }

    //internal static void IconsOnGUIPro(string instanceId, Rect selectionRect)
    //{
    //    // 绘制图标
    //    if (ExporterAsset.ContainsKey(instanceId))
    //    {
    //        //生成放图片的矩形
    //        Rect r = new Rect(selectionRect);
    //        //矩形赋值
    //        r.x = 250;
    //        r.width = 20;
    //        //GUI绘制
    //        GUI.Label(r, ExporterIcon);
    //    }
    //}

    [MenuItem("GameObject/喵：标记", false, 11)]
    private static void SetExporter()
    {
        GameObject[] gos = Selection.gameObjects;
        int[] ids = Selection.instanceIDs;
        for (int i = 0; i < ids.Length; i++)
        {
            if (!ExporterGameObject.ContainsKey(ids[i]))
                ExporterGameObject.Add(ids[i], gos[i]);
        }
    }

    [MenuItem("GameObject/喵：取消", false, 12)]
    private static void UnsetExporter()
    {
        int[] ids = Selection.instanceIDs;
        for (int i = 0; i < ids.Length; i++)
        {
            if (ExporterGameObject.ContainsKey(ids[i]))
                ExporterGameObject.Remove(ids[i]);
        }
    }

    /// <summary>
    /// 场景导出
    /// </summary>
    public static void Exporter()
    {
        string databaseExportPath = EnginePath + '/' + JSONPath + "/AssetsDatabase.json";
        string sceneDataExportPath = EnginePath + '/' + JSONPath + "/ScenesData.json";
        assetsDataJson = new MiJson(databaseExportPath);
        scenesDataJson = new MiJson(sceneDataExportPath);
        xXSceneDataJson = new MiJson();
        Dictionary<int, GameObject>.Enumerator enumerator
                = ExporterGameObject.GetEnumerator();
        EditorUtility.DisplayDialog("喵喵提示", "场景准备进行导出，可能会出现卡顿，请勿进行任何操作~", "好的我不动!");
        while (enumerator.MoveNext())
        {
            isExporter = false;
            string goName = enumerator.Current.Value.name;
            string jsonPath = EnginePath + '/' + JSONPath + '/' + goName + ".json";
            xXSceneDataJson.Init();
            FindChild(enumerator.Current.Value.transform);
            if (isExporter)
            {
                xXSceneDataJson.Exporter(jsonPath);
                scenesDataJson.DeleteKey(goName);
                scenesDataJson.AddKey(goName, SplitPath(jsonPath));
            }
            else
            {
                xXSceneDataJson.Dispose();
            }
        }
        assetsDataJson.Exporter(databaseExportPath);
        scenesDataJson.Exporter(sceneDataExportPath);
        ExporterAsset.Clear();
        EditorUtility.DisplayDialog("喵喵提示", $"所有场景已导出成功，可以动了!", "好哒");
    }

    /// <summary>
    /// 递归遍历所有子物体
    /// 载入相关数据
    /// </summary>
    private static void FindChild(Transform father)
    {
        ComponentInfo componentInfo = new ComponentInfo()
        {
            tr = father,
            filter = father.GetComponent<MeshFilter>(),
            renderer = father.GetComponent<MeshRenderer>(),
            lit = father.GetComponent<Light>(),
            rp = father.GetComponent<ReflectionProbe>(),
            skinned = father.GetComponent<SkinnedMeshRenderer>(),
            animator = father.GetComponent<Animator>(),
            animation = father.GetComponent<Animation>()
        };
        if (
            (componentInfo.rp == null
            && componentInfo.lit == null
            && (componentInfo.filter == null || componentInfo.renderer == null)
            && componentInfo.skinned == null
            && componentInfo.animator == null
            && componentInfo.animation == null)
            || (componentInfo.filter != null && componentInfo.filter.sharedMesh == null)
            || (componentInfo.renderer != null && componentInfo.renderer.sharedMaterial == null)
            || (componentInfo.renderer != null && componentInfo.renderer.sharedMaterial != null && componentInfo.renderer.sharedMaterial.shader.name != "ShouShouPBR"
            && componentInfo.renderer.sharedMaterial.shader.name != "Unreal/Rock")
            ) { }
        else
        {
            // 载入数据
            LoadData(componentInfo);
        }

        if (father.childCount == 0)
            return;

        int len = father.childCount;
        for (int i = 0; i < len; i++)
        {
            FindChild(father.GetChild(i));
        }
    }

    /// <summary>
    /// 载入相关数据
    /// </summary>
    private static void LoadData(ComponentInfo componentInfo)
    {
        MiJson jsonData = new MiJson();
        Transform tr = componentInfo.tr;
        jsonData.AddKey("name", tr.gameObject.name);
        #region Json数据载入
        JsonTransform jtr = new JsonTransform();
        jtr.position = tr.position;
        jtr.rotation = new float4(tr.rotation.x, tr.rotation.y, tr.rotation.z, tr.rotation.w);
        jtr.localscale = tr.localScale;
        jsonData.AddKey(jtr);
        #endregion
        ExporterRenderer(tr, componentInfo.skinned, componentInfo.renderer, componentInfo.filter, ref jsonData);
        ExporterLight(tr, componentInfo.lit, ref jsonData);
        ExporterRP(componentInfo.rp, ref jsonData);
        ExporterAnimClip(componentInfo, ref jsonData);
        xXSceneDataJson.AddJson(jsonData.GetJsonPtr());
        jsonData.Dispose();
    }

    private static void ExporterLight(Transform tr, Light lit, ref MiJson jsonData)
    {
        if (lit != null && (lit.type == LightType.Point || lit.type == LightType.Spot))
        {
            isExporter = true;
            #region Json数据载入
            JsonLight jlt = new JsonLight();
            jlt.lightType = lit.type == LightType.Point ? 0 : 1;
            jlt.intensity = lit.intensity;
            jlt.shadowBias = lit.shadowBias;
            jlt.shadowNearPlane = lit.shadowNearPlane;
            jlt.range = lit.range;
            jlt.angle = lit.spotAngle;
            jlt.color = new float4(lit.color.r, lit.color.g, lit.color.b, lit.color.a);
            jlt.smallAngle = lit.innerSpotAngle;
            jlt.useShadow = lit.shadows != LightShadows.None ? 1 : 0;
            jlt.shadowCache = tr.gameObject.isStatic ? 1 : 0;
            jsonData.AddKey(jlt);
            #endregion
        }
    }

    private static void ExporterRP(ReflectionProbe rp, ref MiJson jsonData)
    {
        if (rp != null && rp.bakedTexture != null)
        {
            isExporter = true;
            // Texture资源载入
            LoadAsset(rp.bakedTexture, TextureExportSetting.RGBA);

            #region Json数据载入
            JsonReflectionProbe jrp = new JsonReflectionProbe();
            jrp.blendDistance = rp.blendDistance;
            jrp.color = rp.intensity;
            jrp.localPosition = rp.center;
            jrp.boxProjection = rp.boxProjection ? 1 : 0;
            jrp.localExtent = rp.size;
            string assetPath = AssetDatabase.GetAssetPath(rp.bakedTexture);
            string txGUID = AssetDatabase.AssetPathToGUID(assetPath);
            jrp.cubemapGUID = txGUID.Ptr();
            jsonData.AddKey(jrp);
            #endregion
        }
    }

    private static void ExporterRenderer(Transform tr, SkinnedMeshRenderer skinned, MeshRenderer renderer, MeshFilter filter, ref MiJson jsonData)
    {
        if (skinned != null || (renderer != null && filter != null))
        {

            isExporter = true;
            Material[] mat = skinned == null ? renderer.sharedMaterials : skinned.sharedMaterials;
            if (mat == null)
            {
                Debug.LogError(tr.name + "：Mat为空");
            }
            foreach (var i in mat)
            {
                // material资源载入
                LoadAsset(i);
                // Texture资源载入
               /* LoadAsset(i.GetTexture("_MainTex"), TextureExportSetting.RGB);
                LoadAsset(i.GetTexture("_BumpMap"), TextureExportSetting.Normal);
                LoadAsset(i.GetTexture("_SpecularMap"), TextureExportSetting.RGB);
                LoadAsset(i.GetTexture("_ClipMap"), TextureExportSetting.SingleChannel);*/
            }


            #region Json数据载入
            float[] meshDises;
            NativeArray<ulong> meshGUIDs;
            
            MiLOD miLOD = tr.GetComponent<MiLOD>();
            if (miLOD == null)
            {
                Mesh mesh = skinned == null ? filter.sharedMesh : skinned.sharedMesh;
                // mesh资源载入 
                LoadAsset(mesh);

                meshDises = new float[1];
                meshDises[0] = -1;
                meshGUIDs = new NativeArray<ulong>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                if (mesh.GetAssetPath() == "Library/unity default resources")
                {
                    meshGUIDs[0] = (ulong)(mesh.name.Ptr());
                }
                else
                {
                    meshGUIDs[0] = (ulong)(mesh.GetGUID().Ptr());
                }
            }
            else
            {
                int count = miLOD.LODMeshes.Count;
                meshDises = new float[count];
                meshGUIDs = new NativeArray<ulong>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < count; i++)
                {
                    meshDises[i] = miLOD.LODMeshes[i].distance;
                    if (miLOD.LODMeshes[i].mesh.GetAssetPath() == "Library/unity default resources")
                    {
                        meshGUIDs[i] = (ulong)(miLOD.LODMeshes[i].mesh.name.Ptr());
                    }
                    else
                    {
                        meshGUIDs[i] = (ulong)(miLOD.LODMeshes[i].mesh.GetGUID().Ptr());
                    }
                    if (miLOD.LODMeshes[i].mesh == null)
                    {
                        Debug.LogError("请检查MiLOD所有Mesh是否不为空");
                    }
                    // mesh资源载入 
                    LoadAsset(miLOD.LODMeshes[i].mesh);
                }
            }
            int meshCount = miLOD == null ? 1 : miLOD.LODMeshes.Count;
            NativeArray<ulong> matGUIDs = new NativeArray<ulong>(
                mat.Length + 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for(int i = 0; i < mat.Length; ++i)
            {
                matGUIDs[i] = (ulong)mat[i].GetGUID().Ptr();
            }
            //Set The Last To NULL
            matGUIDs[mat.Length] = 0;
            CullingMask cm = CullingMask.None;
            var scmode = skinned == null ? renderer.shadowCastingMode : skinned.shadowCastingMode;
            switch (scmode)
            {
                case UnityEngine.Rendering.ShadowCastingMode.On:
                    cm = CullingMask.ALL;
                    break;
                case UnityEngine.Rendering.ShadowCastingMode.TwoSided:
                    cm = CullingMask.ALL;
                    break;
                case UnityEngine.Rendering.ShadowCastingMode.Off:
                    cm = CullingMask.GEOMETRY;
                    break;
                case UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:
                    cm = CullingMask.CASCADE_SHADOWMAP | CullingMask.CUBE_SHADOWMAP | CullingMask.SPOT_SHADOWMAP;
                    break;
            }
            if (skinned == null)
            {
                JsonRenderer jrd = new JsonRenderer();
                jrd.meshCount = meshCount;
                jrd.meshDis = meshDises.Ptr();
                jrd.meshGUID = (char**)(meshGUIDs.Ptr());
                jrd.matGUID = (char**)matGUIDs.Ptr();
                jrd.mask = (int)cm;
                jsonData.AddKey(jrd);
            }
            else
            {
                JsonSkinnedRenderer jrd = new JsonSkinnedRenderer();
                jrd.meshCount = meshCount;
                jrd.meshDis = meshDises.Ptr();
                jrd.meshGUID = (char**)(meshGUIDs.Ptr());
                jrd.matGUID = (char**)matGUIDs.Ptr();
                jrd.mask = (int)cm;
                jsonData.AddKey(jrd);
            }
            meshGUIDs.Dispose();
            #endregion
        }
    }

    private static void ExporterAnimClip(ComponentInfo info, ref MiJson jsonData)
    {
        Animator animator = info.animator;
        Animation animation = info.animation;
        if (animator != null || animation != null)
        {
            isExporter = true;
            NativeArray<ulong> clipGUIDs;
            NativeArray<ulong> clipNames;
            JsonAnimClip animClip = new JsonAnimClip();

            if (animator != null)
            {
                AnimationClip[] animationClip = animator.runtimeAnimatorController.animationClips;
                int count = animationClip.Length;
                animClip.clipCount = count;
                clipGUIDs = new NativeArray<ulong>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                clipNames = new NativeArray<ulong>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < count; i++)
                {
                    clipGUIDs[i] = (ulong)(animationClip[i].GetGUID().Ptr());
                    clipNames[i] = (ulong)(animationClip[i].name.Ptr());
                    LoadAsset(animationClip[i], info.skinned.bones, info.tr.gameObject);
                }
            }
            else
            {
                animClip.clipCount = 1;
                clipGUIDs = new NativeArray<ulong>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                clipNames = new NativeArray<ulong>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                clipGUIDs[0] = (ulong)(animation.clip.GetGUID().Ptr());
                clipNames[0] = (ulong)(animation.clip.name.Ptr());
                LoadAsset(animation.clip, info.skinned.bones, info.tr.gameObject);
            }

            animClip.clipName = (char**)clipNames.Ptr();
            animClip.clipGUID = (char**)clipGUIDs.Ptr();
            jsonData.AddKey(animClip);
            clipGUIDs.Dispose();
            clipNames.Dispose();
        }
    }

    #region 资源载入导出
    private static void LoadAsset(Mesh obj)
    {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        string GUID = AssetDatabase.AssetPathToGUID(assetPath);
        if (assetPath == "Library/unity default resources" && ExporterAsset.ContainsKey(obj.name))
            return;
        if (ExporterAsset.ContainsKey(GUID))
            return;

        #region 整合路径
        string path;
        // 资源导出路径后缀名
        string extension = ".vmesh";
        if (assetPath == "Library/unity default resources")
        {
            path = EnginePath + '/' + ExportPath + '/' + "Editor/DefaultMesh/" + obj.name + extension;
        }
        else
        {
            // 资源原路径后缀名
            string assetExtension = System.IO.Path.GetExtension(assetPath);
            path = (EnginePath + '/' + ExportPath + '/' + assetPath.Substring(7)).Replace(assetExtension, extension);
        }
        #endregion
        // 导出mesh
        ExporterAssets(obj, path);

        if (assetPath == "Library/unity default resources")
        {
            ExporterAsset.Add(obj.name, path);
            assetsDataJson.DeleteKey(obj.name);
            assetsDataJson.AddKey(obj.name, SplitPath(path));
        }
        else
        {
            ExporterAsset.Add(GUID, path);
            assetsDataJson.DeleteKey(GUID);
            assetsDataJson.AddKey(GUID, SplitPath(path));
        }
        Debug.Log(path);
    }

    private static void LoadAsset(Texture obj, TextureExportSetting setting)
    {
        if (obj == null)
            return;
        string assetPath = AssetDatabase.GetAssetPath(obj);
        string GUID = AssetDatabase.AssetPathToGUID(assetPath);
        if (ExporterAsset.ContainsKey(GUID))
            return;
        string path;
        // 资源导出路径后缀名
        string extension = ".vtex";
        // 资源原路径后缀名
        string assetExtension = System.IO.Path.GetExtension(assetPath);
        path = (EnginePath + '/' + ExportPath + '/' + assetPath.Substring(7)).Replace(assetExtension, extension);

        // 导出texture
        ExporterAssets(obj, path, setting);
        ExporterAsset.Add(GUID, path);
        assetsDataJson.DeleteKey(GUID);
        assetsDataJson.AddKey(GUID, SplitPath(path));
        Debug.Log(path);
    }

    private static void LoadAsset(Material obj)
    {
        if (obj == null)
            return;
        string assetPath = AssetDatabase.GetAssetPath(obj);
        string GUID = AssetDatabase.AssetPathToGUID(assetPath);
        if (ExporterAsset.ContainsKey(GUID))
            return;
        string path;
        // 资源导出路径后缀名
        string extension = ".mat";
        // 资源原路径后缀名
        string assetExtension = System.IO.Path.GetExtension(assetPath);
        path = (EnginePath + '/' + ExportPath + '/' + assetPath.Substring(7)).Replace(assetExtension, extension);

        // 导出material
        ExporterAssets(obj, path);
        ExporterAsset.Add(GUID, path);
        assetsDataJson.DeleteKey(GUID);
        assetsDataJson.AddKey(GUID, SplitPath(path));
        Debug.Log(path);
    }
    #endregion

    private static void LoadAsset(AnimationClip clip, Transform[] bones, GameObject go)
    {
        string guid = clip.GetGUID();
        if (ExporterAsset.ContainsKey(guid))
            return;
        string path;
        string assetPath = clip.GetAssetPath();
        // 资源导出路径后缀名
        string extension = ".vanim";
        // 资源原路径后缀名
        string assetExtension = System.IO.Path.GetExtension(assetPath);
        path = (EnginePath + '/' + ExportPath + '/' + assetPath.Substring(7)).Replace(assetExtension, extension);

        clipEx.Exporter(clip, bones, go, path);
        ExporterAsset.Add(guid, path);
        assetsDataJson.DeleteKey(guid);
        assetsDataJson.AddKey(guid, SplitPath(path));
    }
    #region 资源导出
    private static void ExporterAssets(Mesh mesh, string path)
    {
        meshEx.ExportMesh(mesh, path);
    }
   
    private static void ExporterAssets(Texture texture, string path, TextureExportSetting setting)
    {
        texEx.texture = texture;
        texEx.useAlphaInCompress = setting == TextureExportSetting.RGBA;
        texEx.path = path;
        texEx.useMipMap = true;
        texEx.isNormal = setting == TextureExportSetting.Normal;
        if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
        {
            switch(setting)
            {
                case TextureExportSetting.RGB:
                case TextureExportSetting.RGBA:
                    texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
                    break;
                case TextureExportSetting.Normal:
                case TextureExportSetting.RG:
                    texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC5U;
                    break;
                case TextureExportSetting.SingleChannel:
                    texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC4U;
                    break;
                case TextureExportSetting.HDR:
                    texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC6H;
                    break;
            }
        }
        else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Cube)
        {
            texEx.isCubemapCompress = true;
        }
        texEx.Print();
    }

    private static void ExporterAssets(Material material, string path)
    {
        matEx.testMat = material;
        matEx.Path = path;
        matEx.Export(LoadAsset);
    }

    #endregion

    #region 资源标记 暂取消
    //[MenuItem("Assets/喵：标记资源", false, 11)]
    //private static void SetAssetExporter()
    //{
    //    string[] GUID = Selection.assetGUIDs;
    //    for (int i = 0; i < GUID.Length; i++)
    //    {
    //        if (!ExporterAsset.ContainsKey(GUID[i]))
    //        {
    //            ExporterAsset.Add(GUID[i], AssetDatabase.GUIDToAssetPath(GUID[i]));
    //        }
    //    }
    //}

    //[MenuItem("Assets/喵：取消标记", false, 11)]
    //private static void UnsetAssetExporter()
    //{
    //    string[] GUID = Selection.assetGUIDs;
    //    for (int i = 0; i < GUID.Length; i++)
    //    {
    //        if (ExporterAsset.ContainsKey(GUID[i]))
    //            ExporterAsset.Remove(GUID[i]);
    //    }
    //}
    #endregion

    private static string SplitPath(string path)
    {
        string[] result = Regex.Split(path, EnginePath + '/');
        return result[1];
    }
}
