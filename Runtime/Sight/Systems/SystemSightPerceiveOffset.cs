using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [BurstCompile, UpdateAfter(typeof(SystemSightPerceiveSingle))]
    public partial struct SystemSightPerceiveOffset : ISystem
    {
        private EntityQuery _queryWithMemoryWithChildWithClip;
        private EntityQuery _queryWithMemoryWithChild;
        private EntityQuery _queryWithMemoryWithClip;
        private EntityQuery _queryWithChildWithClip;
        private EntityQuery _queryWithMemory;
        private EntityQuery _queryWithChild;
        private EntityQuery _queryWithClip;
        private EntityQuery _query;

        private EntityTypeHandle _handleEntity;

        private BufferTypeHandle<BufferSightRayOffset> _handleBufferRayOffset;
        private BufferTypeHandle<BufferSightPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferSightMemory> _handleBufferMemory;
        private BufferTypeHandle<BufferSightChild> _handleBufferChild;
        private BufferTypeHandle<BufferSightCone> _handleBufferCone;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightMemory> _handleMemory;
        private ComponentTypeHandle<ComponentSightFilter> _handleFilter;
        private ComponentTypeHandle<ComponentSightClip> _handleClip;

        private BufferLookup<BufferSightChild> _lookupBufferChild;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _queryWithMemoryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<ComponentSightMemory, BufferSightChild, ComponentSightClip>().Build();
            _queryWithMemoryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<BufferSightChild, ComponentSightMemory>().WithNone<ComponentSightClip>().Build();
            _queryWithMemoryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<ComponentSightMemory, ComponentSightClip>().WithNone<BufferSightChild>().Build();
            _queryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<BufferSightChild, ComponentSightClip>().WithNone<ComponentSightMemory>().Build();
            _queryWithMemory = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<ComponentSightMemory>().WithNone<BufferSightChild, ComponentSightClip>().Build();
            _queryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<BufferSightChild>().WithNone<ComponentSightMemory, ComponentSightClip>().Build();
            _queryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithAll<ComponentSightClip>().WithNone<BufferSightChild, ComponentSightMemory>().Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>().WithNone<BufferSightChild, ComponentSightMemory, ComponentSightClip>().Build();

            _handleEntity = SystemAPI.GetEntityTypeHandle();

            _handleBufferRayOffset = SystemAPI.GetBufferTypeHandle<BufferSightRayOffset>(isReadOnly: true);
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
            _handleEntity.Update(ref state);

            _handleBufferRayOffset.Update(ref state);
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
                HandleBufferRayOffset = _handleBufferRayOffset, HandleBufferCone = _handleBufferCone,
                HandlePosition = _handlePosition, HandleFilter = _handleFilter,
                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            };

            var jobHandle = new JobUpdatePerceiveWithMemoryWithChildWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferMemory = _handleBufferMemory, HandleBufferChild = _handleBufferChild, HandleMemory = _handleMemory, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithMemoryWithChildWithClip, state.Dependency);

            jobHandle = new JobUpdatePerceiveWithMemoryWithChild
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferMemory = _handleBufferMemory, HandleBufferChild = _handleBufferChild, HandleMemory = _handleMemory,
            }.ScheduleParallel(_queryWithMemoryWithChild, jobHandle);

            jobHandle = new JobUpdatePerceiveWithMemoryWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferMemory = _handleBufferMemory, HandleMemory = _handleMemory, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithMemoryWithClip, jobHandle);

            jobHandle = new JobUpdatePerceiveWithChildWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferChild = _handleBufferChild, HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithChildWithClip, jobHandle);

            jobHandle = new JobUpdatePerceiveWithMemory
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferMemory = _handleBufferMemory, HandleMemory = _handleMemory,
            }.ScheduleParallel(_queryWithMemory, jobHandle);

            jobHandle = new JobUpdatePerceiveWithChild
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleBufferChild = _handleBufferChild,
            }.ScheduleParallel(_queryWithChild, jobHandle);

            jobHandle = new JobUpdatePerceiveWithClip
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
                HandleClip = _handleClip,
            }.ScheduleParallel(_queryWithClip, jobHandle);

            state.Dependency = new JobUpdatePerceive
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles,
            }.ScheduleParallel(_query, jobHandle);
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChildWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isHitByRayOffset = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit, clip.RadiusSquared);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                            {
                                isHitByRayOffset = true;
                                break;
                            }
                        }

                        if (isPerceived && !isHitByRayOffset)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    SystemSightPerceiveSingle.PerceiveToMemory(ref bufferPerceive, perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChild : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isHitByRayOffset = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                            {
                                isHitByRayOffset = true;
                                break;
                            }
                        }

                        if (isPerceived && !isHitByRayOffset)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    SystemSightPerceiveSingle.PerceiveToMemory(ref bufferPerceive, perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isHitByRayOffset = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit, clip.RadiusSquared);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                            {
                                isHitByRayOffset = true;
                                break;
                            }
                        }

                        if (isPerceived && !isHitByRayOffset)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    SystemSightPerceiveSingle.PerceiveToMemory(ref bufferPerceive, perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChildWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferChild = buffersChild[i];
                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit, clip.RadiusSquared);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemory : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [WriteOnly] public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var memory = memories[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = SystemSightPerceiveSingle.IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isHitByRayOffset = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref bufferMemory, ref CommonHandles.LookupBufferChild))
                            {
                                isHitByRayOffset = true;
                                break;
                            }
                        }

                        if (isPerceived && !isHitByRayOffset)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    SystemSightPerceiveSingle.PerceiveToMemory(ref bufferPerceive, perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChild : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var bufferChild = buffersChild[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in bufferChild,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithClip : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver,
                            in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit, clip.RadiusSquared);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver,
                                in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit, clip.RadiusSquared);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceive : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var receiver, out var bufferRayOffset, out var bufferCone, out var position, out var filter);

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in cone.Position, in filter.Value, ref CommonHandles.CollisionWorld, out var hit);
                        if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                        {
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position.Receiver);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            SystemSightPerceiveSingle.CastRay(in receiver, in position.Receiver, in sourcePosition, in filter.Value, ref CommonHandles.CollisionWorld, out hit);
                            if (SystemSightPerceiveSingle.ProcessHit(in hit, in cone, ref bufferPerceive, ref CommonHandles.LookupBufferChild))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct CommonHandles
        {
            public EntityTypeHandle HandleEntity;

            public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
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

                    BuffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset),
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

            public BufferAccessor<BufferSightRayOffset> BuffersRayOffset;
            public BufferAccessor<BufferSightCone> BuffersCone;

            public NativeArray<ComponentSightPosition> Positions;
            public NativeArray<ComponentSightFilter> Filters;

            [BurstCompile]
            public void Get(int index,
                out Entity receiver, out DynamicBuffer<BufferSightRayOffset> bufferRayOffset, out DynamicBuffer<BufferSightCone> bufferCone,
                out ComponentSightPosition position, out ComponentSightFilter filters)
            {
                receiver = Entities[index];

                bufferRayOffset = BuffersRayOffset[index];
                bufferCone = BuffersCone[index];

                position = Positions[index];
                filters = Filters[index];
            }
        }
    }
}