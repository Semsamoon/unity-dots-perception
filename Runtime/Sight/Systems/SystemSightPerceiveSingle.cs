using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightCone))]
    public partial struct SystemSightPerceiveSingle : ISystem
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
                SystemAPI.GetBufferLookup<BufferSightPerceive>(),
                SystemAPI.GetBufferLookup<BufferSightChild>(),
                SystemAPI.GetBufferLookup<BufferSightCone>());

            foreach (var (positionRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, clipRO, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, clipRO, physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightClip>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightClip, BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, physicsRW, in buffers, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, DynamicBuffer<BufferSightChild> receiverBufferChild,
            RefRW<PhysicsWorldSingleton> physicsRW, in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Perceive[receiver.Entity].Clear();

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, in receiver, clipRO, receiverBufferChild,
                        in source, sourceBufferChild, in raycast, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, clipRO, receiverBufferChild, in source, in raycast, physicsRW, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO,
            RefRW<PhysicsWorldSingleton> physicsRW, in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Perceive[receiver.Entity].Clear();

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var bufferChild))
                {
                    ProcessSource(ref state, in receiver, clipRO, in source, bufferChild, in raycast, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, clipRO, in source, in raycast, physicsRW, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> receiverBufferChild, RefRW<PhysicsWorldSingleton> physicsRW,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Perceive[receiver.Entity].Clear();

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    ProcessSource(ref state, in receiver, receiverBufferChild, in source, sourceBufferChild, in raycast, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, receiverBufferChild, in source, in raycast, physicsRW, ref commands);
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
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var bufferChild))
                {
                    ProcessSource(ref state, in receiver, in source, bufferChild, in raycast, physicsRW, ref commands);
                    continue;
                }

                ProcessSource(ref state, in receiver, in source, in raycast, physicsRW, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, DynamicBuffer<BufferSightChild> receiverBufferChild,
            in Source source, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver.Entity, receiverBufferChild, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, in Source source, DynamicBuffer<BufferSightChild> bufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityWithClip(receiver.Entity, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, DynamicBuffer<BufferSightChild> bufferChild, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver.Entity, bufferChild, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityWithClip(receiver.Entity, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> receiverBufferChild,
            in Source source, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, receiverBufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source, DynamicBuffer<BufferSightChild> bufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> bufferChild, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, bufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity == source.Entity)
            {
                AppendPerceive(in receiver, in source, ref commands);
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
            public readonly BufferLookup<BufferSightPerceive> Perceive;
            public readonly BufferLookup<BufferSightChild> Child;
            public readonly BufferLookup<BufferSightCone> Cone;

            public Buffers(
                BufferLookup<BufferSightPerceive> perceive,
                BufferLookup<BufferSightChild> child,
                BufferLookup<BufferSightCone> cone)
            {
                Perceive = perceive;
                Child = child;
                Cone = cone;
            }
        }

        private readonly struct Receiver
        {
            public readonly float3 Position;
            public readonly Entity Entity;

            public Receiver(Entity entity, float3 position)
            {
                Entity = entity;
                Position = position;
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

            public float Clip(RefRO<ComponentSightClip> clipRO)
            {
                return math.sqrt(clipRO.ValueRO.RadiusSquared / math.lengthsq(Input.End - Input.Start));
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
            if (hit.Entity == entity || hit.Fraction >= clip)
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