using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception.Editor
{
    public struct ComponentSightDebug : IComponentData
    {
        public Color ColorReceiverCone;
        public Color ColorReceiverExtend;
        public Color ColorReceiverClip;

        public Color ColorSourcePerceived;
        public Color ColorSourceMemorized;
        public Color ColorSourceHidden;

        public float3 SizeOctahedron;
        public float ScaleOctahedronBig;
        public float ScaleOctahedronSmall;

        public static ComponentSightDebug Default => new()
        {
            ColorReceiverCone = Color.green,
            ColorReceiverExtend = Color.yellow,
            ColorReceiverClip = Color.gray,

            ColorSourcePerceived = Color.green,
            ColorSourceMemorized = Color.yellow,
            ColorSourceHidden = Color.red,

            SizeOctahedron = new float3(0.5f, 1, 0.5f),
            ScaleOctahedronBig = 0.5f,
            ScaleOctahedronSmall = 0.25f,
        };
    }
}