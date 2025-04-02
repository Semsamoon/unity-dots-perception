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

            var buffersInsideCone = SystemAPI.GetBufferLookup<BufferSightInsideCone>();
            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();
            var buffersChild = SystemAPI.GetBufferLookup<BufferSightChild>();
            ref readonly var physicsWorld = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, bufferInsideCone,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightInsideCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, bufferInsideCone,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightChild, BufferSightPerceive>()
                         .WithNone<BufferSightInsideCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    buffersChild[receiver], buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive, BufferSightInsideCone>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    bufferInsideCone, buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive, BufferSightInsideCone>()
                         .WithNone<BufferSightChild, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    bufferInsideCone, buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive>()
                         .WithNone<BufferSightChild, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    buffersChild, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive>()
                         .WithNone<BufferSightChild, BufferSightInsideCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition,
                    buffersChild, in physicsWorld, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightInsideCone> bufferInsideCone,
            DynamicBuffer<BufferSightChild> receiverBufferChild, BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var insideCone in bufferInsideCone)
            {
                var source = insideCone.Source;
                var sourcePosition = insideCone.Position;

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, sourcePosition, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position, DynamicBuffer<BufferSightInsideCone> bufferInsideCone,
            BufferLookup<BufferSightChild> buffersChild,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var insideCone in bufferInsideCone)
            {
                var source = insideCone.Source;
                var sourcePosition = insideCone.Position;

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
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
                var sourcePosition = positionRO.ValueRO.Value;

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, sourcePosition, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var sourcePosition = transformRO.ValueRO.Position;

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, receiverBufferChild,
                        source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, receiverBufferChild,
                    source, sourcePosition, in physicsWorld, ref commands);
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

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var sourcePosition = transformRO.ValueRO.Position;

                if (buffersChild.TryGetBuffer(source, out var buffer))
                {
                    ProcessSource(ref state, receiver, position, source, sourcePosition, buffer, in physicsWorld, ref commands);
                    continue;
                }

                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
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