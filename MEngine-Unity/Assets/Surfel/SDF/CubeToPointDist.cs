using MPipeline;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class CubeToPointDist : MonoBehaviour
{
    public Transform cube;
    public Transform point;
    static float DistanceToLine(float3 linePoint0, float3 linePoint1, float3 targetPoint)
    {
        float3 tarToP0 = linePoint0 - targetPoint;
        float cLenSqr = dot(tarToP0, tarToP0);
        float bLen = dot(normalize(linePoint0 - linePoint1), tarToP0);
        return sqrt(cLenSqr - bLen * bLen);
    }
    static float DistanceToPlane(float3 planeCenter, float3 planeNormal, float3 targetPoint)
    {
        return MathLib.GetDistanceToPlane(MathLib.GetPlane(planeNormal, planeCenter), targetPoint);
    }
    float GetDistanceToCube(float4x4 worldToLocal, float3 localExtent, float3 point)
    {
        //Plane
        float3 planeCenter = 0;
        float3 planeNormal = 0;
        //Line
        float3 linePoint0 = 0;
        float3 linePoint1 = 0;
        //Point
        float3 targetPoint = 0;

        point = abs(mul(worldToLocal, float4(point, 1)).xyz);
        int mode = 0;       //Point, Line, Plane

        if (point.x > localExtent.x)
        {
            if (point.y > localExtent.y)
            {
                if (point.z > localExtent.z)
                {
                    mode = 0;
                    targetPoint = float3(1, 1, 1) * localExtent;
                }
                else
                {
                    mode = 1;
                    linePoint0 = float3(1, 1, 1) * localExtent;
                    linePoint1 = float3(1, 1, -1) * localExtent;
                }
            }
            else
            {
                if (point.z > localExtent.z)
                {
                    mode = 1;
                    linePoint0 = float3(1, 1, 1) * localExtent;
                    linePoint1 = float3(1, -1, 1) * localExtent;
                }
               
                else
                {
                    mode = 2;
                    planeNormal = float3(1, 0, 0);
                    planeCenter = float3(1, 0, 0) * localExtent;
                }
            }
        }
        else
        {
            if (point.y > localExtent.y)
            {
                if (point.z > localExtent.z)
                {
                    mode = 1;
                    linePoint0 = float3(1, 1, 1) * localExtent;
                    linePoint1 = float3(-1, 1, 1) * localExtent;
                }
                else
                {
                    mode = 2;
                    planeNormal = float3(0, 1, 0);
                    planeCenter = float3(0, 1, 0) * localExtent;
                }
            }
            else
            {
                if (point.z > localExtent.z)
                {
                    mode = 2;
                    planeNormal = float3(0, 0, 1);
                    planeCenter = float3(0, 0, 1) * localExtent;
                }
                else
                {
                    return max(
                        max(
                            DistanceToPlane(float3(0, 0, 1), float3(0, 0, 1) * localExtent, point),
                            DistanceToPlane(float3(0, 1, 0), float3(0, 1, 0) * localExtent, point)
                            ),
                        DistanceToPlane(float3(1, 0, 0), float3(1, 0, 0) * localExtent, point));
                }
            }

        }
        switch (mode)
        {
            case 0:
                return distance(targetPoint, point);
            case 1:
                return DistanceToLine(linePoint0, linePoint1, point);
            default:
                return DistanceToPlane(planeCenter, planeNormal, point);
        }
    }
    float GetDistanceToCap(float4x4 worldToLocal, float3 localExtent, float3 point)
    {
        point = mul(worldToLocal, float4(point, 1)).xyz;
        point = abs(point);
        float xzLen = length(point.xz);
        if(point.y > localExtent.y * 2)
        {
            if (xzLen < 0.5f)
            {
                return point.y - 1;
            }
            else
            {
                float aLen = point.y - 1;
                float bLen = xzLen - 0.5f;
                return sqrt(dot(float2(aLen, bLen), float2(aLen, bLen)));
            }
        }
        else
        {
            if(xzLen > 0.5f)
            {
                return xzLen - 0.5f;
            }
            else
            {
                return max(point.y - 1, xzLen - 0.5f);
            }
        }
    }
    [EasyButtons.Button]
    void RunTest()
    {
        float4x4 localToWorld = float4x4(
            float4(cube.right, 0),
            float4(cube.up, 0),
            float4(cube.forward, 0),
            float4(cube.position, 1));
        Debug.Log(GetDistanceToCap(inverse(localToWorld), cube.localScale * 0.5f, point.position));
    }
}
