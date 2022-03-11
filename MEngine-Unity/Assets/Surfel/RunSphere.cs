using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;
using Unity.Collections;

public unsafe class RunSphere : MonoBehaviour
{
    public GameObject sphereInstance;
    [EasyButtons.Button]
    void AddSpheres()
    {
        Random r = new Random((uint)System.Guid.NewGuid().GetHashCode());
        uint2 randP = r.NextUInt2();
        NativeArray<float3> arr = new NativeArray<float3>(128, Allocator.Temp, NativeArrayOptions.ClearMemory);

        for (int i = 0; i < 128; ++i)
        {
            float2 random = new float2();
            float4 result = 0;
            Montcalo.Hammersley((uint)i, 128, 0, random.Ptr());
            Montcalo.UniformSampleSphere(random, result.Ptr());
            arr[i] += result.xyz;

        }

        for (int i = 0; i < 128; ++i)
        {
            Instantiate(sphereInstance, (Vector3)normalize(arr[i]) + transform.position, Quaternion.identity, transform);
        }

    }
}
