#ifndef __SDF_INCLUDE
	#define __SDF_INCLUDE
	#define Float32MaxValue  3.40282347E+37f
	#define Float32MinValue  -3.40282347E+37f
	#include "Plane.cginc"
	struct SDFPrimitive
	{
		float4x4 worldToLocalMatrix;
		float3 localExtent;
		uint type;
		//0: cube 1: cylinder
	};
	float DistanceToLine(float3 linePoint0, float3 linePoint1, float3 targetPoint)
	{
		float3 tarToP0 = linePoint0 - targetPoint;
		float cLenSqr = dot(tarToP0, tarToP0);
		float bLen = dot(normalize(linePoint0 - linePoint1), tarToP0);
		return sqrt(cLenSqr - bLen * bLen);
	}
	float DistanceToPlane(float3 planeNormal, float3 planeCenter,float3 targetPoint)
	{
		return GetDistanceToPlane(GetPlane(planeNormal, planeCenter), targetPoint);
	}
	float GetDistanceToCylinder(float4x4 worldToLocal, float3 localExtent, float3 samplePoint)
	{
		float upScale = localExtent.y * 2;
		float horScale = max(localExtent.x, localExtent.z);

		samplePoint = mul(worldToLocal, float4(samplePoint, 1)).xyz;
		samplePoint = abs(samplePoint);
		float xzLen = length(samplePoint.xz);
		if(samplePoint.y > upScale)
		{
			if (xzLen < horScale)
			{
				return samplePoint.y - upScale;
			}
			else
			{
				float aLen = samplePoint.y - upScale;
				float bLen = xzLen - horScale;
				return sqrt(dot(float2(aLen, bLen), float2(aLen, bLen)));
			}
		}
		else
		{
			if(xzLen > horScale)
			{
				return xzLen - horScale;
			}
			else
			{
				return max(samplePoint.y - upScale, xzLen - horScale);
			}
		}
	}
	float GetDistanceToCube(float4x4 worldToLocal, float3 localExtent, float3 samplePoint)
	{
		//Plane
		float3 planeCenter = 0;
		float3 planeNormal = 0;
		//Line
		float3 linePoint0 = 0;
		float3 linePoint1 = 0;
		//Point
		float3 targetPoint = 0;

		samplePoint = abs(mul(worldToLocal, float4(samplePoint, 1)).xyz);
		uint mode = 0;       //Point, Line, Plane

		if (samplePoint.x > localExtent.x)
		{
			if (samplePoint.y > localExtent.y)
			{
				if (samplePoint.z > localExtent.z)
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
				if (samplePoint.z > localExtent.z)
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
			if (samplePoint.y > localExtent.y)
			{
				if (samplePoint.z > localExtent.z)
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
				if (samplePoint.z > localExtent.z)
				{
					mode = 2;
					planeNormal = float3(0, 0, 1);
					planeCenter = float3(0, 0, 1) * localExtent;
				}
				else
				{
					return max(
					max(
					DistanceToPlane(float3(0, 0, 1), float3(0, 0, 1) * localExtent, samplePoint),
					DistanceToPlane(float3(0, 1, 0), float3(0, 1, 0) * localExtent, samplePoint)
					),
					DistanceToPlane(float3(1, 0, 0), float3(1, 0, 0) * localExtent, samplePoint));
				}
			}

		}
		switch (mode)
		{
			case 0:
			return distance(targetPoint, samplePoint);
			case 1:
			return DistanceToLine(linePoint0, linePoint1, samplePoint);
			default:
			return DistanceToPlane(planeNormal, planeCenter, samplePoint);
		}
	}
	
	uint3 GetUInt3FromUInt(uint voxelValue)
	{
		uint3 voxelID;
		voxelID.z = voxelValue & 1023;
		voxelValue >>= 10;
		voxelID.y = voxelValue & 1023;
		voxelValue >>= 10;
		voxelID.x = voxelValue & 1023;
		return voxelID;
	}
	float3 GetForceToPrimitive(SDFPrimitive prim, float3 worldPos, float3 targetDist)
	{
		float4 dist = Float32MaxValue;
		const float2 e = float2(1.0,-1.0)*0.5773*0.0005;
		float3 worldPos0 = worldPos + e.xyy;
		float3 worldPos1 = worldPos + e.yyx;
		float3 worldPos2 = worldPos + e.yxy;
		float3 worldPos3 = worldPos + e.xxx;

		switch(prim.type)
		{
			
			case 0:
			{
				dist = float4(
				GetDistanceToCube(prim.worldToLocalMatrix, prim.localExtent, worldPos0),
				GetDistanceToCube(prim.worldToLocalMatrix, prim.localExtent, worldPos1),
				GetDistanceToCube(prim.worldToLocalMatrix, prim.localExtent, worldPos2),
				GetDistanceToCube(prim.worldToLocalMatrix, prim.localExtent, worldPos3)
				);
			}
			break;
			case 1:
			{
				dist = float4(
				GetDistanceToCylinder(prim.worldToLocalMatrix, prim.localExtent, worldPos0),
				GetDistanceToCylinder(prim.worldToLocalMatrix, prim.localExtent, worldPos1),
				GetDistanceToCylinder(prim.worldToLocalMatrix, prim.localExtent, worldPos2),
				GetDistanceToCylinder(prim.worldToLocalMatrix, prim.localExtent, worldPos3)
				);
				
			}
			break;
		}
		float3 normal = normalize( e.xyy * dist.x + 
		e.yyx * dist.y + 
		e.yxy * dist.z + 
		e.xxx * dist.w );
		return max(0, targetDist - dot(dist, 0.25)) * normal;
	}
	float3 GetOffsetForce(
	uint4 indices_Int,
	StructuredBuffer<SDFPrimitive> primitives,
	float3 worldPos,
	float3 targetDist)
	{
		float3 offsetForce = 0;
		uint3 indices;
		#define ACCUMULATE_FORCE offsetForce += GetForceToPrimitive(prim, worldPos, targetDist);

		SDFPrimitive prim;
		if(indices_Int.x == 0) return offsetForce;
		indices = GetUInt3FromUInt(indices_Int.x);
		if(indices.x == 0) return offsetForce;
		prim = primitives[indices.x - 1];
		ACCUMULATE_FORCE
		if(indices.y == 0) return offsetForce;
		prim = primitives[indices.y - 1];
		ACCUMULATE_FORCE
		if(indices.z == 0) return offsetForce;
		prim = primitives[indices.z - 1];
		ACCUMULATE_FORCE

		if(indices_Int.y == 0) return offsetForce;
		indices = GetUInt3FromUInt(indices_Int.y);
		if(indices.x == 0) return offsetForce;
		prim = primitives[indices.x - 1];
		ACCUMULATE_FORCE
		if(indices.y == 0) return offsetForce;
		prim = primitives[indices.y - 1];
		ACCUMULATE_FORCE
		if(indices.z == 0) return offsetForce;
		prim = primitives[indices.z - 1];
		ACCUMULATE_FORCE

		if(indices_Int.z == 0) return offsetForce;
		indices = GetUInt3FromUInt(indices_Int.z);
		if(indices.x == 0) return offsetForce;
		prim = primitives[indices.x - 1];
		ACCUMULATE_FORCE
		if(indices.y == 0) return offsetForce;
		prim = primitives[indices.y - 1];
		ACCUMULATE_FORCE
		if(indices.z == 0) return offsetForce;
		prim = primitives[indices.z - 1];
		ACCUMULATE_FORCE

		if(indices_Int.w == 0) return offsetForce;
		indices = GetUInt3FromUInt(indices_Int.w);
		if(indices.x == 0) return offsetForce;
		prim = primitives[indices.x - 1];
		ACCUMULATE_FORCE
		if(indices.y == 0) return offsetForce;
		prim = primitives[indices.y - 1];
		ACCUMULATE_FORCE
		if(indices.z == 0) return offsetForce;
		prim = primitives[indices.z - 1];
		ACCUMULATE_FORCE
		return offsetForce;
		
		
	}

	float GetDistanceFromArea(
	StructuredBuffer<SDFPrimitive> primitives, uint primCount,
	StructuredBuffer<float4> spheres, uint sphereCount, float3 worldPos)
	{
		float dist = Float32MaxValue;
		for(uint i = 0; i < primCount; ++i)
		{
			SDFPrimitive prim = primitives[i];
			float currDist;
			//cube
			if(prim.type == 0)
			{
				currDist = GetDistanceToCube(prim.worldToLocalMatrix, prim.localExtent, worldPos);
				
			}
			else
			{
				currDist = GetDistanceToCylinder(prim.worldToLocalMatrix, prim.localExtent, worldPos);
			}
			dist = min(dist, currDist);
		}
		for(uint i = 0; i < sphereCount; ++i)
		{
			float4 sph = spheres[i];
			float currDist;
			float absCurrDist;
			
			currDist = distance(sph.xyz, worldPos) - 0.5;
			dist = min(dist, currDist);
		}
		return dist;
	}
	float3 CalcSDFNormal(
	StructuredBuffer<SDFPrimitive> primitives, uint primCount,
	StructuredBuffer<float4> spheres, uint sphereCount, float3 pos, out float dist)
	{
		
		const float2 e = float2(1.0,-1.0)*0.5773*0.0005;
		float4 allDists = float4(
		GetDistanceFromArea(primitives, primCount, spheres, sphereCount, pos + e.xyy ),
		GetDistanceFromArea(primitives, primCount, spheres, sphereCount, pos + e.yyx ),
		GetDistanceFromArea(primitives, primCount, spheres, sphereCount, pos + e.yxy ),
		GetDistanceFromArea(primitives, primCount, spheres, sphereCount,pos + e.xxx )
		);
		dist = dot(allDists, 0.25);
		return normalize( e.xyy * allDists.x + 
		e.yyx * allDists.y + 
		e.yxy * allDists.z + 
		e.xxx * allDists.w );
		
	}
	
#endif