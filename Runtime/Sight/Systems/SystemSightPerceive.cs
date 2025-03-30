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
            ref readonly var physicsWorld = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRO;

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, bufferInsideCone, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive, BufferSightInsideCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var bufferInsideCone = buffersInsideCone[receiver];
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, bufferInsideCone, in physicsWorld, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive>()
                         .WithNone<BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                var receiverPosition = positionRO.ValueRO.Value;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightReceiver, BufferSightPerceive>()
                         .WithNone<BufferSightInsideCone, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var receiverPosition = transformRO.ValueRO.Position;
                buffersPerceive[receiver].Clear();
                ProcessReceiver(ref state, receiver, receiverPosition, in physicsWorld, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position,
            DynamicBuffer<BufferSightInsideCone> bufferInsideCone,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var insideCone in bufferInsideCone)
            {
                var source = insideCone.Source;
                var sourcePosition = insideCone.Position;
                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, float3 position,
            in PhysicsWorldSingleton physicsWorld, ref EntityCommandBuffer commands)
        {
            foreach (var (positionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourcePosition = positionRO.ValueRO.Value;
                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
            }

            foreach (var (transformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var sourcePosition = transformRO.ValueRO.Position;
                ProcessSource(ref state, receiver, position, source, sourcePosition, in physicsWorld, ref commands);
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
    }
}