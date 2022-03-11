using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using MPipeline;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using UnityEngine.Jobs;
using UnityEditor;
public class TerrainDecal : MonoBehaviour
{

    public Block.Operator ope;
    public Texture height;
    public float scale = 1;
    public float offset = 0;
    public Block.MaskStrategy maskStrategy = Block.MaskStrategy.Circle;
    [HideInInspector]
    public float2 rectOffset = 0;
    [HideInInspector]
    public float rectSide = 1;
    [Range(0.01f, 10f)]
    public float power = 1;
    [HideInInspector]
    public Texture blendMask;
    public static List<TerrainDecal> decalList = new List<TerrainDecal>(1000);
    public static List<TerrainDecal> updateDecalList = new List<TerrainDecal>(100);
    int index;
    [System.NonSerialized]
    public double3 absolutePos;
    public double4x4 localToWorldMatrix { get; private set; }
    private double4x4 worldToLocalMatrix;
    private bool shouldEnable = false;
    [HideInInspector]
    public double3 lastMin = 0;
    [HideInInspector]
    public double3 lastMax = 0;
    private void Awake()
    {
        enabled = false;
        shouldEnable = true;
        transform.localScale = float3(100, 1000, 100);
    }
    private void UpdateMat()
    {
        localToWorldMatrix = new double4x4(
            double4((float3)transform.right * transform.localScale.x, 0),
            double4((float3)transform.up * transform.localScale.y, 0),
            double4((float3)transform.forward * transform.localScale.z, 0),
            double4((double3)(float3)transform.position + TerrainHeightEditor.current.chunk * 100, 1));
        worldToLocalMatrix = inverse(localToWorldMatrix);
    }
    public BlockTotalData GetData()
    {
        BlockTotalData td;
        td.block.blendMask = blendMask;
        td.block.height = height;
        td.block.offset = offset;
        td.block.scale = scale;
        td.block.ope = ope;
        td.worldToLocal = worldToLocalMatrix;
        td.block.rectOffset = rectOffset;
        td.block.rectSize = rectSide;
        td.block.power = power;
        td.block.strategy = maskStrategy;
        return td;
    }
    private void OnEnable()
    {
        if (!shouldEnable) return;
        var he = TerrainHeightEditor.current;
        absolutePos = (float3)transform.position;
        absolutePos += he.chunk * 100;
        index = decalList.Count;
        decalList.Add(this);
        he.accArray.Add(transform);
        UpdateMat();
        updateDecalList.Add(this);
    }
    private void OnDisable()
    {
        if (!shouldEnable) return;
        decalList[index] = decalList[decalList.Count - 1];
        decalList[index].index = index;
        decalList.RemoveAt(decalList.Count - 1);
        if (TerrainHeightEditor.current)
        {
            TerrainHeightEditor.current.accArray.RemoveAtSwapBack(index);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (!shouldEnable) return;
        transform.eulerAngles = float3(0, transform.eulerAngles.y, 0);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 1, 1, 1);
        Gizmos.DrawWireCube(new Vector3(0, 0, 0), Vector3.one);
        Gizmos.color = new Color(0.2f, 0.3f, 0.8f, 0.3f);
        Gizmos.DrawCube(new Vector3(0, 0, 0), Vector3.one);
        var he = TerrainHeightEditor.current;
        if (he)
        {
            absolutePos = (float3)transform.position;
            absolutePos += he.chunk * 100;
        }
        updateDecalList.Add(this);
        UpdateMat();
    }
}

[CustomEditor(typeof(TerrainDecal))]
class TerrainDecalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TerrainDecal dc = (TerrainDecal)serializedObject.targetObject;
        Undo.RecordObject(dc, dc.GetInstanceID().ToString());
        switch (dc.maskStrategy)
        {
            case Block.MaskStrategy.RectangleMask:
                dc.blendMask = EditorGUILayout.ObjectField("Blend Mask", dc.blendMask, typeof(Texture), false) as Texture;
                dc.rectOffset = EditorGUILayout.Vector2Field("Rectangle Offset", dc.rectOffset);
                dc.rectSide = EditorGUILayout.FloatField("Rectangle Side", dc.rectSide);
                break;
            case Block.MaskStrategy.Rectangle:
                dc.rectOffset = EditorGUILayout.Vector2Field("Rectangle Offset", dc.rectOffset);
                dc.rectSide = EditorGUILayout.FloatField("Rectangle Side", dc.rectSide);
                break;
            case Block.MaskStrategy.CircleMask:
                dc.blendMask = EditorGUILayout.ObjectField("Blend Mask", dc.blendMask, typeof(Texture), false) as Texture;
                break;
            case Block.MaskStrategy.Mask:
                dc.blendMask = EditorGUILayout.ObjectField("Blend Mask", dc.blendMask, typeof(Texture), false) as Texture;
                break;
        }
    }
}