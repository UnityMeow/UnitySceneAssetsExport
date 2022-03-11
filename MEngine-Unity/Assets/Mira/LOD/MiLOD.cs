using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
public class MiLOD : MonoBehaviour
{
    private MeshFilter meshFilter;
    private SkinnedMeshRenderer skinned;
    private Vector3 tempCameraPos = Vector3.zero;
    private Vector3 tempGOPos = Vector3.zero;
    public float maxDis;

    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += UpdatePerFrame;
        RefreshMesh();
#endif
    }

    public void Awake()
    {
        RefreshMesh();
        // 初始化
        Mesh mmesh = skinned == null ? meshFilter.sharedMesh : skinned.sharedMesh;
        if (LODMeshes.Count == 0)
        {
            const int LOD_COUNT = 5;
            LODMeshes.Capacity = LOD_COUNT;
            maxDis = 50;
            for (int i = 0; i < LOD_COUNT; i++)
            {
                float dis = ((i == 0) ? -1 : (LOD_COUNT - i - 0.5f));
                if (i != 0)
                {
                    LODMeshes.Add(new LODData { mesh = null, distance = dis });
                }
                else
                {
                    LODMeshes.Add(new LODData { mesh = mmesh, distance = dis });
                }
            }
        }
    }

    private void RefreshMesh()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        skinned = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (meshFilter == null && skinned == null)
        {
            Debug.LogError("MeshFilter or Skinned must have one that is not empty");
        }
    }

    [Serializable]
    public struct LODData
    {
        public Mesh mesh;
        public float distance;
    }
    public List<LODData> LODMeshes = new List<LODData>();

    private void UpdatePerFrame()
    {
        if (LODMeshes == null || LODMeshes.Count < 1)
            return;
        if (tempCameraPos != SceneView.lastActiveSceneView.camera.transform.position || transform.position != tempGOPos)
        {
            tempCameraPos = SceneView.lastActiveSceneView.camera.transform.position;
            tempGOPos = transform.position;
            float dis = (SceneView.lastActiveSceneView.camera.transform.position - transform.position).magnitude;
            if (dis > LODMeshes[1].distance)
            {
                if (skinned == null)
                {
                    meshFilter.sharedMesh = LODMeshes[0].mesh;
                }
                else
                {
                    skinned.sharedMesh = LODMeshes[0].mesh;
                }
            }
            else
            {
                for (int i = 1; i < LODMeshes.Count; ++i)
                {
                    if (skinned == null)
                    {
                        if (i == LODMeshes.Count - 1)
                        {
                            if (dis <= LODMeshes[i].distance && meshFilter.sharedMesh != LODMeshes[i].mesh)
                                meshFilter.sharedMesh = LODMeshes[i].mesh;
                        }
                        else if (dis <= LODMeshes[i].distance
                            && dis > LODMeshes[i + 1].distance
                            && meshFilter.sharedMesh != LODMeshes[i].mesh)
                        {
                            meshFilter.sharedMesh = LODMeshes[i].mesh;
                        }
                    }
                    else
                    {
                        if (i == LODMeshes.Count - 1)
                        {
                            if (dis <= LODMeshes[i].distance && skinned.sharedMesh != LODMeshes[i].mesh)
                                skinned.sharedMesh = LODMeshes[i].mesh;
                        }
                        else if (dis <= LODMeshes[i].distance
                            && dis > LODMeshes[i + 1].distance
                            && skinned.sharedMesh != LODMeshes[i].mesh)
                        {
                            skinned.sharedMesh = LODMeshes[i].mesh;
                        }
                    }
                }
            }
            //Debug.Log(dis);
        }
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= UpdatePerFrame;
#endif 
    }
}
