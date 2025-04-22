using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Perception.Editor
{
    [BurstCompile]
    public partial struct SystemSightDebug : ISystem
    {
        private EntityQuery _queryWithoutDebug;

        private EntityQuery _queryWithExtendWithClip;
        private EntityQuery _queryWithExtend;
        private EntityQuery _queryWithClip;
        private EntityQuery _query;
        private EntityQuery _queryPerceiveWithMemory;
        private EntityQuery _queryPerceive;

        private ComponentLookup<ComponentSightPosition> _lookupPosition;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutDebug = SystemAPI.QueryBuilder().WithAll<TagSightReceiver>().WithAbsent<TagSightDebug>().Build();

            _queryWithExtendWithClip = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithAll<ComponentSightExtend, ComponentSightClip>().Build();
            _queryWithExtend = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithAll<ComponentSightExtend>().WithNone<ComponentSightClip>().Build();
            _queryWithClip = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithAll<ComponentSightClip>().WithNone<ComponentSightExtend>().Build();
            _query = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithNone<ComponentSightExtend, ComponentSightClip>().Build();
            _queryPerceiveWithMemory = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<BufferSightPerceive, BufferSightCone, ComponentSightPosition>().WithAll<BufferSightMemory, ComponentSightMemory>().Build();
            _queryPerceive = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<BufferSightPerceive, BufferSightCone, ComponentSightPosition>().WithNone<ComponentSightMemory>().Build();

            _lookupPosition = SystemAPI.GetComponentLookup<ComponentSightPosition>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in _queryWithoutDebug.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent<TagSightDebug>(receiver);
                commands.SetComponentEnabled<TagSightDebug>(receiver, false);
            }

            commands.Playback(state.EntityManager);

            _lookupPosition.Update(ref state);

            var jobHandle = new JobDebugReceiverWithExtendWithClip().ScheduleParallel(_queryWithExtendWithClip, state.Dependency);
            jobHandle = new JobDebugReceiverWithExtend().ScheduleParallel(_queryWithExtend, jobHandle);
            jobHandle = new JobDebugReceiverWithClip().ScheduleParallel(_queryWithClip, jobHandle);
            jobHandle = new JobDebugReceiver().ScheduleParallel(_query, jobHandle);
            jobHandle = new JobDebugSourceWithMemory { LookupPosition = _lookupPosition, }.ScheduleParallel(_queryPerceiveWithMemory, jobHandle);
            state.Dependency = new JobDebugSource().ScheduleParallel(_queryPerceive, jobHandle);
        }

        [BurstCompile]
        public static bool IsMemorized(in Entity entity, in DynamicBuffer<BufferSightMemory> bufferMemory)
        {
            foreach (var memory in bufferMemory)
            {
                if (memory.Source == entity)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithExtendWithClip : IJobEntity
        {
            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform,
                in ComponentSightExtend extend, in ComponentSightClip clip)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var extendRadius = math.sqrt(extend.RadiusSquared);
                var extendAngles = math.acos(extend.AnglesCos);
                var clipRadius = math.sqrt(clip.RadiusSquared);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, extendRadius, extendAngles, Color.yellow);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, radius, angles, Color.green);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clipRadius, extendAngles, Color.gray);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithExtend : IJobEntity
        {
            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform, in ComponentSightExtend extend)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var extendRadius = math.sqrt(extend.RadiusSquared);
                var extendAngles = math.acos(extend.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, extendRadius, extendAngles, Color.yellow);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, radius, angles, Color.green);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithClip : IJobEntity
        {
            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform, in ComponentSightClip clip)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var clipRadius = math.sqrt(clip.RadiusSquared);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, radius, angles, Color.green);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clipRadius, angles, Color.gray);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiver : IJobEntity
        {
            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, radius, angles, Color.green);
            }
        }

        [BurstCompile]
        private partial struct JobDebugSourceWithMemory : IJobEntity
        {
            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;

            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferSightPerceive> bufferPerceive, in DynamicBuffer<BufferSightMemory> bufferMemory,
                in DynamicBuffer<BufferSightCone> bufferCone, in ComponentSightPosition position)
            {
                foreach (var perceive in bufferPerceive)
                {
                    Debug.DrawLine(position.Receiver, perceive.Position, Color.green);
                    DebugAdvanced.DrawOctahedron(perceive.Position, new float3(0.25f, 0.5f, 0.25f), Color.green);
                }

                foreach (var memory in bufferMemory)
                {
                    var sourcePosition = LookupPosition[memory.Source].Source;

                    Debug.DrawLine(position.Receiver, memory.Position, Color.yellow);
                    Debug.DrawLine(sourcePosition, memory.Position, Color.yellow);
                    DebugAdvanced.DrawOctahedron(memory.Position, new float3(0.125f, 0.25f, 0.125f), Color.yellow);
                    DebugAdvanced.DrawOctahedron(sourcePosition, new float3(0.25f, 0.5f, 0.25f), Color.yellow);
                }

                foreach (var cone in bufferCone)
                {
                    if (!SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, bufferPerceive.Length, out _, out _)
                        && !IsMemorized(cone.Source, bufferMemory))
                    {
                        Debug.DrawLine(position.Receiver, cone.Position, Color.red);
                        DebugAdvanced.DrawOctahedron(cone.Position, new float3(0.25f, 0.5f, 0.25f), Color.red);
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct JobDebugSource : IJobEntity
        {
            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferSightPerceive> bufferPerceive, in DynamicBuffer<BufferSightCone> bufferCone, in ComponentSightPosition position)
            {
                foreach (var perceive in bufferPerceive)
                {
                    Debug.DrawLine(position.Receiver, perceive.Position, Color.green);
                    DebugAdvanced.DrawOctahedron(perceive.Position, new float3(0.25f, 0.5f, 0.25f), Color.green);
                }

                foreach (var cone in bufferCone)
                {
                    if (!SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, bufferPerceive.Length, out _, out _))
                    {
                        Debug.DrawLine(position.Receiver, cone.Position, Color.red);
                        DebugAdvanced.DrawOctahedron(cone.Position, new float3(0.25f, 0.5f, 0.25f), Color.red);
                    }
                }
            }
        }
    }
}