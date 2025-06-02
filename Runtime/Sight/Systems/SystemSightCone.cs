using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(FixedSightSystemGroup)), UpdateAfter(typeof(SystemSightPosition))]
    public partial struct SystemSightCone : ISystem
    {
        private EntityQuery _queryWithoutPerceive;
        private EntityQuery _queryWithoutCone;
        private EntityQuery _queryWithoutFilter;

        private EntityQuery _querySources;

        private EntityQuery _queryWithExtend;
        private EntityQuery _query;

        private BufferTypeHandle<BufferSightPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferSightCone> _handleBufferCone;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightExtend> _handleExtend;
        private ComponentTypeHandle<ComponentSightFilter> _handleFilter;
        private ComponentTypeHandle<ComponentSightCone> _handleCone;
        private ComponentTypeHandle<LocalToWorld> _handleTransform;

        private ComponentLookup<ComponentSightPosition> _lookupPosition;
        private ComponentLookup<ComponentSightFilter> _lookupFilter;

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPerceive = SystemAPI.QueryBuilder().WithAll<TagSightReceiver>().WithNone<BufferSightPerceive>().Build();
            _queryWithoutCone = SystemAPI.QueryBuilder().WithAll<TagSightReceiver>().WithNone<BufferSightCone>().Build();
            _queryWithoutFilter = SystemAPI.QueryBuilder().WithAny<TagSightReceiver, TagSightSource>().WithNone<ComponentSightFilter>().Build();

            _querySources = SystemAPI.QueryBuilder().WithAll<TagSightSource>().Build();

            _queryWithExtend = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, ComponentSightCone>().WithAll<ComponentSightExtend>().Build();
            _query = SystemAPI.QueryBuilder().WithAll<TagSightReceiver, ComponentSightCone>().WithNone<ComponentSightExtend>().Build();

            _handleBufferPerceive = SystemAPI.GetBufferTypeHandle<BufferSightPerceive>(isReadOnly: true);
            _handleBufferCone = SystemAPI.GetBufferTypeHandle<BufferSightCone>();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>(isReadOnly: true);
            _handleExtend = SystemAPI.GetComponentTypeHandle<ComponentSightExtend>(isReadOnly: true);
            _handleFilter = SystemAPI.GetComponentTypeHandle<ComponentSightFilter>(isReadOnly: true);
            _handleCone = SystemAPI.GetComponentTypeHandle<ComponentSightCone>(isReadOnly: true);
            _handleTransform = SystemAPI.GetComponentTypeHandle<LocalToWorld>(isReadOnly: true);

            _lookupPosition = SystemAPI.GetComponentLookup<ComponentSightPosition>(isReadOnly: true);
            _lookupFilter = SystemAPI.GetComponentLookup<ComponentSightFilter>(isReadOnly: true);
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

            foreach (var entity in _queryWithoutFilter.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new ComponentSightFilter { BelongsTo = uint.MaxValue, Perceives = uint.MaxValue });
            }

            commands.Playback(state.EntityManager);

            _handleBufferPerceive.Update(ref state);
            _handleBufferCone.Update(ref state);

            _handlePosition.Update(ref state);
            _handleExtend.Update(ref state);
            _handleFilter.Update(ref state);
            _handleCone.Update(ref state);
            _handleTransform.Update(ref state);

            _lookupPosition.Update(ref state);
            _lookupFilter.Update(ref state);

            var sources = _querySources.ToEntityArray(Allocator.TempJob);

            var commonHandles = new CommonHandles
            {
                HandlePosition = _handlePosition, HandleFilter = _handleFilter, HandleCone = _handleCone, HandleTransform = _handleTransform,
                LookupPosition = _lookupPosition, LookupFilter = _lookupFilter,
                Sources = sources.AsReadOnly(),
            };

            var ranges = new NativeArray<int2>(2, Allocator.Temp);

            if (SystemAPI.TryGetSingleton(out ComponentSightLimit limit) && limit.ChunksAmountCone > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _queryWithExtend.CalculateChunkCountWithoutFiltering();
                amounts[1] = _query.CalculateChunkCountWithoutFiltering();

                ComponentSightLimit.CalculateRanges(limit.ChunksAmountCone, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
            }
            else
            {
                for (var i = 0; i < ranges.Length; i++)
                {
                    ranges[i] = new int2(0, int.MaxValue);
                }
            }

            var jobHandle = new JobUpdateConeWithExtend
            {
                HandleBufferCone = _handleBufferCone, CommonHandles = commonHandles, ChunkIndexRange = ranges[0],
                HandleBufferPerceive = _handleBufferPerceive, HandleExtend = _handleExtend,
            }.ScheduleParallel(_queryWithExtend, state.Dependency);

            jobHandle = new JobUpdateCone
            {
                HandleBufferCone = _handleBufferCone, CommonHandles = commonHandles, ChunkIndexRange = ranges[1],
            }.ScheduleParallel(_query, jobHandle);

            state.Dependency = sources.Dispose(jobHandle);
        }

        [BurstCompile]
        private struct JobUpdateConeWithExtend : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public ComponentTypeHandle<ComponentSightExtend> HandleExtend;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var extends = chunk.GetNativeArray(ref HandleExtend);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    arrays.Get(i, out var position, out var filter, out var cone, out var transform);

                    var bufferPerceive = buffersPerceive[i];
                    var extend = extends[i];

                    bufferCone.Clear();

                    foreach (var source in CommonHandles.Sources)
                    {
                        if (!filter.CanPerceive(CommonHandles.LookupFilter[source]))
                        {
                            continue;
                        }

                        var sourcePosition = CommonHandles.LookupPosition[source].Source;

                        if (bufferPerceive.Contains(in source)
                                ? extend.IsInside(in position.Receiver, in sourcePosition, in transform)
                                : cone.IsInside(in position.Receiver, in sourcePosition, in transform))
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
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);
                CommonHandles.Get(in chunk, out var arrays);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferCone = buffersCone[i];
                    arrays.Get(i, out var position, out var filter, out var cone, out var transform);

                    bufferCone.Clear();

                    foreach (var source in CommonHandles.Sources)
                    {
                        if (!filter.CanPerceive(CommonHandles.LookupFilter[source]))
                        {
                            continue;
                        }

                        var sourcePosition = CommonHandles.LookupPosition[source].Source;

                        if (cone.IsInside(in position.Receiver, in sourcePosition, in transform))
                        {
                            bufferCone.Add(new BufferSightCone { Position = sourcePosition, Source = source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct CommonHandles
        {
            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            public ComponentTypeHandle<ComponentSightFilter> HandleFilter;
            public ComponentTypeHandle<ComponentSightCone> HandleCone;
            public ComponentTypeHandle<LocalToWorld> HandleTransform;

            public ComponentLookup<ComponentSightPosition> LookupPosition;
            public ComponentLookup<ComponentSightFilter> LookupFilter;

            public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Get(in ArchetypeChunk chunk, out CommonArrays arrays)
            {
                arrays = new CommonArrays
                {
                    Positions = chunk.GetNativeArray(ref HandlePosition),
                    Filters = chunk.GetNativeArray(ref HandleFilter),
                    Cones = chunk.GetNativeArray(ref HandleCone),
                    Transforms = chunk.GetNativeArray(ref HandleTransform),
                };
            }
        }

        [BurstCompile]
        private struct CommonArrays
        {
            public NativeArray<ComponentSightPosition> Positions;
            public NativeArray<ComponentSightFilter> Filters;
            public NativeArray<ComponentSightCone> Cones;
            public NativeArray<LocalToWorld> Transforms;

            [BurstCompile]
            public void Get(int index, out ComponentSightPosition position, out ComponentSightFilter filter, out ComponentSightCone cone, out LocalToWorld transform)
            {
                position = Positions[index];
                filter = Filters[index];
                cone = Cones[index];
                transform = Transforms[index];
            }
        }
    }
}