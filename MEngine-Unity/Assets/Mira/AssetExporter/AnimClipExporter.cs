using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;

public unsafe class AnimClipExporter
{
    struct AnimationClipInfo
    {
        public float3 boundCenter;
        public float3 boundExtent;

        public float lengthInSecond;
        /// <summary>
        /// FPS
        /// </summary>
        public float framePerSecond;
        /// <summary>
        /// 总帧数
        /// </summary>
        public int frameCount;
        /// <summary>
        /// bones长度
        /// </summary>
        public int bonesCount;
    }

    public void Exporter(AnimationClip clip, Transform[] bones, GameObject go,string path)
    {
        // 初始化动画基本信息
        AnimationClipInfo info = new AnimationClipInfo()
        {
            framePerSecond = clip.frameRate,
            frameCount = (int)(clip.frameRate * clip.length),
            boundCenter = clip.localBounds.center,
            boundExtent = clip.localBounds.extents,
            lengthInSecond = clip.length,
            bonesCount = bones.Length,
        };

        // 写入文件的总大小
        int fileSize = sizeof(AnimationClipInfo) + info.frameCount * bones.Length * (sizeof(float4x3));
        // 要写入的文件
        byte[] file = new byte[fileSize];
        // 要写入的文件地址
        byte* filePtr = file.Ptr();
        MiTool.AutoCreatFolder(path.Ptr());
        // 动画基本信息写入
        MiTool.WriteByte(ref filePtr, info.Ptr(), sizeof(AnimationClipInfo));

        //所有骨骼的位置按帧写入
        float4x3[] allBonePos = new float4x3[info.frameCount * bones.Length];
        
        for (int i = 0; i < info.frameCount; ++i)
        {
            clip.SampleAnimation(go, ((i + 0.5f) / clip.frameRate));
            for (int j = 0; j < bones.Length; j++)
            {
                allBonePos[(i * bones.Length) + j] = bones[j].LocalToWorldFloat4x3();
            }
        }
        // 所有骨骼位置信息写入
        MiTool.WriteByte(ref filePtr, allBonePos.Ptr(), allBonePos.Length * sizeof(float4x3));
        // 输出到文件
        File.WriteAllBytes(path, file);
    }
}
