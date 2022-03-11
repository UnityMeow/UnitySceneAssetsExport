using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MiLOD))]
public class MiLODEditor : Editor
{
    private int LODCount;
    private MiLOD miLOD;
    private SerializedProperty lodMeshes;

    protected void OnEnable()
    {
        miLOD = (MiLOD)base.target;
        LODCount = miLOD.LODMeshes.Count;
        lodMeshes = serializedObject.FindProperty("LODMeshes");
    }
    public override void OnInspectorGUI()
    {
        Undo.RecordObject(miLOD, miLOD.GetInstanceID().ToString());
        serializedObject.Update();
        GUILayout.BeginHorizontal();
        
        // 输入LOD数量
        GUILayout.Label($"LOD层次:   {LODCount}   ");
        if (GUILayout.Button("增加", GUILayout.Width(50.0f)))
        {
            LODCount++;
            miLOD.LODMeshes.Add(new MiLOD.LODData { mesh = null, distance = 0});
        }

        if (GUILayout.Button("删除", GUILayout.Width(50.0f)) && LODCount > 2)
        {
            LODCount--;
            miLOD.LODMeshes.RemoveAt(LODCount);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("最大距离：");
        miLOD.maxDis = EditorGUILayout.FloatField(miLOD.maxDis, GUILayout.Width(50));

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if (lodMeshes.isArray)
        {
            for (int i = 0; i < lodMeshes.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                
                SerializedProperty lodMesh = lodMeshes.GetArrayElementAtIndex(i);
                if (lodMesh != null && i < miLOD.LODMeshes.Count)
                {
                    SerializedProperty meshProperty = lodMesh.FindPropertyRelative("mesh");
                    if (meshProperty != null)
                    {
                        GUILayout.Label("Mesh");
                        EditorGUILayout.PropertyField(meshProperty, new GUIContent(""));

                        if (i > 0)
                        {
                            GUILayout.Label("距离");
                            float tempdis = miLOD.LODMeshes[i].distance;
                            tempdis = EditorGUILayout.Slider(tempdis, 0F, miLOD.maxDis);
                            var value = miLOD.LODMeshes[i];

                            if (i > 1 && miLOD.LODMeshes[i - 1].distance <= tempdis)
                            {
                                float fTmp = miLOD.LODMeshes[i - 1].distance - 1;
                                if (fTmp < 0)
                                    fTmp = 0;
                                value.distance = fTmp;
                            }
                            else
                                value.distance = tempdis;
                            miLOD.LODMeshes[i] = value;
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }
                
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
