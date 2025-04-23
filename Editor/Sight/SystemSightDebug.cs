using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception.Editor
{
    [BurstCompile, UpdateInGroup(typeof(SightSystemGroup), OrderLast = true)]
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
            state.RequireForUpdate<ComponentSightDebug>();

            if (!SystemAPI.HasSingleton<ComponentSightDebug>())
            {
                state.EntityManager.CreateSingleton(ComponentSightDebug.Default);
            }

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

            ref readonly var debug = ref SystemAPI.GetSingletonRW<ComponentSightDebug>().ValueRO;

            var jobHandle = new JobDebugReceiverWithExtendWithClip { Debug = debug }.ScheduleParallel(_queryWithExtendWithClip, state.Dependency);
            jobHandle = new JobDebugReceiverWithExtend { Debug = debug }.ScheduleParallel(_queryWithExtend, jobHandle);
            jobHandle = new JobDebugReceiverWithClip { Debug = debug }.ScheduleParallel(_queryWithClip, jobHandle);
            jobHandle = new JobDebugReceiver { Debug = debug }.ScheduleParallel(_query, jobHandle);
            jobHandle = new JobDebugSourceWithMemory { Debug = debug, LookupPosition = _lookupPosition, }.ScheduleParallel(_queryPerceiveWithMemory, jobHandle);
            state.Dependency = new JobDebugSource { Debug = debug }.ScheduleParallel(_queryPerceive, jobHandle);
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithExtendWithClip : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform,
                in ComponentSightExtend extend, in ComponentSightClip clip)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var extendRadius = math.sqrt(extend.RadiusSquared);
                var extendAngles = math.acos(extend.AnglesCos);
                var clipRadius = math.sqrt(clip.RadiusSquared);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, extendRadius, extendAngles, Debug.ColorReceiverExtend);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, radius, angles, Debug.ColorReceiverCone);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clipRadius, extendAngles, Debug.ColorReceiverClip);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithExtend : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform, in ComponentSightExtend extend)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var extendRadius = math.sqrt(extend.RadiusSquared);
                var extendAngles = math.acos(extend.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, extendRadius, extendAngles, Debug.ColorReceiverExtend);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, radius, angles, Debug.ColorReceiverCone);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiverWithClip : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform, in ComponentSightClip clip)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);
                var clipRadius = math.sqrt(clip.RadiusSquared);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clipRadius, radius, angles, Debug.ColorReceiverCone);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clipRadius, angles, Debug.ColorReceiverClip);
            }
        }

        [BurstCompile]
        private partial struct JobDebugReceiver : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var angles = math.acos(cone.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, radius, angles, Debug.ColorReceiverCone);
            }
        }

        [BurstCompile]
        private partial struct JobDebugSourceWithMemory : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;

            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferSightPerceive> bufferPerceive, in DynamicBuffer<BufferSightMemory> bufferMemory,
                in DynamicBuffer<BufferSightCone> bufferCone, in ComponentSightPosition position)
            {
                foreach (var perceive in bufferPerceive)
                {
                    UnityEngine.Debug.DrawLine(position.Receiver, perceive.Position, Debug.ColorSourcePerceived);
                    DebugAdvanced.DrawOctahedron(perceive.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourcePerceived);
                }

                foreach (var memory in bufferMemory)
                {
                    var sourcePosition = LookupPosition[memory.Source].Source;

                    UnityEngine.Debug.DrawLine(position.Receiver, memory.Position, Debug.ColorSourceMemorized);
                    UnityEngine.Debug.DrawLine(sourcePosition, memory.Position, Debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(memory.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronSmall, Debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(sourcePosition, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourceMemorized);
                }

                foreach (var cone in bufferCone)
                {
                    if (!bufferPerceive.Contains(in cone.Source, bufferPerceive.Length) && !bufferMemory.Contains(in cone.Source))
                    {
                        UnityEngine.Debug.DrawLine(position.Receiver, cone.Position, Debug.ColorSourceHidden);
                        DebugAdvanced.DrawOctahedron(cone.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourceHidden);
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct JobDebugSource : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferSightPerceive> bufferPerceive, in DynamicBuffer<BufferSightCone> bufferCone, in ComponentSightPosition position)
            {
                foreach (var perceive in bufferPerceive)
                {
                    UnityEngine.Debug.DrawLine(position.Receiver, perceive.Position, Debug.ColorSourcePerceived);
                    DebugAdvanced.DrawOctahedron(perceive.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourcePerceived);
                }

                foreach (var cone in bufferCone)
                {
                    if (!bufferPerceive.Contains(in cone.Source, bufferPerceive.Length))
                    {
                        UnityEngine.Debug.DrawLine(position.Receiver, cone.Position, Debug.ColorSourceHidden);
                        DebugAdvanced.DrawOctahedron(cone.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourceHidden);
                    }
                }
            }
        }
    }
}