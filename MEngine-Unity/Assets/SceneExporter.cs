using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using System.IO;
using System;

//public enum CullingMask
//{
//    None = 0,
//    ALL = -1,
//	GEOMETRY = 1,
//	CASCADE_SHADOWMAP = 2,
//	SPOT_SHADOWMAP = 4,
//	CUBE_SHADOWMAP = 8
//};

[RequireComponent(typeof(TextureExporter))]
[RequireComponent(typeof(MaterialExporter))]
[RequireComponent(typeof(MeshExporter))]
public unsafe class SceneExporter : MonoBehaviour
{

    static void GetSceneData(Scene targetScene, Dictionary<Mesh, bool> meshDict, Dictionary<Material, bool> matDict, Dictionary<Texture, bool> texDict)
    {
        void RunLogic(GameObject i)
        {
            //Mark Textures
            MeshFilter filter = null;
            ReflectionProbe rp = null;
            MeshRenderer renderer = null;
            if ((filter = i.GetComponent<MeshFilter>()) && filter.sharedMesh != null)
            {
                meshDict[filter.sharedMesh] = true;
            }
            if (renderer = i.GetComponent<MeshRenderer>())
            {
                Material mat = renderer.sharedMaterial;
                if (mat)
                {
                    matDict[mat] = true;
                    Texture tex = null;
                    string[] names =
                    {
                        "_MainTex",
                        "_BumpMap",
                        "_SpecularMap",
                    };
                    bool[] useAlpha =
                    {
                        false,
                        false,
                        false
                    };
                    for(uint a = 0; a < names.Length; ++a)
                    {
                        var str = names[a];
                        if (tex = mat.GetTexture(str) as Texture)
                        {
                            texDict[tex] = useAlpha[a];
                        }
                    }
                }
            }

        }
        void IterateObject(Transform tr)
        {
            RunLogic(tr.gameObject);
            for (int i = 0; i < tr.childCount; ++i)
            {
                IterateObject(tr.GetChild(i));
            }
        }
        GameObject[] rootGOs = targetScene.GetRootGameObjects();
        foreach (var i in rootGOs)
        {
            IterateObject(i.transform);
        }
    }
    static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(go);
        return m;
    }
    static string GetGUID(Mesh m, Mesh[] meshes)
    {
        string guid = GetGUID(m);
        for (uint i = 0; i < meshes.Length; ++i)
        {
            if (meshes[i] == m)
            {
                guid.Ptr()[0] = (char)(i + 48);
            }
        }
        return guid;
    }
    public string folder = "Resource/";
    public string sceneName = "TestScene";

    static string GetGUID(UnityEngine.Object obj)
    {
        return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
    }
    static string GetSceneJson(Scene targetScene)
    {
        Mesh[] meshes =
        {
            GetPrimitiveMesh(PrimitiveType.Capsule),
            GetPrimitiveMesh(PrimitiveType.Cube),
            GetPrimitiveMesh(PrimitiveType.Cylinder),
            GetPrimitiveMesh(PrimitiveType.Plane),
            GetPrimitiveMesh(PrimitiveType.Quad),
            GetPrimitiveMesh(PrimitiveType.Sphere)
        };
        List<string> transformDatas = new List<string>();
        void GetTransformString(Transform tr)
        {
            MeshFilter filter = tr.GetComponent<MeshFilter>();
            MeshRenderer renderer = tr.GetComponent<MeshRenderer>();
            Light lit = tr.GetComponent<Light>();
            ReflectionProbe rp = tr.GetComponent<ReflectionProbe>();
            if (!rp && !lit && (!filter || !renderer)) return;
            if (filter && filter.sharedMesh == null)
            {
                return;
            }
            if (renderer && renderer.sharedMaterial == null)
            {
                return;
            }
            List<string> jsonObjs = new List<string>();
            jsonObjs.Add(
                 MJsonUtility.GetKeyValue(
                            "transform",
                            MJsonUtility.MakeJsonObject(
                                MJsonUtility.GetKeyValue(
                                    "position",
                                    tr.position.x.ToString() + ',' + tr.position.y.ToString() + ',' + tr.position.z.ToString(), false),
                                MJsonUtility.GetKeyValue(
                                    "rotation",
                                    tr.rotation.x.ToString() + ',' + tr.rotation.y.ToString() + ',' + tr.rotation.z.ToString() + ',' + tr.rotation.w.ToString(),
                                    false),
                                MJsonUtility.GetKeyValue(
                                    "localscale",
                                    tr.localScale.x.ToString() + ',' + tr.localScale.y.ToString() + ',' + tr.localScale.z.ToString(),
                                    false)
                    ), true));
            if (renderer && filter)
            {
                //CullingMask cm = CullingMask.None;
                //switch(renderer.shadowCastingMode)
                //{
                //    case UnityEngine.Rendering.ShadowCastingMode.On:
                //        cm = CullingMask.ALL;
                //        break;
                //    case UnityEngine.Rendering.ShadowCastingMode.TwoSided:
                //        cm = CullingMask.ALL;
                //        break;
                //    case UnityEngine.Rendering.ShadowCastingMode.Off:
                //        cm = CullingMask.GEOMETRY;
                //        break;
                //    case UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:
                //        cm = CullingMask.CASCADE_SHADOWMAP | CullingMask.CUBE_SHADOWMAP | CullingMask.SPOT_SHADOWMAP;
                //        break;
                //}
                jsonObjs.Add(MJsonUtility.GetKeyValue(
                                "renderer",
                                MJsonUtility.MakeJsonObject(
                                    MJsonUtility.GetKeyValue("mesh", GetGUID(filter.sharedMesh, meshes), false),
                                    MJsonUtility.GetKeyValue("material", GetGUID(renderer.sharedMaterial), false),
                                    MJsonUtility.GetKeyValue("mask", (int)0)
                                    ), true));
            }
            

            transformDatas.Add(
                    MJsonUtility.GetKeyValue(
                        tr.gameObject.name,
                        MJsonUtility.MakeJsonObject(jsonObjs), true)
                );
        }
        void IterateTransform(Transform tr)
        {
            GetTransformString(tr);
            for (int i = 0; i < tr.childCount; ++i)
            {
                IterateTransform(tr.GetChild(i));
            }
        }
        GameObject[] rootGameObjects = targetScene.GetRootGameObjects();
        foreach (var i in rootGameObjects)
        {
            IterateTransform(i.transform);
        }
        return MJsonUtility.MakeJsonObject(transformDatas);
    }
    [EasyButtons.Button]
    void ExportSceneResource()
    {
        Mesh[] meshesPri =
        {
            GetPrimitiveMesh(PrimitiveType.Capsule),
            GetPrimitiveMesh(PrimitiveType.Cube),
            GetPrimitiveMesh(PrimitiveType.Cylinder),
            GetPrimitiveMesh(PrimitiveType.Plane),
            GetPrimitiveMesh(PrimitiveType.Quad),
            GetPrimitiveMesh(PrimitiveType.Sphere)
        };
        MaterialExporter matEx = GetComponent<MaterialExporter>();
        MeshExporter meshEx = GetComponent<MeshExporter>();
        TextureExporter texEx = GetComponent<TextureExporter>();
        Dictionary<Material, bool> matDict = new Dictionary<Material, bool>();
        Dictionary<Mesh, bool> meshDict = new Dictionary<Mesh, bool>();
        Dictionary<Texture, bool> texDict = new Dictionary<Texture, bool>();
        Scene sc = SceneManager.GetActiveScene();
        GetSceneData(
            SceneManager.GetActiveScene(),
            meshDict, matDict, texDict);
      //  void* jsonObjPtr = UnsafeUtility.Malloc((long)MJsonUtility.JsonObjectSize(), 16, Unity.Collections.Allocator.Temp);
        string jsonPath = folder + "AssetDatabase.json";
        fixed (char* c = jsonPath)
        {
          //  MJsonUtility.CreateJsonObject(c, (ulong)jsonPath.Length, jsonObjPtr);
        }
        List<Mesh> meshes = new List<Mesh>(meshDict.Count);
        List<string> meshNames = new List<string>(meshDict.Count);
        foreach (var i in meshDict)
        {
            string guid = GetGUID(i.Key, meshesPri);
            string name = folder + guid + ".vmesh";
            meshes.Add(i.Key);
            meshNames.Add(name);
            //MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);

        }
        foreach (var i in matDict)
        {
            string guid = GetGUID(i.Key);
            string name = folder + guid + ".mat";
          //  MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);
            matEx.testMat = i.Key;
            matEx.Path = name;
          //  matEx.Export();
        }
        foreach (var i in texDict)
        {
            string guid = GetGUID(i.Key);
            string name = folder + guid + ".vtex";
          //  MJsonUtility.UpdateKeyValue(jsonObjPtr, guid.Ptr(), (ulong)guid.Length, name.Ptr(), (ulong)name.Length);
            texEx.texture = i.Key;
            texEx.useAlphaInCompress = i.Value;
            texEx.path = name;
            texEx.useMipMap = true;
            if (i.Key.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
            {
                texEx.tex2DFormat = TextureData.LoadFormat.LoadFormat_BC7;
                texEx.Print();
            }
            else if (i.Key.dimension == UnityEngine.Rendering.TextureDimension.Cube)
            {
                texEx.isCubemapCompress = true;
                texEx.Print();
            }
        }
        string sceneID = "Scene_" + sc.name;
        string path = folder + sceneName + ".json";
       // MJsonUtility.UpdateKeyValue(jsonObjPtr, sceneID.Ptr(), (ulong)sceneID.Length, path.Ptr(), (ulong)path.Length);
        meshEx.ExportAllMeshes(meshes, meshNames);
        //MJsonUtility.OutputJsonObject(jsonPath.Ptr(), (ulong)jsonPath.Length, jsonObjPtr);
      //  MJsonUtility.DisposeJsonObject(jsonObjPtr);
    }
    [EasyButtons.Button]
    void ExportSceneJson()
    {
        string s = GetSceneJson(SceneManager.GetActiveScene());
        using (StreamWriter sw = new StreamWriter(folder + sceneName + ".json", false, System.Text.Encoding.ASCII))
        {
            sw.Write(s);
        }
    }
}
