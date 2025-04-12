using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightCone)), UpdateAfter(typeof(SystemSightMemory))]
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

            var physicsRW = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>();
            var buffers = new Buffers(
                SystemAPI.GetBufferLookup<BufferSightPerceive>(),
                SystemAPI.GetBufferLookup<BufferSightMemory>(),
                SystemAPI.GetBufferLookup<BufferSightChild>(),
                SystemAPI.GetBufferLookup<BufferSightCone>());

            foreach (var (positionRO, memoryRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightMemory>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone, BufferSightMemory>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, memoryRO, clipRO, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, memoryRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightMemory>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone, BufferSightMemory>()
                         .WithNone<BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, memoryRO, clipRO, physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, memoryRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightMemory>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone, BufferSightMemory>()
                         .WithNone<ComponentSightClip>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, memoryRO, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, memoryRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightMemory>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone, BufferSightMemory>()
                         .WithNone<ComponentSightClip, BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, memoryRO, physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightMemory, BufferSightMemory>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, clipRO, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightMemory, BufferSightMemory, BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, clipRO, physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle, BufferSightChild>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightMemory, BufferSightMemory, ComponentSightClip>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, buffers.Child[receiver], physicsRW, in buffers, ref commands);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightReceiver, TagSightRaySingle>()
                         .WithAll<BufferSightPerceive, BufferSightCone>()
                         .WithNone<ComponentSightMemory, BufferSightMemory>()
                         .WithNone<ComponentSightClip, BufferSightChild>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Receiver);
                ProcessReceiver(ref state, in receiverData, physicsRW, in buffers, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightMemory> memoryRO, RefRO<ComponentSightClip> clipRO,
            DynamicBuffer<BufferSightChild> receiverBufferChild, RefRW<PhysicsWorldSingleton> physicsRW,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            var bufferPerceive = buffers.Perceive[receiver.Entity];
            var bufferMemory = buffers.Memory[receiver.Entity];

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    if (ProcessSource(ref state, in receiver, clipRO, receiverBufferChild,
                            in source, sourceBufferChild, in raycast, physicsRW, ref commands))
                    {
                        RemoveMemory(in source, bufferMemory, bufferPerceive);
                        continue;
                    }

                    AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
                    continue;
                }

                if (ProcessSource(ref state, in receiver, clipRO, receiverBufferChild, in source, in raycast, physicsRW, ref commands))
                {
                    RemoveMemory(in source, bufferMemory, bufferPerceive);
                    continue;
                }

                AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
            }

            AppendMemory(in receiver, memoryRO, bufferPerceive, ref commands);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightMemory> memoryRO, RefRO<ComponentSightClip> clipRO,
            RefRW<PhysicsWorldSingleton> physicsRW, in Buffers buffers, ref EntityCommandBuffer commands)
        {
            var bufferPerceive = buffers.Perceive[receiver.Entity];
            var bufferMemory = buffers.Memory[receiver.Entity];

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var bufferChild))
                {
                    if (ProcessSource(ref state, in receiver, clipRO, in source, bufferChild, in raycast, physicsRW, ref commands))
                    {
                        RemoveMemory(in source, bufferMemory, bufferPerceive);
                        continue;
                    }

                    AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
                    continue;
                }

                if (ProcessSource(ref state, in receiver, clipRO, in source, in raycast, physicsRW, ref commands))
                {
                    RemoveMemory(in source, bufferMemory, bufferPerceive);
                    continue;
                }

                AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
            }

            AppendMemory(in receiver, memoryRO, bufferPerceive, ref commands);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightMemory> memoryRO, DynamicBuffer<BufferSightChild> receiverBufferChild,
            RefRW<PhysicsWorldSingleton> physicsRW, in Buffers buffers, ref EntityCommandBuffer commands)
        {
            var bufferPerceive = buffers.Perceive[receiver.Entity];
            var bufferMemory = buffers.Memory[receiver.Entity];

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var sourceBufferChild))
                {
                    if (ProcessSource(ref state, in receiver, receiverBufferChild,
                            in source, sourceBufferChild, in raycast, physicsRW, ref commands))
                    {
                        RemoveMemory(in source, bufferMemory, bufferPerceive);
                        continue;
                    }

                    AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
                    continue;
                }

                if (ProcessSource(ref state, in receiver, receiverBufferChild, in source, in raycast, physicsRW, ref commands))
                {
                    RemoveMemory(in source, bufferMemory, bufferPerceive);
                    continue;
                }

                AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
            }

            AppendMemory(in receiver, memoryRO, bufferPerceive, ref commands);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightMemory> memoryRO, RefRW<PhysicsWorldSingleton> physicsRW,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            var bufferPerceive = buffers.Perceive[receiver.Entity];
            var bufferMemory = buffers.Memory[receiver.Entity];

            foreach (var cone in buffers.Cone[receiver.Entity])
            {
                var source = new Source(cone.Source, cone.Position);
                var raycast = new RayCast(in receiver, in source);

                if (buffers.Child.TryGetBuffer(cone.Source, out var bufferChild))
                {
                    if (ProcessSource(ref state, in receiver, in source, bufferChild, in raycast, physicsRW, ref commands))
                    {
                        RemoveMemory(in source, bufferMemory, bufferPerceive);
                        continue;
                    }

                    AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
                    continue;
                }

                if (ProcessSource(ref state, in receiver, in source, in raycast, physicsRW, ref commands))
                {
                    RemoveMemory(in source, bufferMemory, bufferPerceive);
                    continue;
                }

                AppendMemory(in receiver, in source, memoryRO, bufferPerceive, ref commands);
            }

            AppendMemory(in receiver, memoryRO, bufferPerceive, ref commands);
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

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, DynamicBuffer<BufferSightChild> receiverBufferChild,
            in Source source, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver.Entity, receiverBufferChild, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, in Source source, DynamicBuffer<BufferSightChild> bufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityWithClip(receiver.Entity, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, DynamicBuffer<BufferSightChild> bufferChild, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChildWithClip(receiver.Entity, bufferChild, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity != source.Entity)
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityWithClip(receiver.Entity, rayCast.Clip(clipRO));
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity != source.Entity)
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> receiverBufferChild,
            in Source source, DynamicBuffer<BufferSightChild> sourceBufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, receiverBufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, sourceBufferChild))
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source, DynamicBuffer<BufferSightChild> bufferChild,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (CollectorClosestIgnoreEntityAndChild.CheckHit(collector.Hit, source.Entity, bufferChild))
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, DynamicBuffer<BufferSightChild> bufferChild, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntityAndChild(receiver.Entity, bufferChild);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity != source.Entity)
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private bool ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source,
            in RayCast rayCast, RefRW<PhysicsWorldSingleton> physicsRW, ref EntityCommandBuffer commands)
        {
            var collector = new CollectorClosestIgnoreEntity(receiver.Entity);
            physicsRW.ValueRO.CollisionWorld.CastRay(rayCast.Input, ref collector);

            if (collector.Hit.Entity != source.Entity)
            {
                return false;
            }

            AppendPerceive(in receiver, in source, ref commands);
            return true;
        }

        private void AppendPerceive(in Receiver receiver, in Source source, ref EntityCommandBuffer commands)
        {
            commands.AppendToBuffer(receiver.Entity, new BufferSightPerceive
            {
                Source = source.Entity,
                Position = source.Position,
            });
        }

        private void AppendMemory(
            in Receiver receiver, in Source source, RefRO<ComponentSightMemory> memoryRO,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            for (var i = bufferPerceive.Length - 1; i >= 0; i--)
            {
                if (bufferPerceive[i].Source == source.Entity)
                {
                    commands.AppendToBuffer(receiver.Entity, new BufferSightMemory
                    {
                        Source = source.Entity,
                        Position = source.Position,
                        Time = memoryRO.ValueRO.Time,
                    });

                    bufferPerceive.RemoveAt(i);
                    return;
                }
            }
        }

        private void AppendMemory(
            in Receiver receiver, RefRO<ComponentSightMemory> memoryRO,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            for (var i = bufferPerceive.Length - 1; i >= 0; i--)
            {
                commands.AppendToBuffer(receiver.Entity, new BufferSightMemory
                {
                    Source = bufferPerceive[i].Source,
                    Position = bufferPerceive[i].Position,
                    Time = memoryRO.ValueRO.Time,
                });

                bufferPerceive.RemoveAt(i);
            }
        }

        private void RemoveMemory(
            in Source source, DynamicBuffer<BufferSightMemory> bufferMemory,
            DynamicBuffer<BufferSightPerceive> bufferPerceive)
        {
            for (var i = 0; i < bufferMemory.Length; i++)
            {
                if (bufferMemory[i].Source == source.Entity)
                {
                    bufferMemory.RemoveAt(i);
                    return;
                }
            }

            for (var i = bufferPerceive.Length - 1; i >= 0; i--)
            {
                if (bufferPerceive[i].Source == source.Entity)
                {
                    bufferPerceive.RemoveAt(i);
                    return;
                }
            }
        }

        private readonly struct Buffers
        {
            public readonly BufferLookup<BufferSightPerceive> Perceive;
            public readonly BufferLookup<BufferSightMemory> Memory;
            public readonly BufferLookup<BufferSightChild> Child;
            public readonly BufferLookup<BufferSightCone> Cone;

            public Buffers(
                BufferLookup<BufferSightPerceive> perceive,
                BufferLookup<BufferSightMemory> memory,
                BufferLookup<BufferSightChild> child,
                BufferLookup<BufferSightCone> cone)
            {
                Perceive = perceive;
                Memory = memory;
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