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

            var hitCollector = new CollectorClosestIgnoreEntityAndChild(receiver, receiverBufferChild);
            physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(hitCollector.Hit, source, sourceBufferChild))
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

            var hitCollector = new CollectorClosestIgnoreEntity(receiver);
            physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

            if (!CollectorClosestIgnoreEntityAndChild.CheckHit(hitCollector.Hit, source, sourceBufferChild))
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

            var hitCollector = new CollectorClosestIgnoreEntityAndChild(receiver, receiverBufferChild);
            physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

            if (hitCollector.Hit.Entity == source)
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

            var hitCollector = new CollectorClosestIgnoreEntity(receiver);
            physicsWorld.CollisionWorld.CastRay(raycast, ref hitCollector);

            if (hitCollector.Hit.Entity == source)
            {
                commands.AppendToBuffer(receiver, new BufferSightPerceive
                {
                    Position = sourcePosition,
                    Source = source,
                });
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