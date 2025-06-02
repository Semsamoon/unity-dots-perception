using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    /// <summary>
    /// Adds extension of the cone of vision for visible sources.
    /// </summary>
    public struct ComponentSightExtend : IComponentData
    {
        public float2 AnglesCos;
        public float RadiusSquared;
        public float ClipSquared;
    }

    [BurstCompile]
    public static class ComponentSightExtendExtensions
    {
        [BurstCompile]
        public static bool IsInside(this in ComponentSightExtend extend, in float3 origin, in float3 target, in LocalToWorld transform)
        {
            var difference = target - origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > extend.RadiusSquared || distanceSquared <= extend.ClipSquared)
            {
                return false;
            }

            var local = transform.Value.InverseTransformDirection(difference);
            var directionXOZ = math.normalize(new float3(local.x, 0, local.z));
            var directionYOZ = math.normalize(new float3(0, local.y, local.z));

            return math.dot(directionXOZ, new float3(0, 0, 1)) >= extend.AnglesCos.x
                   && math.dot(directionYOZ, new float3(0, 0, 1)) >= extend.AnglesCos.y;
        }
    }
}