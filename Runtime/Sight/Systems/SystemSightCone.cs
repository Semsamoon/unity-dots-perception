using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [BurstCompile, UpdateAfter(typeof(SystemSightPosition))]
    public partial struct SystemSightCone : ISystem
    {
        private EntityQuery _queryWithoutCone;
        private EntityQuery _queryWithoutPerceive;
        private EntityQuery _querySources;
        private EntityQuery _queryWithExtendWithClip;
        private EntityQuery _queryWithExtend;
        private EntityQuery _queryWithClip;
        private EntityQuery _query;

        private BufferTypeHandle<BufferSightPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferSightCone> _handleBufferCone;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightExtend> _handleExtend;
        private ComponentTypeHandle<ComponentSightCone> _handleCone;
        private ComponentTypeHandle<ComponentSightClip> _handleClip;
        private ComponentTypeHandle<LocalToWorld> _handleTransform;

        private ComponentLookup<ComponentSightPosition> _lookupPosition;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPerceive = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver>()
                .WithNone<BufferSightPerceive>()
                .Build();
            _queryWithoutCone = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver>()
                .WithNone<BufferSightCone>()
                .Build();
            _querySources = SystemAPI.QueryBuilder()
                .WithAll<TagSightSource, ComponentSightPosition>()
                .Build();
            _queryWithExtendWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, BufferSightCone, BufferSightPerceive>()
                .WithAll<LocalToWorld, ComponentSightPosition, ComponentSightCone>()
                .WithAll<ComponentSightExtend, ComponentSightClip>()
                .Build();
            _queryWithExtend = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, BufferSightCone, BufferSightPerceive>()
                .WithAll<LocalToWorld, ComponentSightPosition, ComponentSightCone>()
                .WithAll<ComponentSightExtend>()
                .WithNone<ComponentSightClip>()
                .Build();
            _queryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, BufferSightCone, BufferSightPerceive>()
                .WithAll<LocalToWorld, ComponentSightPosition, ComponentSightCone>()
                .WithAll<ComponentSightClip>()
                .WithNone<ComponentSightExtend>()
                .Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, BufferSightCone, BufferSightPerceive>()
                .WithAll<LocalToWorld, ComponentSightPosition, ComponentSightCone>()
                .WithNone<ComponentSightExtend, ComponentSightClip>()
                .Build();

            _handleBufferPerceive = SystemAPI.GetBufferTypeHandle<BufferSightPerceive>(isReadOnly: true);
            _handleBufferCone = SystemAPI.GetBufferTypeHandle<BufferSightCone>();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>(isReadOnly: true);
            _handleExtend = SystemAPI.GetComponentTypeHandle<ComponentSightExtend>(isReadOnly: true);
            _handleCone = SystemAPI.GetComponentTypeHandle<ComponentSightCone>(isReadOnly: true);
            _handleClip = SystemAPI.GetComponentTypeHandle<ComponentSightClip>(isReadOnly: true);
            _handleTransform = SystemAPI.GetComponentTypeHandle<LocalToWorld>(isReadOnly: true);

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

            foreach (var receiver in _queryWithoutCone.ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightCone>(receiver);
            }

            foreach (var receiver in _queryWithoutPerceive.ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightPerceive>(receiver);
            }

            commands.Playback(state.EntityManager);

            _handleBufferPerceive.Update(ref state);
            _handleBufferCone.Update(ref state);

            _handlePosition.Update(ref state);
            _handleExtend.Update(ref state);
            _handleCone.Update(ref state);
            _handleClip.Update(ref state);
            _handleTransform.Update(ref state);

            _lookupPosition.Update(ref state);

            var sources = _querySources.ToEntityArray(Allocator.TempJob);
            var sourcesReadOnly = sources.AsReadOnly();

            var jobWithExtendWithClip = new JobUpdateConeWithExtendWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleExtend = _handleExtend,
                HandleCone = _handleCone,
                HandleClip = _handleClip,
                HandleTransform = _handleTransform,

                LookupPosition = _lookupPosition,
                Sources = sourcesReadOnly,
            }.ScheduleParallel(_queryWithExtendWithClip, state.Dependency);

            var jobWithExtend = new JobUpdateConeWithExtend
            {
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleExtend = _handleExtend,
                HandleCone = _handleCone,
                HandleTransform = _handleTransform,

                LookupPosition = _lookupPosition,
                Sources = sourcesReadOnly,
            }.ScheduleParallel(_queryWithExtend, jobWithExtendWithClip);

            var jobWithClip = new JobUpdateConeWithClip
            {
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleCone = _handleCone,
                HandleClip = _handleClip,
                HandleTransform = _handleTransform,

                LookupPosition = _lookupPosition,
                Sources = sourcesReadOnly,
            }.ScheduleParallel(_queryWithClip, jobWithExtend);

            var job = new JobUpdateCone
            {
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleCone = _handleCone,
                HandleTransform = _handleTransform,

                LookupPosition = _lookupPosition,
                Sources = sourcesReadOnly,
            }.ScheduleParallel(_query, jobWithClip);

            var dispose = sources.Dispose(job);

            state.Dependency = dispose;
        }

        [BurstCompile]
        private static bool IsInsideCone(in float3 origin, in float3 target,
            in LocalToWorld transform, in float2 anglesCos, float radiusSquared, float clipSquared)
        {
            var difference = target - origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > radiusSquared || distanceSquared < clipSquared)
            {
                return false;
            }

            var directionLocal = transform.Value.InverseTransformDirection(difference);
            var xSquared = directionLocal.x * directionLocal.x;
            var ySquared = directionLocal.y * directionLocal.y;
            var zSquared = directionLocal.z * directionLocal.z;

            return directionLocal.z / math.sqrt(xSquared + zSquared) >= anglesCos.x
                   && math.sqrt((xSquared + zSquared) / (xSquared + ySquared + zSquared)) >= anglesCos.y;
        }

        [BurstCompile]
        private static bool IsInsideCone(in float3 origin, in float3 target,
            in LocalToWorld transform, in float2 anglesCos, float radiusSquared)
        {
            var difference = target - origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > radiusSquared)
            {
                return false;
            }

            var directionLocal = transform.Value.InverseTransformDirection(difference);
            var xSquared = directionLocal.x * directionLocal.x;
            var ySquared = directionLocal.y * directionLocal.y;
            var zSquared = directionLocal.z * directionLocal.z;

            return directionLocal.z / math.sqrt(xSquared + zSquared) >= anglesCos.x
                   && math.sqrt((xSquared + zSquared) / (xSquared + ySquared + zSquared)) >= anglesCos.y;
        }

        [BurstCompile]
        private static bool IsPerceived(in Entity entity, in DynamicBuffer<BufferSightPerceive> bufferPerceive)
        {
            foreach (var perceive in bufferPerceive)
            {
                if (perceive.Source == entity)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private struct JobUpdateConeWithExtendWithClip : IJobChunk
        {
            [ReadOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [WriteOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightExtend> HandleExtend;
            [ReadOnly] public ComponentTypeHandle<ComponentSightCone> HandleCone;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;
            [ReadOnly] public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var extends = chunk.GetNativeArray(ref HandleExtend);
                var cones = chunk.GetNativeArray(ref HandleCone);
                var clips = chunk.GetNativeArray(ref HandleClip);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    var extend = extends[i];
                    var cone = cones[i];

                    bufferCone.Clear();

                    foreach (var source in Sources)
                    {
                        var sourcePosition = LookupPosition[source].Source;
                        var (anglesCos, radiusSquared) = IsPerceived(in source, buffersPerceive[i])
                            ? (extend.AnglesCos, extend.RadiusSquared)
                            : (cone.AnglesCos, cone.RadiusSquared);

                        if (IsInsideCone(positions[i].Receiver, in sourcePosition,
                                transforms[i], in anglesCos, radiusSquared, clips[i].RadiusSquared))
                        {
                            bufferCone.Add(new BufferSightCone { Position = sourcePosition, Source = source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdateConeWithExtend : IJobChunk
        {
            [ReadOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [WriteOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightExtend> HandleExtend;
            [ReadOnly] public ComponentTypeHandle<ComponentSightCone> HandleCone;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;
            [ReadOnly] public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var extends = chunk.GetNativeArray(ref HandleExtend);
                var cones = chunk.GetNativeArray(ref HandleCone);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    var extend = extends[i];
                    var cone = cones[i];

                    bufferCone.Clear();

                    foreach (var source in Sources)
                    {
                        var sourcePosition = LookupPosition[source].Source;
                        var (anglesCos, radiusSquared) = IsPerceived(in source, buffersPerceive[i])
                            ? (extend.AnglesCos, extend.RadiusSquared)
                            : (cone.AnglesCos, cone.RadiusSquared);

                        if (IsInsideCone(positions[i].Receiver, in sourcePosition,
                                transforms[i], in anglesCos, radiusSquared))
                        {
                            bufferCone.Add(new BufferSightCone { Position = sourcePosition, Source = source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdateConeWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightCone> HandleCone;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;
            [ReadOnly] public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var cones = chunk.GetNativeArray(ref HandleCone);
                var clips = chunk.GetNativeArray(ref HandleClip);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    var cone = cones[i];

                    bufferCone.Clear();

                    foreach (var source in Sources)
                    {
                        var sourcePosition = LookupPosition[source].Source;

                        if (IsInsideCone(positions[i].Receiver, in sourcePosition,
                                transforms[i], in cone.AnglesCos, cone.RadiusSquared, clips[i].RadiusSquared))
                        {
                            bufferCone.Add(new BufferSightCone { Position = sourcePosition, Source = source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdateCone : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightCone> HandleCone;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [ReadOnly] public ComponentLookup<ComponentSightPosition> LookupPosition;
            [ReadOnly] public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var cones = chunk.GetNativeArray(ref HandleCone);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    var cone = cones[i];

                    bufferCone.Clear();

                    foreach (var source in Sources)
                    {
                        var sourcePosition = LookupPosition[source].Source;

                        if (IsInsideCone(positions[i].Receiver, in sourcePosition,
                                transforms[i], in cone.AnglesCos, cone.RadiusSquared))
                        {
                            bufferCone.Add(new BufferSightCone { Position = sourcePosition, Source = source });
                        }
                    }
                }
            }
        }
    }
}