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

        private EntityQuery _queryConeWithExtend;
        private EntityQuery _queryCone;
        private EntityQuery _queryPerceivedHiddenMemorized;
        private EntityQuery _queryPerceivedHidden;

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

            _queryConeWithExtend = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithAll<ComponentSightExtend>().Build();
            _queryCone = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<ComponentSightPosition, ComponentSightCone, LocalToWorld>().WithNone<ComponentSightExtend>().Build();
            _queryPerceivedHiddenMemorized = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
                .WithAll<BufferSightPerceive, BufferSightCone, ComponentSightPosition>().WithAll<BufferSightMemory, ComponentSightMemory>().Build();
            _queryPerceivedHidden = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, TagSightDebug>()
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

            var jobHandle = new JobDebugConeWithExtend { Debug = debug }.ScheduleParallel(_queryConeWithExtend, state.Dependency);
            jobHandle = new JobDebugCone { Debug = debug }.ScheduleParallel(_queryCone, jobHandle);
            jobHandle = new JobDebugPerceivedHiddenMemorized { Debug = debug, LookupPosition = _lookupPosition }.ScheduleParallel(_queryPerceivedHiddenMemorized, jobHandle);
            state.Dependency = new JobDebugPerceivedHidden { Debug = debug }.ScheduleParallel(_queryPerceivedHidden, jobHandle);
            state.Dependency.Complete();
        }

        [BurstCompile]
        private partial struct JobDebugConeWithExtend : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform, in ComponentSightExtend extend)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var clip = math.sqrt(cone.ClipSquared);
                var angles = math.acos(cone.AnglesCos);
                var extendRadius = math.sqrt(extend.RadiusSquared);
                var extendAngles = math.acos(extend.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clip, extendRadius, extendAngles, Debug.ColorReceiverExtend);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clip, radius, angles, Debug.ColorReceiverCone);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clip, extendAngles, Debug.ColorReceiverClip);
            }
        }

        [BurstCompile]
        private partial struct JobDebugCone : IJobEntity
        {
            [ReadOnly] public ComponentSightDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentSightPosition position, in ComponentSightCone cone, in LocalToWorld transform)
            {
                var radius = math.sqrt(cone.RadiusSquared);
                var clip = math.sqrt(cone.ClipSquared);
                var angles = math.acos(cone.AnglesCos);

                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, clip, radius, angles, Debug.ColorReceiverCone);
                SightSenseAuthoring.DrawCone(position.Receiver, transform.Rotation, 0, clip, angles, Debug.ColorReceiverClip);
            }
        }

        [BurstCompile]
        private partial struct JobDebugPerceivedHiddenMemorized : IJobEntity
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
                    UnityEngine.Debug.DrawLine(position.Receiver, memory.Position, Debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(memory.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronSmall, Debug.ColorSourceMemorized);

                    if (LookupPosition.TryGetComponent(memory.Source, out var sourcePosition))
                    {
                        UnityEngine.Debug.DrawLine(sourcePosition.Source, memory.Position, Debug.ColorSourceMemorized);
                        DebugAdvanced.DrawOctahedron(sourcePosition.Source, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, Debug.ColorSourceMemorized);
                    }
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
        private partial struct JobDebugPerceivedHidden : IJobEntity
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