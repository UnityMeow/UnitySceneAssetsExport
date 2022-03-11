using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(TerrainHeightEditor))]
public class TerrainSplatDrawer : MonoBehaviour
{
    private TerrainHeightEditor heightEditor;
    private ComputeBuffer cbuffer;
    private void Awake()
    {   
        heightEditor = GetComponent<TerrainHeightEditor>();  
    }
}
