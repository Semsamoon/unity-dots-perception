using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Perception.Editor
{
    public partial struct SystemSightDebug : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var entity in SystemAPI
                         .QueryBuilder()
                         .WithAny<TagSightReceiver>()
                         .WithNone<TagSightDebug>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent<TagSightDebug>(entity);
                commands.SetComponentEnabled<TagSightDebug>(entity, false);
            }

            commands.Playback(state.EntityManager);

            foreach (var (transformRO, positionRO, coneRO, extendRO, clipRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightExtend>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightDebug>())
            {
                var position = positionRO.ValueRO.Receiver;
                var rotation = transformRO.ValueRO.Rotation;
                var radius = math.sqrt(coneRO.ValueRO.RadiusSquared);
                var extendRadius = math.sqrt(extendRO.ValueRO.RadiusSquared);
                var angles = math.acos(coneRO.ValueRO.AnglesCos);
                var extendAngles = math.acos(extendRO.ValueRO.AnglesCos);
                var clip = math.sqrt(clipRO.ValueRO.RadiusSquared);

                SightSenseAuthoring.DrawCone(position, rotation, clip, extendRadius, extendAngles, Color.yellow);
                SightSenseAuthoring.DrawCone(position, rotation, clip, radius, angles, Color.green);
                SightSenseAuthoring.DrawCone(position, rotation, 0, clip, extendAngles, Color.gray);
            }

            foreach (var (transformRO, positionRO, coneRO, clipRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightDebug>()
                         .WithNone<ComponentSightExtend>())
            {
                var position = positionRO.ValueRO.Receiver;
                var rotation = transformRO.ValueRO.Rotation;
                var radius = math.sqrt(coneRO.ValueRO.RadiusSquared);
                var angles = math.acos(coneRO.ValueRO.AnglesCos);
                var clip = math.sqrt(clipRO.ValueRO.RadiusSquared);

                SightSenseAuthoring.DrawCone(position, rotation, clip, radius, angles, Color.green);
                SightSenseAuthoring.DrawCone(position, rotation, 0, clip, angles, Color.gray);
            }

            foreach (var (transformRO, positionRO, coneRO, extendRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, TagSightDebug>()
                         .WithNone<ComponentSightClip>())
            {
                var position = positionRO.ValueRO.Receiver;
                var rotation = transformRO.ValueRO.Rotation;
                var radius = math.sqrt(coneRO.ValueRO.RadiusSquared);
                var extendRadius = math.sqrt(extendRO.ValueRO.RadiusSquared);
                var angles = math.acos(coneRO.ValueRO.AnglesCos);
                var extendAngles = math.acos(extendRO.ValueRO.AnglesCos);

                SightSenseAuthoring.DrawCone(position, rotation, 0, extendRadius, extendAngles, Color.yellow);
                SightSenseAuthoring.DrawCone(position, rotation, 0, radius, angles, Color.green);
            }

            foreach (var (transformRO, positionRO, coneRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, TagSightDebug>()
                         .WithNone<ComponentSightClip, ComponentSightExtend>())
            {
                var position = positionRO.ValueRO.Receiver;
                var rotation = transformRO.ValueRO.Rotation;
                var radius = math.sqrt(coneRO.ValueRO.RadiusSquared);
                var angles = math.acos(coneRO.ValueRO.AnglesCos);

                SightSenseAuthoring.DrawCone(position, rotation, 0, radius, angles, Color.green);
            }
        }
    }
}