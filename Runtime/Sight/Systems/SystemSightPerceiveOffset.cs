using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

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

                var hitCollector = new CollectorClosestIgnoreEntityAndChild(receiver, receiverBufferChild);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (!CollectorClosestIgnoreEntityAndChild.CheckHit(hitCollector.Hit, source, sourceBufferChild))
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

                var hitCollector = new CollectorClosestIgnoreEntity(receiver);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (!CollectorClosestIgnoreEntityAndChild.CheckHit(hitCollector.Hit, source, sourceBufferChild))
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

                var hitCollector = new CollectorClosestIgnoreEntityAndChild(receiver, receiverBufferChild);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (hitCollector.Hit.Entity == source)
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

                var hitCollector = new CollectorClosestIgnoreEntity(receiver);
                physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

                if (hitCollector.Hit.Entity == source)
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
    }
}