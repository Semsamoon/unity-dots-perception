using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Perception
{
    /// <summary>
    /// Adds cone of vision to the receiver.
    /// </summary>
    public struct ComponentSightCone : IComponentData
    {
        public CollisionFilter Filter;
        public float2 AnglesCos;
        public float RadiusSquared;
        public float ClipSquared;
    }

    [BurstCompile]
    public static class ComponentSightConeExtensions
    {
        [BurstCompile]
        public static bool IsInside(this in ComponentSightCone cone, in float3 origin, in float3 target, in LocalToWorld transform)
        {
            var difference = target - origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > cone.RadiusSquared || distanceSquared <= cone.ClipSquared)
            {
                return false;
            }

            var local = transform.Value.InverseTransformDirection(difference);
            var directionXOZ = math.normalize(new float3(local.x, 0, local.z));
            var directionYOZ = math.normalize(new float3(0, local.y, local.z));

            return math.dot(directionXOZ, new float3(0, 0, 1)) >= cone.AnglesCos.x
                   && math.dot(directionYOZ, new float3(0, 0, 1)) >= cone.AnglesCos.y;
        }
    }
}