﻿using Unity.Burst;
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

            if (distanceSquared > cone.RadiusSquared || distanceSquared < cone.ClipSquared)
            {
                return false;
            }

            var directionLocal = transform.Value.InverseTransformDirection(difference);
            var squared = directionLocal * directionLocal;

            return directionLocal.z / math.sqrt(squared.x + squared.z) >= cone.AnglesCos.x
                   && math.sqrt((squared.x + squared.z) / (squared.x + squared.y + squared.z)) >= cone.AnglesCos.y;
        }
    }
}