using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [BurstCompile, UpdateAfter(typeof(SystemSightCone)), UpdateAfter(typeof(SystemSightMemory))]
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
        private EntityTypeHandle _handleEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _queryWithMemoryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory>()
                .WithAll<BufferSightChild, ComponentSightClip>()
                .Build();
            _queryWithMemoryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, BufferSightChild, ComponentSightMemory>()
                .WithNone<ComponentSightClip>()
                .Build();
            _queryWithMemoryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory, ComponentSightClip>()
                .WithNone<BufferSightChild>()
                .Build();
            _queryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightChild, ComponentSightClip>()
                .WithNone<BufferSightMemory, ComponentSightMemory>()
                .Build();
            _queryWithMemory = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory>()
                .WithNone<BufferSightChild, ComponentSightClip>()
                .Build();
            _queryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightChild>()
                .WithNone<BufferSightMemory, ComponentSightMemory, ComponentSightClip>()
                .Build();
            _queryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithAll<ComponentSightClip>()
                .WithNone<BufferSightMemory, BufferSightChild, ComponentSightMemory>()
                .Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRayMultiple>()
                .WithAll<ComponentSightPosition, ComponentSightFilter>()
                .WithAll<BufferSightRayOffset, BufferSightPerceive, BufferSightCone>()
                .WithNone<BufferSightMemory, ComponentSightMemory>()
                .WithNone<BufferSightChild, ComponentSightClip>()
                .Build();

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
            _handleEntity = SystemAPI.GetEntityTypeHandle();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
            _handleEntity.Update(ref state);

            ref readonly var physics = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            var jobUpdatePerceiveWithMemoryWithChildWithClip = new JobUpdatePerceiveWithMemoryWithChildWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleFilter = _handleFilter,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithChildWithClip, state.Dependency);

            var jobUpdatePerceiveWithMemoryWithChild = new JobUpdatePerceiveWithMemoryWithChild
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleFilter = _handleFilter,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithChild, jobUpdatePerceiveWithMemoryWithChildWithClip);

            var jobUpdatePerceiveWithMemoryWithClip = new JobUpdatePerceiveWithMemoryWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleFilter = _handleFilter,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithClip, jobUpdatePerceiveWithMemoryWithChild);

            var jobUpdatePerceiveWithChildWithClip = new JobUpdatePerceiveWithChildWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleFilter = _handleFilter,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithChildWithClip, jobUpdatePerceiveWithMemoryWithClip);

            var jobUpdatePerceiveWithMemory = new JobUpdatePerceiveWithMemory
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleFilter = _handleFilter,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemory, jobUpdatePerceiveWithChildWithClip);

            var jobUpdatePerceiveWithChild = new JobUpdatePerceiveWithChild
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleFilter = _handleFilter,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithChild, jobUpdatePerceiveWithMemory);

            var jobUpdatePerceiveWithClip = new JobUpdatePerceiveWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleFilter = _handleFilter,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithClip, jobUpdatePerceiveWithChild);

            var jobUpdatePerceive = new JobUpdatePerceive
            {
                HandleEntity = _handleEntity,

                HandleBufferRayOffset = _handleBufferRayOffset,
                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleFilter = _handleFilter,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_query, jobUpdatePerceiveWithClip);

            state.Dependency = jobUpdatePerceive;
        }

        [BurstCompile]
        private static bool IsChild(in Entity entity, in DynamicBuffer<BufferSightChild> bufferChild)
        {
            foreach (var child in bufferChild)
            {
                if (child.Value == entity)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private static bool IsPerceived(in Entity entity, in DynamicBuffer<BufferSightPerceive> bufferPerceive, int length, out int index)
        {
            index = -1;

            for (var i = 0; i < length; i++)
            {
                if (bufferPerceive[i].Source == entity)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private static void RemoveFromMemory(in Entity entity, ref DynamicBuffer<BufferSightMemory> bufferMemory)
        {
            for (var i = 0; i < bufferMemory.Length; i++)
            {
                if (bufferMemory[i].Source == entity)
                {
                    bufferMemory.RemoveAtSwapBack(i);
                    return;
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChildWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var filters = chunk.GetNativeArray(ref HandleFilter);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var position = positions[i].Receiver;

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        if (CastRay(entities[i], in position, bufferChild, clip.RadiusSquared,
                                in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isSucceed = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, bufferChild, clip.RadiusSquared,
                                    in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                RemoveFromMemory(in cone.Source, ref bufferMemory);
                                isSucceed = true;
                                break;
                            }
                        }

                        if (!isSucceed && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = cone.Position, Source = cone.Source, Time = memory.Time });
                        }
                    }

                    for (var j = perceiveLength - 1; j >= 0; j--)
                    {
                        var perceive = bufferPerceive[j];
                        bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        bufferPerceive.RemoveAtSwapBack(j);
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, in DynamicBuffer<BufferSightChild> bufferChild, float clip,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var clipFraction = clip / math.distancesq(sourcePosition, position);
                var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver, bufferChild, clipFraction);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var sourceBufferChild)
                           && IsChild(collector.Hit.Entity, in sourceBufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChild : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var filters = chunk.GetNativeArray(ref HandleFilter);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];
                    var position = positions[i].Receiver;

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        if (CastRay(entities[i], in position, in bufferChild, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isSucceed = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, in bufferChild, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                RemoveFromMemory(in cone.Source, ref bufferMemory);
                                isSucceed = true;
                                break;
                            }
                        }

                        if (!isSucceed && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = cone.Position, Source = cone.Source, Time = memory.Time });
                        }
                    }

                    for (var j = perceiveLength - 1; j >= 0; j--)
                    {
                        var perceive = bufferPerceive[j];
                        bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        bufferPerceive.RemoveAtSwapBack(j);
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, in DynamicBuffer<BufferSightChild> bufferChild,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var collector = new CollectorClosestIgnoreEntityAndChild(receiver, bufferChild);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var sourceBufferChild)
                           && IsChild(collector.Hit.Entity, in sourceBufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var filters = chunk.GetNativeArray(ref HandleFilter);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];
                    var clip = clips[i];
                    var position = positions[i].Receiver;

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        if (CastRay(entities[i], in position, clip.RadiusSquared, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isSucceed = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, clip.RadiusSquared, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                RemoveFromMemory(in cone.Source, ref bufferMemory);
                                isSucceed = true;
                                break;
                            }
                        }

                        if (!isSucceed && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = cone.Position, Source = cone.Source, Time = memory.Time });
                        }
                    }

                    for (var j = perceiveLength - 1; j >= 0; j--)
                    {
                        var perceive = bufferPerceive[j];
                        bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        bufferPerceive.RemoveAtSwapBack(j);
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, float clip,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var clipFraction = clip / math.distancesq(sourcePosition, position);
                var collector = new CollectorClosestIgnoreEntityWithClip(receiver, clipFraction);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var bufferChild)
                           && IsChild(collector.Hit.Entity, in bufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChildWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var filters = chunk.GetNativeArray(ref HandleFilter);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];
                    var clip = clips[i];
                    var position = positions[i].Receiver;

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        if (CastRay(entities[i], in position, in bufferChild, clip.RadiusSquared,
                                in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, in bufferChild, clip.RadiusSquared,
                                    in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                break;
                            }
                        }
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, in DynamicBuffer<BufferSightChild> bufferChild, float clip,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var clipFraction = clip / math.distancesq(sourcePosition, position);
                var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver, bufferChild, clipFraction);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var sourceBufferChild)
                           && IsChild(collector.Hit.Entity, in sourceBufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemory : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var filters = chunk.GetNativeArray(ref HandleFilter);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];
                    var position = positions[i].Receiver;

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        if (CastRay(entities[i], in position, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
                        var isSucceed = false;

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                RemoveFromMemory(in cone.Source, ref bufferMemory);
                                isSucceed = true;
                                break;
                            }
                        }

                        if (!isSucceed && isPerceived)
                        {
                            bufferMemory.Add(new BufferSightMemory { Position = cone.Position, Source = cone.Source, Time = memory.Time });
                        }
                    }

                    for (var j = perceiveLength - 1; j >= 0; j--)
                    {
                        var perceive = bufferPerceive[j];
                        bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        bufferPerceive.RemoveAtSwapBack(j);
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var collector = new CollectorClosestIgnoreEntity(receiver);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var bufferChild)
                           && IsChild(collector.Hit.Entity, in bufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChild : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var filters = chunk.GetNativeArray(ref HandleFilter);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];
                    var position = positions[i].Receiver;

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        if (CastRay(entities[i], in position, in bufferChild, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, in bufferChild, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                break;
                            }
                        }
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, in DynamicBuffer<BufferSightChild> bufferChild,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var collector = new CollectorClosestIgnoreEntityAndChild(receiver, bufferChild);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var sourceBufferChild)
                           && IsChild(collector.Hit.Entity, in sourceBufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var filters = chunk.GetNativeArray(ref HandleFilter);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferCone = buffersCone[i];
                    var clip = clips[i];
                    var position = positions[i].Receiver;

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        if (CastRay(entities[i], in position, clip.RadiusSquared, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, clip.RadiusSquared, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                break;
                            }
                        }
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position, float clip,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var clipFraction = clip / math.distancesq(sourcePosition, position);
                var collector = new CollectorClosestIgnoreEntityWithClip(receiver, clipFraction);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var bufferChild)
                           && IsChild(collector.Hit.Entity, in bufferChild));
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceive : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [ReadOnly] public BufferTypeHandle<BufferSightRayOffset> HandleBufferRayOffset;
            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightFilter> HandleFilter;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersRayOffset = chunk.GetBufferAccessor(ref HandleBufferRayOffset);
                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var filters = chunk.GetNativeArray(ref HandleFilter);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferRayOffset = buffersRayOffset[i];
                    var bufferPerceive = buffersPerceive[i];
                    var bufferCone = buffersCone[i];
                    var position = positions[i].Receiver;

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        if (CastRay(entities[i], in position, in cone.Source, in cone.Position, filters[i].Value))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            continue;
                        }

                        var direction = math.normalizesafe(cone.Position - position);
                        var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

                        foreach (var rayOffset in bufferRayOffset)
                        {
                            var sourcePosition = cone.Position + math.rotate(lookRotation, rayOffset.Value);

                            if (CastRay(entities[i], in position, in cone.Source, in sourcePosition, filters[i].Value))
                            {
                                bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                                break;
                            }
                        }
                    }
                }
            }

            [BurstCompile]
            private bool CastRay(in Entity receiver, in float3 position,
                in Entity source, in float3 sourcePosition, in CollisionFilter filter)
            {
                var collector = new CollectorClosestIgnoreEntity(receiver);
                var raycast = new RaycastInput { Start = position, End = sourcePosition, Filter = filter };

                CollisionWorld.CastRay(raycast, ref collector);

                return collector.Hit.Entity == source
                       || (LookupBufferChild.TryGetBuffer(source, out var bufferChild)
                           && IsChild(collector.Hit.Entity, in bufferChild));
            }
        }
    }
}