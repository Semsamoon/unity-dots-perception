using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [BurstCompile, UpdateAfter(typeof(SystemSightCone)), UpdateAfter(typeof(SystemSightMemory))]
    public partial struct SystemSightPerceiveSingle : ISystem
    {
        private EntityQuery _queryWithMemoryWithChildWithClip;
        private EntityQuery _queryWithMemoryWithChild;
        private EntityQuery _queryWithMemoryWithClip;
        private EntityQuery _queryWithChildWithClip;
        private EntityQuery _queryWithMemory;
        private EntityQuery _queryWithChild;
        private EntityQuery _queryWithClip;
        private EntityQuery _query;

        private BufferTypeHandle<BufferSightPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferSightMemory> _handleBufferMemory;
        private BufferTypeHandle<BufferSightChild> _handleBufferChild;
        private BufferTypeHandle<BufferSightCone> _handleBufferCone;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightMemory> _handleMemory;
        private ComponentTypeHandle<ComponentSightClip> _handleClip;

        private BufferLookup<BufferSightChild> _lookupBufferChild;
        private EntityTypeHandle _handleEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _queryWithMemoryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory>()
                .WithAll<BufferSightChild, ComponentSightClip>()
                .Build();
            _queryWithMemoryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, BufferSightChild, ComponentSightMemory>()
                .WithNone<ComponentSightClip>()
                .Build();
            _queryWithMemoryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory, ComponentSightClip>()
                .WithNone<BufferSightChild>()
                .Build();
            _queryWithChildWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightChild, ComponentSightClip>()
                .WithNone<BufferSightMemory, ComponentSightMemory>()
                .Build();
            _queryWithMemory = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightMemory, ComponentSightMemory>()
                .WithNone<BufferSightChild, ComponentSightClip>()
                .Build();
            _queryWithChild = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<BufferSightChild>()
                .WithNone<BufferSightMemory, ComponentSightMemory, ComponentSightClip>()
                .Build();
            _queryWithClip = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithAll<ComponentSightClip>()
                .WithNone<BufferSightMemory, BufferSightChild, ComponentSightMemory>()
                .Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, TagSightRaySingle, ComponentSightPosition>()
                .WithAll<BufferSightPerceive, BufferSightCone>()
                .WithNone<BufferSightMemory, ComponentSightMemory>()
                .WithNone<BufferSightChild, ComponentSightClip>()
                .Build();

            _handleBufferPerceive = SystemAPI.GetBufferTypeHandle<BufferSightPerceive>();
            _handleBufferMemory = SystemAPI.GetBufferTypeHandle<BufferSightMemory>();
            _handleBufferChild = SystemAPI.GetBufferTypeHandle<BufferSightChild>(isReadOnly: true);
            _handleBufferCone = SystemAPI.GetBufferTypeHandle<BufferSightCone>(isReadOnly: true);

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>(isReadOnly: true);
            _handleMemory = SystemAPI.GetComponentTypeHandle<ComponentSightMemory>(isReadOnly: true);
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
            _handleBufferPerceive.Update(ref state);
            _handleBufferMemory.Update(ref state);
            _handleBufferChild.Update(ref state);
            _handleBufferCone.Update(ref state);

            _handlePosition.Update(ref state);
            _handleMemory.Update(ref state);
            _handleClip.Update(ref state);

            _lookupBufferChild.Update(ref state);
            _handleEntity.Update(ref state);

            ref readonly var physics = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            var jobUpdatePerceiveWithMemoryWithChildWithClip = new JobUpdatePerceiveWithMemoryWithChildWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithChildWithClip, state.Dependency);

            var jobUpdatePerceiveWithMemoryWithChild = new JobUpdatePerceiveWithMemoryWithChild
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithChild, jobUpdatePerceiveWithMemoryWithChildWithClip);

            var jobUpdatePerceiveWithMemoryWithClip = new JobUpdatePerceiveWithMemoryWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemoryWithClip, jobUpdatePerceiveWithMemoryWithChild);

            var jobUpdatePerceiveWithChildWithClip = new JobUpdatePerceiveWithChildWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithChildWithClip, jobUpdatePerceiveWithMemoryWithClip);

            var jobUpdatePerceiveWithMemory = new JobUpdatePerceiveWithMemory
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferMemory = _handleBufferMemory,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleMemory = _handleMemory,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithMemory, jobUpdatePerceiveWithChildWithClip);

            var jobUpdatePerceiveWithChild = new JobUpdatePerceiveWithChild
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferChild = _handleBufferChild,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithChild, jobUpdatePerceiveWithMemory);

            var jobUpdatePerceiveWithClip = new JobUpdatePerceiveWithClip
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,
                HandleClip = _handleClip,

                LookupBufferChild = _lookupBufferChild,
                CollisionWorld = physics.CollisionWorld,
            }.ScheduleParallel(_queryWithClip, jobUpdatePerceiveWithChild);

            var jobUpdatePerceive = new JobUpdatePerceive
            {
                HandleEntity = _handleEntity,

                HandleBufferPerceive = _handleBufferPerceive,
                HandleBufferCone = _handleBufferCone,

                HandlePosition = _handlePosition,

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

            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];
                    var clip = clips[i];

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var clipFraction = clip.RadiusSquared / math.distancesq(cone.Position, positions[i].Receiver);
                        var collector = new CollectorClosestIgnoreEntityAndChildWithClip(entities[i], bufferChild, clipFraction);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var sourceBufferChild)
                                && IsChild(collector.Hit.Entity, in sourceBufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        if (isPerceived)
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
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithChild : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var bufferChild = buffersChild[i];
                    var memory = memories[i];

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var collector = new CollectorClosestIgnoreEntityAndChild(entities[i], bufferChild);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var sourceBufferChild)
                                && IsChild(collector.Hit.Entity, in sourceBufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        if (isPerceived)
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
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemoryWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];
                    var clip = clips[i];

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var clipFraction = clip.RadiusSquared / math.distancesq(cone.Position, positions[i].Receiver);
                        var collector = new CollectorClosestIgnoreEntityWithClip(entities[i], clipFraction);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var bufferChild)
                                && IsChild(collector.Hit.Entity, in bufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        if (isPerceived)
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
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChildWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];
                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        var clipFraction = clip.RadiusSquared / math.distancesq(cone.Position, positions[i].Receiver);
                        var collector = new CollectorClosestIgnoreEntityAndChildWithClip(entities[i], bufferChild, clipFraction);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var sourceBufferChild)
                                && IsChild(collector.Hit.Entity, in sourceBufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemory : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightMemory> HandleMemory;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferMemory = buffersMemory[i];
                    var bufferCone = buffersCone[i];
                    var memory = memories[i];

                    var perceiveLength = bufferPerceive.Length;

                    foreach (var cone in bufferCone)
                    {
                        var collector = new CollectorClosestIgnoreEntity(entities[i]);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };
                        var isPerceived = IsPerceived(in cone.Source, in bufferPerceive, perceiveLength, out var index);

                        if (isPerceived)
                        {
                            perceiveLength--;
                            bufferPerceive[index] = bufferPerceive[perceiveLength];
                            bufferPerceive.RemoveAtSwapBack(perceiveLength);
                        }

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var bufferChild)
                                && IsChild(collector.Hit.Entity, in bufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                            RemoveFromMemory(in cone.Source, ref bufferMemory);
                            continue;
                        }

                        if (isPerceived)
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
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithChild : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightChild> HandleBufferChild;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersChild = chunk.GetBufferAccessor(ref HandleBufferChild);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferChild = buffersChild[i];
                    var bufferCone = buffersCone[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        var collector = new CollectorClosestIgnoreEntityAndChild(entities[i], bufferChild);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var sourceBufferChild)
                                && IsChild(collector.Hit.Entity, in sourceBufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithClip : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightClip> HandleClip;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);
                var clips = chunk.GetNativeArray(ref HandleClip);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferCone = buffersCone[i];
                    var clip = clips[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        var clipFraction = clip.RadiusSquared / math.distancesq(cone.Position, positions[i].Receiver);
                        var collector = new CollectorClosestIgnoreEntityWithClip(entities[i], clipFraction);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var bufferChild)
                                && IsChild(collector.Hit.Entity, in bufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceive : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [WriteOnly] public BufferTypeHandle<BufferSightPerceive> HandleBufferPerceive;
            [ReadOnly] public BufferTypeHandle<BufferSightCone> HandleBufferCone;

            [ReadOnly] public ComponentTypeHandle<ComponentSightPosition> HandlePosition;

            [ReadOnly] public BufferLookup<BufferSightChild> LookupBufferChild;
            [ReadOnly] public CollisionWorld CollisionWorld;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(HandleEntity);

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                var buffersCone = chunk.GetBufferAccessor(ref HandleBufferCone);

                var positions = chunk.GetNativeArray(ref HandlePosition);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    var bufferCone = buffersCone[i];

                    bufferPerceive.Clear();

                    foreach (var cone in bufferCone)
                    {
                        var collector = new CollectorClosestIgnoreEntity(entities[i]);
                        var raycast = new RaycastInput { Start = positions[i].Receiver, End = cone.Position, Filter = CollisionFilter.Default };

                        CollisionWorld.CastRay(raycast, ref collector);

                        if (collector.Hit.Entity == cone.Source
                            || (LookupBufferChild.TryGetBuffer(cone.Source, out var bufferChild)
                                && IsChild(collector.Hit.Entity, in bufferChild)))
                        {
                            bufferPerceive.Add(new BufferSightPerceive { Position = cone.Position, Source = cone.Source });
                        }
                    }
                }
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
            if (CheckHit(hit, _entity, _bufferChild, _clip))
            {
                MaxFraction = hit.Fraction;
                Hit = hit;
                return true;
            }

            return false;
        }

        public static bool CheckHit(RaycastHit hit, Entity entity, DynamicBuffer<BufferSightChild> bufferChild, float clip)
        {
            if (hit.Entity == entity || hit.Fraction < clip)
            {
                return false;
            }

            foreach (var child in bufferChild)
            {
                if (hit.Entity == child.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public struct CollectorClosestIgnoreEntityAndChild : ICollector<RaycastHit>
    {
        private readonly Entity _entity;
        private readonly DynamicBuffer<BufferSightChild> _bufferChild;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits => Hit.Entity == Entity.Null ? 0 : 1;
        public RaycastHit Hit { get; private set; }

        public CollectorClosestIgnoreEntityAndChild(Entity entity, DynamicBuffer<BufferSightChild> bufferChild)
        {
            _entity = entity;
            _bufferChild = bufferChild;
            MaxFraction = 1;
            Hit = default;
        }

        public bool AddHit(RaycastHit hit)
        {
            if (CheckHit(hit, _entity, _bufferChild))
            {
                MaxFraction = hit.Fraction;
                Hit = hit;
                return true;
            }

            return false;
        }

        public static bool CheckHit(RaycastHit hit, Entity entity, DynamicBuffer<BufferSightChild> bufferChild)
        {
            if (hit.Entity == entity)
            {
                return false;
            }

            foreach (var child in bufferChild)
            {
                if (hit.Entity == child.Value)
                {
                    return false;
                }
            }

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
            if (hit.Entity != _entity && hit.Fraction >= _clip)
            {
                MaxFraction = hit.Fraction;
                Hit = hit;
                return true;
            }

            return false;
        }
    }

    public struct CollectorClosestIgnoreEntity : ICollector<RaycastHit>
    {
        private readonly Entity _entity;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits => Hit.Entity == Entity.Null ? 0 : 1;
        public RaycastHit Hit { get; private set; }

        public CollectorClosestIgnoreEntity(Entity entity)
        {
            _entity = entity;
            MaxFraction = 1;
            Hit = default;
        }

        public bool AddHit(RaycastHit hit)
        {
            if (hit.Entity != _entity)
            {
                MaxFraction = hit.Fraction;
                Hit = hit;
                return true;
            }

            return false;
        }
    }
}