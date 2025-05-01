using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception.Editor
{
    public struct ComponentHearingDebug : IComponentData
    {
        public Color ColorSourceSphere;
        public Color ColorSourceInternal;

        public Color ColorSourcePerceived;
        public Color ColorSourceMemorized;

        public float3 SizeOctahedron;
        public float ScaleOctahedronBig;
        public float ScaleOctahedronSmall;

        public static ComponentHearingDebug Default => new()
        {
            ColorSourceSphere = Color.green,
            ColorSourceInternal = Color.red,

            ColorSourcePerceived = Color.green,
            ColorSourceMemorized = Color.yellow,

            SizeOctahedron = new float3(0.5f, 1, 0.5f),
            ScaleOctahedronBig = 0.5f,
            ScaleOctahedronSmall = 0.25f,
        };
    }
}