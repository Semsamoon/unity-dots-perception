using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightCone))]
    public partial struct SystemSightPerceiveOffset : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in SystemAPI
                         .QueryBuilder()
                         .WithAll<TagSightReceiver>()
                         .WithNone<BufferSightPerceive>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightPerceive>(receiver);
            }

            commands.Playback(state.EntityManager);
            commands = new EntityCommandBuffer(Allocator.Temp);

            var buffersRayOffset = SystemAPI.GetBufferLookup<BufferSightRayOffset>();
            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();
            var buffersChild = SystemAPI.GetBufferLookup<BufferSightChild>();
            var buffersCone = SystemAPI.GetBufferLookup<BufferSightCone>();
            ref readonly var physicsWorld = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightChild, BufferSightPerceive, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value, buffersCone[receiver],
                    buffersRayOffset[receiver], buffersChild[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightChild, BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position, buffersCone[receiver],
                    buffersRayOffset[receiver], buffersChild[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersRayOffset[receiver], buffersChild[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersRayOffset[receiver], buffersChild[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersCone[receiver], buffersRayOffset[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersCone[receiver], buffersRayOffset[receiver], buffersChild,
                    in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithNone<BufferSightPerceive, BufferSightChild, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersRayOffset[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple>()
                         .WithAll<BufferSightRayOffset, BufferSightPerceive>()
                         .WithNone<BufferSightChild, BufferSightCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersRayOffset[receiver], buffersChild, in physicsWorld, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position,
            DynamicBuffer<BufferSightCone> bufferCone, DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            DynamicBuffer<BufferSightChild> receiverBufferChild, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var cone in bufferCone)
            {
                if (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        cone.Source, cone.Position, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    cone.Source, cone.Position, bufferRayOffset, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightCone> bufferCone,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var cone in bufferCone)
            {
                if (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position,
                        cone.Source, cone.Position, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    cone.Source, cone.Position, bufferRayOffset, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            DynamicBuffer<BufferSightChild> receiverBufferChild, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var (positionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                if (buffersChild.TryGetBuffer(source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        source, positionRO.ValueRO.Value, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, positionRO.ValueRO.Value, bufferRayOffset,
                    in physicsWorld, ref commands);
            }

            foreach (var (transformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                if (buffersChild.TryGetBuffer(source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        source, transformRO.ValueRO.Position, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, transformRO.ValueRO.Position, bufferRayOffset,
                    in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var (positionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourcePosition = positionRO.ValueRO.Value;

                if (buffersChild.TryGetBuffer(source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position,
                        source, sourcePosition, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    source, sourcePosition, bufferRayOffset, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var sourcePosition = transformRO.ValueRO.Position;

                if (buffersChild.TryGetBuffer(source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position,
                        source, sourcePosition, sourceBufferChild, bufferRayOffset,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    source, sourcePosition, bufferRayOffset, in physicsWorld, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightChild> receiverBufferChild,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightChild> sourceBufferChild,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var rayOffset in bufferRayOffset)
            {
                var raycast = new RaycastInput
                {
                    Start = position,
                    End = sourcePosition + rayOffset.Value,
                    Filter = CollisionFilter.Default,
                };

                var hitCollector = new EntitiesFilterCollector<RaycastHit>(0, 1, receiver, receiverBufferChild);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (IsHit(hitCollector.ClosestHit.Entity, source, sourceBufferChild))
                {
                    commands.AppendToBuffer(receiver, new BufferSightPerceive
                    {
                        Position = sourcePosition,
                        Source = source,
                    });
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightChild> sourceBufferChild,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var rayOffset in bufferRayOffset)
            {
                var raycast = new RaycastInput
                {
                    Start = position,
                    End = sourcePosition + rayOffset.Value,
                    Filter = CollisionFilter.Default,
                };

                var hitCollector = new EntityFilterCollector<RaycastHit>(0, 1, receiver);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (IsHit(hitCollector.ClosestHit.Entity, source, sourceBufferChild))
                {
                    commands.AppendToBuffer(receiver, new BufferSightPerceive
                    {
                        Position = sourcePosition,
                        Source = source,
                    });
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightChild> receiverBufferChild,
            Entity source, float3 sourcePosition,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var rayOffset in bufferRayOffset)
            {
                var raycast = new RaycastInput
                {
                    Start = position,
                    End = sourcePosition + rayOffset.Value,
                    Filter = CollisionFilter.Default,
                };

                var hitCollector = new EntitiesFilterCollector<RaycastHit>(0, 1, receiver, receiverBufferChild);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (hitCollector.ClosestHit.Entity == source)
                {
                    commands.AppendToBuffer(receiver, new BufferSightPerceive
                    {
                        Position = sourcePosition,
                        Source = source,
                    });
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, Entity source, float3 sourcePosition,
            DynamicBuffer<BufferSightRayOffset> bufferRayOffset,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var rayOffset in bufferRayOffset)
            {
                var raycast = new RaycastInput
                {
                    Start = position,
                    End = sourcePosition + rayOffset.Value,
                    Filter = CollisionFilter.Default,
                };

                var hitCollector = new EntityFilterCollector<RaycastHit>(0, 1, receiver);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (hitCollector.ClosestHit.Entity == source)
                {
                    commands.AppendToBuffer(receiver, new BufferSightPerceive
                    {
                        Position = sourcePosition,
                        Source = source,
                    });
                    return;
                }
            }
        }

        private bool IsHit(Entity hit, Entity entity, DynamicBuffer<BufferSightChild> bufferChild)
        {
            if (hit == entity)
            {
                return true;
            }

            foreach (var child in bufferChild)
            {
                if (hit == child.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public struct EntityFilterCollector<T> : ICollector<T> where T : struct, IQueryResult
        {
            public bool EarlyOutOnFirstHit => false;

            public float MinFraction { get; }
            public float MaxFraction { get; private set; }

            public Entity ExcludedEntity { get; }

            public T ClosestHit { get; private set; }
            public int NumHits => ClosestHit.Entity == Entity.Null ? 0 : 1;

            public EntityFilterCollector(float minFraction, float maxFraction, Entity excludedEntity)
            {
                MinFraction = minFraction;
                MaxFraction = maxFraction;
                ExcludedEntity = excludedEntity;
                ClosestHit = default;
            }

            public bool AddHit(T hit)
            {
                if (hit.Entity == ExcludedEntity || hit.Fraction < MinFraction)
                {
                    return false;
                }

                MaxFraction = hit.Fraction;
                ClosestHit = hit;
                return true;
            }
        }

        public struct EntitiesFilterCollector<T> : ICollector<T> where T : struct, IQueryResult
        {
            public bool EarlyOutOnFirstHit => false;

            public float MinFraction { get; }
            public float MaxFraction { get; private set; }

            public Entity ExcludedEntity { get; }
            public DynamicBuffer<BufferSightChild> ExcludedChildren { get; }

            public T ClosestHit { get; private set; }
            public int NumHits => ClosestHit.Entity == Entity.Null ? 0 : 1;

            public EntitiesFilterCollector(
                float minFraction, float maxFraction, Entity excludedEntity,
                DynamicBuffer<BufferSightChild> excludedChildren)
            {
                MinFraction = minFraction;
                MaxFraction = maxFraction;
                ExcludedEntity = excludedEntity;
                ExcludedChildren = excludedChildren;
                ClosestHit = default;
            }

            public bool AddHit(T hit)
            {
                if (hit.Entity == ExcludedEntity || hit.Fraction < MinFraction)
                {
                    return false;
                }

                foreach (var child in ExcludedChildren)
                {
                    if (hit.Entity == child.Value)
                    {
                        return false;
                    }
                }

                MaxFraction = hit.Fraction;
                ClosestHit = hit;
                return true;
            }
        }
    }
}