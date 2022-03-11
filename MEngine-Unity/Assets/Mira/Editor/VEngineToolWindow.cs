using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VEngineToolWindow : EditorWindow
{
    [MenuItem("VEngine/工具", priority = 1)]
    private static void ToolMain()
    {
        EditorWindow window = GetWindow(typeof(VEngineToolWindow));
        window.Show();
        window.titleContent = new GUIContent("VEngine");
        window.minSize = new Vector2(430, 230);
    }
    private void OnGUI()
    {

        GUILayout.Box("场景导出");
        GUILayout.Label("Mesh导出设置");
        MeshExporter.useNormal = EditorGUILayout.Toggle("    Normal", MeshExporter.useNormal);
        MeshExporter.useTangent = EditorGUILayout.Toggle("    Tangent", MeshExporter.useTangent);
        MeshExporter.useUV = EditorGUILayout.Toggle("    UV", MeshExporter.useUV);
        MeshExporter.useVertexColor = EditorGUILayout.Toggle("    VertexColor", MeshExporter.useVertexColor);
        MeshExporter.useBone = EditorGUILayout.Toggle("    Bone", MeshExporter.useBone);
        GUILayout.Space(20);
        GUILayout.Label($"当前选中场景数量：{MiSceneExporter.ExporterGameObject.Count}");
        // ===================================
        GUILayout.BeginHorizontal();
        GUILayout.Label("选择引擎根目录");
        MiSceneExporter.EnginePath = GUILayout.TextField(MiSceneExporter.EnginePath, GUILayout.Width(300));
        if (GUILayout.Button("...", GUILayout.Width(30.0f)))
        {
            MiSceneExporter.EnginePath = EditorUtility.OpenFolderPanel("选择引擎根目录", Application.dataPath, "");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        // ================取消JSON导出路径设置===================
        //GUILayout.BeginHorizontal();
        //GUILayout.Label("设置JSON路径");
        //MiSceneExporter.JSONPath = GUILayout.TextField(MiSceneExporter.JSONPath, GUILayout.Width(300));
        //if (GUILayout.Button("...", GUILayout.Width(30.0f)))
        //{
        //    MiSceneExporter.JSONPath = EditorUtility.OpenFolderPanel("设置JSON路径", Application.dataPath, "");
        //}
        //GUILayout.FlexibleSpace();
        //GUILayout.EndHorizontal();
        // ===================================
        if (MiSceneExporter.ExporterGameObject.Count >= 1)
        {
            GUI.color = Color.green;
            if (GUILayout.Button("场景导出", GUILayout.Width(150.0f)))
            {
                MiSceneExporter.Exporter();
            }
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Button("暂无导出", GUILayout.Width(150.0f));
            GUI.color = Color.white;
        }
    }
}
