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

            state.Dependency.Complete();

            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();
            var buffersMemory = SystemAPI.GetBufferLookup<BufferSightMemory>();
            var buffersCone = SystemAPI.GetBufferLookup<BufferSightCone>();

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightDebug>()
                         .WithAll<BufferSightCone, BufferSightPerceive, BufferSightMemory>()
                         .WithEntityAccess())
            {
                var position = positionRO.ValueRO.Receiver;
                var bufferPerceive = buffersPerceive[receiver];
                var bufferMemory = buffersMemory[receiver];
                var bufferCone = buffersCone[receiver];

                foreach (var cone in bufferCone)
                {
                    if (!IsPerceived(cone.Source, bufferPerceive) && !IsMemorized(cone.Source, bufferMemory))
                    {
                        Debug.DrawLine(position, cone.Position, Color.red);
                        DebugAdvanced.DrawOctahedron(cone.Position, new float3(0.25f, 0.5f, 0.25f), Color.red);
                    }
                }

                foreach (var perceive in bufferPerceive)
                {
                    Debug.DrawLine(position, perceive.Position, Color.green);
                    DebugAdvanced.DrawOctahedron(perceive.Position, new float3(0.25f, 0.5f, 0.25f), Color.green);
                }

                foreach (var memory in bufferMemory)
                {
                    var sourcePosition = SystemAPI.GetComponentRO<ComponentSightPosition>(memory.Source).ValueRO.Source;

                    Debug.DrawLine(position, memory.Position, Color.yellow);
                    Debug.DrawLine(sourcePosition, memory.Position, Color.yellow);
                    DebugAdvanced.DrawOctahedron(memory.Position, new float3(0.125f, 0.25f, 0.125f), Color.yellow);
                    DebugAdvanced.DrawOctahedron(sourcePosition, new float3(0.25f, 0.5f, 0.25f), Color.yellow);
                }
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightDebug>()
                         .WithAll<BufferSightCone, BufferSightPerceive>()
                         .WithNone<BufferSightMemory>()
                         .WithEntityAccess())
            {
                var position = positionRO.ValueRO.Receiver;
                var bufferPerceive = buffersPerceive[receiver];
                var bufferCone = buffersCone[receiver];

                foreach (var cone in bufferCone)
                {
                    if (!IsPerceived(cone.Source, bufferPerceive))
                    {
                        Debug.DrawLine(position, cone.Position, Color.red);
                        DebugAdvanced.DrawOctahedron(cone.Position, new float3(0.25f, 0.5f, 0.25f), Color.red);
                    }
                }

                foreach (var perceive in bufferPerceive)
                {
                    Debug.DrawLine(position, perceive.Position, Color.green);
                    DebugAdvanced.DrawOctahedron(perceive.Position, new float3(0.25f, 0.5f, 0.25f), Color.green);
                }
            }

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

        private bool IsPerceived(Entity source, DynamicBuffer<BufferSightPerceive> bufferPerceive)
        {
            foreach (var perceive in bufferPerceive)
            {
                if (perceive.Source == source)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsMemorized(Entity source, DynamicBuffer<BufferSightMemory> bufferMemory)
        {
            foreach (var memory in bufferMemory)
            {
                if (memory.Source == source)
                {
                    return true;
                }
            }

            return false;
        }
    }
}