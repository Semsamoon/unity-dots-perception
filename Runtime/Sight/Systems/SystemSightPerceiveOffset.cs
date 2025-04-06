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

            var physicsRW = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>();
            var buffers = new Buffers(
                SystemAPI.GetBufferLookup<BufferSightRayOffset>(),
                SystemAPI.GetBufferLookup<BufferSightPerceive>(),
                SystemAPI.GetBufferLookup<BufferSightChild>(),
                SystemAPI.GetBufferLookup<BufferSightCone>());

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightChild, BufferSightPerceive, BufferSightCone>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, in buffers);
                ProcessReceiver(ref state, in receiverData, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRayMultiple, BufferSightRayOffset>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, in buffers);
                ProcessReceiver(ref state, in receiverData, physicsRW, in buffers, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> receiverBufferChild, RefRW<PhysicsWorldSingleton> physicsRW,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Perceive[receiver.Entity].Clear();

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, in receiver, receiverBufferChild, in source, sourceBufferChild, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, receiverBufferChild, in source, physicsRW, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRW<PhysicsWorldSingleton> physicsRW,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Perceive[receiver.Entity].Clear();

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, in receiver, in source, sourceBufferChild, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, in source, physicsRW, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> receiverBufferChild,
            in Source source, DynamicBuffer<BufferSightChild> sourceBufferChild,
            RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var rayCast = new RayCast(in receiver, in source);
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, receiverBufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
                return;
            }

            var direction = math.normalizesafe(source.Position - receiver.Position);
            var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

            foreach (var rayOffset in receiver.RayOffset)
            {
                rayCast = new RayCast(in receiver, in source, in lookRotation, rayOffset.Value);
                collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, receiverBufferChild);
                physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

                if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
                {
                    AppendPerceive(in receiver, in source, ref commands);
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source, DynamicBuffer<BufferSightChild> bufferChild,
            RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var rayCast = new RayCast(in receiver, in source);
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
                return;
            }

            var direction = math.normalizesafe(source.Position - receiver.Position);
            var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

            foreach (var rayOffset in receiver.RayOffset)
            {
                rayCast = new RayCast(in receiver, in source, in lookRotation, rayOffset.Value);
                collector = new CollectorClosestIgnoreEntity(receiver.Entity);
                physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

                if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
                {
                    AppendPerceive(in receiver, in source, ref commands);
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> bufferChild, in Source source,
            RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var rayCast = new RayCast(in receiver, in source);
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, bufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
                return;
            }

            var direction = math.normalizesafe(source.Position - receiver.Position);
            var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

            foreach (var rayOffset in receiver.RayOffset)
            {
                rayCast = new RayCast(in receiver, in source, in lookRotation, rayOffset.Value);
                collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, bufferChild);
                physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

                if (collector.Hit.Entity == source.Entity)
                {
                    AppendPerceive(in receiver, in source, ref commands);
                    return;
                }
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source,
            RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var rayCast = new RayCast(in receiver, in source);
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
                return;
            }

            var direction = math.normalizesafe(source.Position - receiver.Position);
            var lookRotation = quaternion.LookRotation(direction, new float3(0, 1, 0));

            foreach (var rayOffset in receiver.RayOffset)
            {
                rayCast = new RayCast(in receiver, in source, in lookRotation, rayOffset.Value);
                collector = new CollectorClosestIgnoreEntity(receiver.Entity);
                physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

                if (collector.Hit.Entity == source.Entity)
                {
                    AppendPerceive(in receiver, in source, ref commands);
                    return;
                }
            }
        }

        private void AppendPerceive(in Receiver receiver, in Source source, ref EntityCommandBuffer commands)
        {
            commands.AppendToBuffer(receiver.Entity, new BufferSightPerceive
            {
                Source = source.Entity,
                Position = source.Position,
            });
        }

        private readonly struct Buffers
        {
            public readonly BufferLookup<BufferSightRayOffset> RayOffset;
            public readonly BufferLookup<BufferSightPerceive> Perceive;
            public readonly BufferLookup<BufferSightChild> Child;
            public readonly BufferLookup<BufferSightCone> Cone;

            public Buffers(
                BufferLookup<BufferSightRayOffset> rayOffset,
                BufferLookup<BufferSightPerceive> perceive,
                BufferLookup<BufferSightChild> child,
                BufferLookup<BufferSightCone> cone)
            {
                RayOffset = rayOffset;
                Perceive = perceive;
                Child = child;
                Cone = cone;
            }
        }

        private readonly struct Receiver
        {
            public readonly float3 Position;
            public readonly Entity Entity;
            public readonly DynamicBuffer<BufferSightRayOffset> RayOffset;

            public Receiver(Entity entity, float3 position, in Buffers buffers)
            {
                Entity = entity;
                Position = position;
                RayOffset = buffers.RayOffset[entity];
            }
        }

        private readonly struct Source
        {
            public readonly float3 Position;
            public readonly Entity Entity;

            public Source(Entity entity, float3 position)
            {
                Entity = entity;
                Position = position;
            }
        }

        private readonly struct RayCast
        {
            public readonly RaycastInput Input;

            public RayCast(in Receiver receiver, in Source source)
            {
                Input = new RaycastInput
                {
                    Start = receiver.Position,
                    End = source.Position,
                    Filter = CollisionFilter.Default,
                };
            }

            public RayCast(in Receiver receiver, in Source source, in quaternion lookRotation, float3 offset)
            {
                Input = new RaycastInput
                {
                    Start = receiver.Position,
                    End = source.Position + math.rotate(lookRotation, offset),
                    Filter = CollisionFilter.Default,
                };
            }
        }
    }
}