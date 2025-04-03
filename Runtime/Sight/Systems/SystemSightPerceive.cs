using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightCone))]
    public partial struct SystemSightPerceive : ISystem
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

            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();
            var buffersChild = SystemAPI.GetBufferLookup<BufferSightChild>();
            var buffersCone = SystemAPI.GetBufferLookup<BufferSightCone>();
            ref readonly var physicsWorld = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value, buffersCone[receiver],
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position, buffersCone[receiver],
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersCone[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersCone[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightPerceive>()
                         .WithNone<BufferSightChild, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, positionRO.ValueRO.Value,
                    buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightPerceive>()
                         .WithNone<BufferSightChild, BufferSightCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, transformRO.ValueRO.Position,
                    buffersChild, in physicsWorld, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightCone> bufferCone,
            DynamicBuffer<BufferSightChild> receiverBufferChild, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var cone in bufferCone)
            {
                if (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        cone.Source, cone.Position, sourceBufferChild, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    cone.Source, cone.Position, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightCone> bufferCone,
            BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var cone in bufferCone)
            {
                if (buffersChild.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, receiver, position,
                        cone.Source, cone.Position, sourceBufferChild, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    cone.Source, cone.Position, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightChild> receiverBufferChild,
            BufferLookup<BufferSightChild> buffersChild,
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
                        source, positionRO.ValueRO.Value, sourceBufferChild,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, positionRO.ValueRO.Value, in physicsWorld, ref commands);
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
                        source, transformRO.ValueRO.Position, sourceBufferChild,
                        in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, transformRO.ValueRO.Position, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, BufferLookup<BufferSightChild> buffersChild,
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
                        source, sourcePosition, sourceBufferChild, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    source, sourcePosition, in physicsWorld, ref commands);
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
                        source, sourcePosition, sourceBufferChild, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position,
                    source, sourcePosition, in physicsWorld, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightChild> receiverBufferChild,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            var raycast = new RaycastInput
            {
                Start = position,
                End = sourcePosition,
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
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            var raycast = new RaycastInput
            {
                Start = position,
                End = sourcePosition,
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
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightChild> receiverBufferChild,
            Entity source, float3 sourcePosition,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            var raycast = new RaycastInput
            {
                Start = position,
                End = sourcePosition,
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
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, float3 position, Entity source, float3 sourcePosition,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            var raycast = new RaycastInput
            {
                Start = position,
                End = sourcePosition,
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