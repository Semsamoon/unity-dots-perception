using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(SightSystemGroup)), UpdateAfter(typeof(SystemSightCone)), UpdateAfter(typeof(SystemSightMemory))]
    public partial struct SystemSightPerceiveSingle : ISystem
    {
        private EntityQuery _queryWithoutFilter;

        private EntityQuery _queryWithMemoryWithChildWithClip;
        private EntityQuery _queryWithMemoryWithChild;
        private EntityQuery _queryWithMemoryWithClip;
        private EntityQuery _queryWithChildWithClip;
        private EntityQuery _queryWithMemory;
        private EntityQuery _queryWithChild;
        private EntityQuery _queryWithClip;
        private EntityQuery _query;

        private EntityTypeHandle _handleEntity;

        private BufferTypeHandle<BufferSightPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferSightMemory> _handleBufferMemory;
        private BufferTypeHandle<BufferSightChild> _handleBufferChild;
        private BufferTypeHandle<BufferSightCone> _handleBufferCone;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightMemory> _handleMemory;
        private ComponentTypeHandle<ComponentSightFilter> _handleFilter;
        private ComponentTypeHandle<ComponentSightClip> _handleClip;

        private BufferLookup<BufferSightChild> _lookupBufferChild;

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _queryWithoutFilter = SystemAPI.QueryBuilder().WithAll<TagSightReceiver>().WithNone<ComponentSightFilter>().Build();

            _queryWithMemoryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<BufferSightChild, ComponentSightMemory, ComponentSightClip>().Build();
            _queryWithMemoryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<BufferSightChild, ComponentSightMemory>().WithNone<ComponentSightClip>().Build();
            _queryWithMemoryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<ComponentSightMemory, ComponentSightClip>().WithNone<BufferSightChild>().Build();
            _queryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<BufferSightChild, ComponentSightClip>().WithNone<ComponentSightMemory>().Build();
            _queryWithMemory = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<ComponentSightMemory>().WithNone<BufferSightChild, ComponentSightClip>().Build();
            _queryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<BufferSightChild>().WithNone<ComponentSightMemory, ComponentSightClip>().Build();
            _queryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithAll<ComponentSightClip>().WithNone<BufferSightChild, ComponentSightMemory>().Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle>().WithNone<BufferSightChild, ComponentSightMemory, ComponentSightClip>().Build();

            _handleEntity = SystemAPI.GetEntityTypeHandle();

            _handleBufferPerceive = SystemAPI.GetBufferTypeHandle<BufferSightPerceive>();
            _handleBufferMemory = SystemAPI.GetBufferTypeHandle<BufferSightMemory>();
            _handleBufferChild = SystemAPI.GetBufferTypeHandle<BufferSightChild>(isReadOnly: true);
            _handleBufferCone = SystemAPI.GetBufferTypeHandle<BufferSightCone>(isReadOnly: true);

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>(isReadOnly: true);
            _handleMemory = SystemAPI.GetComponentTypeHandle<ComponentSightMemory>(isReadOnly: true);
            _handleFilter = SystemAPI.GetComponentTypeHandle<ComponentSightFilter>(isReadOnly: true);
            _handleClip = SystemAPI.GetComponentTypeHandle<ComponentSightClip>(isReadOnly: true);

            _lookupBufferChild = SystemAPI.GetBufferLookup<BufferSightChild>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in _queryWithoutFilter.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(receiver, new ComponentSightFilter { Value = CollisionFilter.Default });
            }

            commands.Playback(state.EntityManager);

            _handleEntity.Update(ref state);

            _handleBufferPerceive.Update(ref state);
            _handleBufferMemory.Update(ref state);
            _handleBufferChild.Update(ref state);
            _handleBufferCone.Update(ref state);

            _handlePosition.Update(ref state);
            _handleMemory.Update(ref state);
            _handleFilter.Update(ref state);
            _handleClip.Update(ref state);

            _lookupBufferChild.Update(ref state);

            ref readonly var physics = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            var commonHandles = new CommonHandles
            {
                HandleEntity = _handleEntity,
                HandleBufferCone = _handleBufferCone,
                HandlePosition = _handlePosition, HandleFilter = _handleFilter,
                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            };

            var ranges = new NativeArray<int2>(8, Allocator.Temp);

            if (SystemAPI.TryGetSingleton(out ComponentSightLimit limit) && limit.ChunksAmountPerceiveSingle > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _queryWithMemoryWithChildWithClip.CalculateChunkCountWithoutFiltering();
                amounts[1] = _queryWithMemoryWithChild.CalculateChunkCountWithoutFiltering();
                amounts[2] = _queryWithMemoryWithClip.CalculateChunkCountWithoutFiltering();
                amounts[3] = _queryWithChildWithClip.CalculateChunkCountWithoutFiltering();
                amounts[4] = _queryWithMemory.CalculateChunkCountWithoutFiltering();
                amounts[5] = _queryWithChild.CalculateChunkCountWithoutFiltering();
                amounts[6] = _queryWithClip.CalculateChunkCountWithoutFiltering();
                amounts[7] = _query.CalculateChunkCountWithoutFiltering();

                ComponentSightLimit.CalculateRanges(limit.ChunksAmountPerceiveSingle, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
            }
            else
            {
                for (var i = 0; i < ranges.Length; i++)
                {
                    ranges[i] = new int2(0, int.MaxValue);
                }
            }

            var jobHandle = new JobUpdatePerceiveWithMemoryWithChildWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[0],
                HandleBufferMemory = _handleBufferMemory, HandleBufferChild = _handleBufferChild, HandleMemory = _handleMemory, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithMemoryWithChildWithClip, state.Dependency);

            jobHandle = new JobUpdatePerceiveWithMemoryWithChild
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[1],
                HandleBufferMemory = _handleBufferMemory, HandleBufferChild = _handleBufferChild, HandleMemory = _handleMemory,
            }.ScheduleParallel(_queryWithMemoryWithChild, jobHandle);

            jobHandle = new JobUpdatePerceiveWithMemoryWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[2],
                HandleBufferMemory = _handleBufferMemory, HandleMemory = _handleMemory, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithMemoryWithClip, jobHandle);

            jobHandle = new JobUpdatePerceiveWithChildWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[3],
                HandleBufferChild = _handleBufferChild, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithChildWithClip, jobHandle);

            jobHandle = new JobUpdatePerceiveWithMemory
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[4],
                HandleBufferMemory = _handleBufferMemory, HandleMemory = _handleMemory,
            }.ScheduleParallel(_queryWithMemory, jobHandle);

            jobHandle = new JobUpdatePerceiveWithChild
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[5],
                HandleBufferChild = _handleBufferChild,
            }.ScheduleParallel(_queryWithChild, jobHandle);

            jobHandle = new JobUpdatePerceiveWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[6],
                HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithClip, jobHandle);

            state.Dependency = new JobUpdatePerceive
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[7],
            }.ScheduleParallel(_query, jobHandle);
        }

        [BurstCompile]
        public static void CastRay(in Entity receiver, in float3 position, in DynamicBuffer<BufferSightChild> bufferChild,
            in float3 sourcePosition, in CollisionFilter filter, ref CollisionWorld collisionWorld, out Entity hit, float clipSquare = 0)
        {
            var clipFraction = clipSquare / math.distancesq(sourcePosition, position);
            var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver, bufferChild, clipFraction);
            var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

            collisionWorld.CastRay(raycast, ref collector);
            hit = collector.Hit.Entity;
        }

        [BurstCompile]
        public static void CastRay(in Entity receiver, in float3 position,
            in float3 sourcePosition, in CollisionFilter filter, ref CollisionWorld collisionWorld, out Entity hit, float clipSquare = 0)
        {
            var clipFraction = clipSquare / math.distancesq(sourcePosition, position);
            var collector = new CollectorClosestIgnoreEntityWithClip(receiver, clipFraction);
            var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

            collisionWorld.CastRay(raycast, ref collector);
            hit = collector.Hit.Entity;
        }

        [BurstCompile]
        public static bool ProcessHit(in Entity hit, in BufferSightCone cone,
            ref DynamicBuffer<BufferSightPerceive> bufferPerceive, ref BufferLookup<BufferSightChild> buffersChild)
        {
            if (hit == cone.Source || (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild) && sourceBufferChild.Contains(in hit)))
            {
                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                return true;
            }

            return false;
        }

        [BurstCompile]
        public static bool ProcessHit(in Entity hit, in BufferSightCone cone,
            ref DynamicBuffer<BufferSightPerceive> bufferPerceive, ref DynamicBuffer<BufferSightMemory> bufferMemory, ref BufferLookup<BufferSightChild> buffersChild)
        {
            if (hit == cone.Source || (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild) && sourceBufferChild.Contains(in hit)))
            {
                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                bufferMemory.Remove(in cone.Source);
                return true;
            }

            return false;
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChildWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = bufferPerceive.Contains(in cone.Source, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                        }

                        CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (!ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild) && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChild : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = bufferPerceive.Contains(in cone.Source, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                        }

                        CastRay(in receiver, in position.Receiver, in bufferChild, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (!ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild) && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = bufferPerceive.Contains(in cone.Source, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                        }

                        CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (!ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild) && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChildWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferChild = buffersChild[i];
                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild);
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemory : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var memory = memories[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = bufferPerceive.Contains(in cone.Source, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                        }

                        CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (!ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild) && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChild : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var bufferChild = buffersChild[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        CastRay(in receiver, in position.Receiver, bufferChild, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild);
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild);
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceive : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferCone, out var position, out var filter);

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild);
                    }
                }
            }
        }

        [BurstCompile]
        private struct CommonHandles
        {
            public EntityTypeHandle HandleEntity;

            public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            public ComponentTypeHandle<ComponentSightFilter> HandleFilter;

            public BufferLookup<BufferSightChild> LookupBufferChild;

            public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Get(in ArchetypeChunk chunk, out CommonArrays arrays)
            {
                arrays = new CommonArrays
                {
                    Entities = chunk.GetNativeArray(HandleEntity),

                    BuffersCone = chunk.GetBufferAccessor(ref HandleBufferCone),

                    Positions = chunk.GetNativeArray(ref HandlePosition),
                    Filters = chunk.GetNativeArray(ref HandleFilter),
                };
            }
        }

        [BurstCompile]
        private struct CommonArrays
        {
            public NativeArray<Entity> Entities;

            public BufferAccessor<BufferSightCone> BuffersCone;

            public NativeArray<ComponentSightPosition> Positions;
            public NativeArray<ComponentSightFilter> Filters;

            [BurstCompile]
            public void Get(int index,
                out Entity receiver, out DynamicBuffer<BufferSightCone> bufferCone, out ComponentSightPosition position, out ComponentSightFilter filters)
            {
                receiver = Entities[index];

                bufferCone = BuffersCone[index];

                position = Positions[index];
                filters = Filters[index];
            }
        }
    }

    public struct CollectorClosestIgnoreEntityAndChildWithClip : ICollector<RaycastHit>
    {
        private readonly Entity _entity;
        private readonly DynamicBuffer<BufferSightChild> _bufferChild;
        private readonly float _clip;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits => Hit.Entity == Entity.Null ? 0 : 1;
        public RaycastHit Hit { get; private set; }

        public CollectorClosestIgnoreEntityAndChildWithClip(Entity entity, DynamicBuffer<BufferSightChild> bufferChild, float clip)
        {
            _entity = entity;
            _bufferChild = bufferChild;
            _clip = clip;
            MaxFraction = 1;
            Hit = default;
        }

        public bool AddHit(RaycastHit hit)
        {
            if (hit.Entity == _entity || hit.Fraction < _clip)
            {
                return false;
            }

            foreach (var child in _bufferChild)
            {
                if (hit.Entity == child.Value)
                {
                    return false;
                }
            }

            MaxFraction = hit.Fraction;
            Hit = hit;
            return true;
        }
    }

    public struct CollectorClosestIgnoreEntityWithClip : ICollector<RaycastHit>
    {
        private readonly Entity _entity;
        private readonly float _clip;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits => Hit.Entity == Entity.Null ? 0 : 1;
        public RaycastHit Hit { get; private set; }

        public CollectorClosestIgnoreEntityWithClip(Entity entity, float clip)
        {
            _entity = entity;
            _clip = clip;
            MaxFraction = 1;
            Hit = default;
        }

        public bool AddHit(RaycastHit hit)
        {
            if (hit.Entity == _entity || hit.Fraction < _clip)
            {
                return false;
            }

            MaxFraction = hit.Fraction;
            Hit = hit;
            return true;
        }
    }
}