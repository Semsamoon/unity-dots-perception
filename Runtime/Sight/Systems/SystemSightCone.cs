using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightPosition))]
    public partial struct SystemSightCone : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
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
                         .WithNone<BufferSightCone>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightCone>(receiver);
            }

            commands.Playback(state.EntityManager);
            commands = new EntityCommandBuffer(Allocator.Temp);

            var buffers = new Buffers(
                SystemAPI.GetBufferLookup<BufferSightPerceive>(),
                SystemAPI.GetBufferLookup<BufferSightCone>());

            foreach (var (transformRO, positionRO, coneRO, clipRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, transformRO, coneRO);
                ProcessReceiver(ref state, in receiverData, extendRO, clipRO, in buffers, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightClip>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, transformRO, coneRO);
                ProcessReceiver(ref state, in receiverData, extendRO, in buffers, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, transformRO, coneRO);
                ProcessReceiver(ref state, in receiverData, clipRO, in buffers, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightClip>()
                         .WithEntityAccess())
            {
                var receiverData = new Receiver(receiver, positionRO.ValueRO.Value, transformRO, coneRO);
                ProcessReceiver(ref state, in receiverData, in buffers, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightExtend> extendRO, RefRO<ComponentSightClip> clipRO,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Cone[receiver.Entity].Clear();

            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourceData = new Source(source, sourcePositionRO.ValueRO.Value);
                var coneCast = IsPerceived(source, buffers.Perceive[receiver.Entity])
                    ? new ConeCast(in receiver, sourcePositionRO, extendRO)
                    : new ConeCast(in receiver, sourcePositionRO);
                ProcessSource(ref state, in receiver, in sourceData, clipRO, in coneCast, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightExtend> extendRO,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Cone[receiver.Entity].Clear();

            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourceData = new Source(source, sourcePositionRO.ValueRO.Value);
                var coneCast = IsPerceived(source, buffers.Perceive[receiver.Entity])
                    ? new ConeCast(in receiver, sourcePositionRO, extendRO)
                    : new ConeCast(in receiver, sourcePositionRO);
                ProcessSource(ref state, in receiver, in sourceData, in coneCast, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, RefRO<ComponentSightClip> clipRO,
            in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Cone[receiver.Entity].Clear();

            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourceData = new Source(source, sourcePositionRO.ValueRO.Value);
                var coneCast = new ConeCast(in receiver, sourcePositionRO);
                ProcessSource(ref state, in receiver, in sourceData, clipRO, in coneCast, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            in Receiver receiver, in Buffers buffers, ref EntityCommandBuffer commands)
        {
            buffers.Cone[receiver.Entity].Clear();

            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourceData = new Source(source, sourcePositionRO.ValueRO.Value);
                var coneCast = new ConeCast(in receiver, sourcePositionRO);
                ProcessSource(ref state, in receiver, in sourceData, in coneCast, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source, RefRO<ComponentSightClip> clipRO,
            in ConeCast coneCast, ref EntityCommandBuffer commands)
        {
            if (IsInsideCone(in coneCast, clipRO.ValueRO.RadiusSquared))
            {
                commands.AppendToBuffer(receiver.Entity, new BufferSightCone
                {
                    Source = source.Entity,
                    Position = source.Position,
                });
            }
        }

        private void ProcessSource(ref SystemState state,
            in Receiver receiver, in Source source,
            in ConeCast coneCast, ref EntityCommandBuffer commands)
        {
            if (IsInsideCone(in coneCast))
            {
                commands.AppendToBuffer(receiver.Entity, new BufferSightCone
                {
                    Source = source.Entity,
                    Position = source.Position,
                });
            }
        }

        private bool IsInsideCone(in ConeCast coneCast, float clipRadiusSquared)
        {
            var difference = coneCast.Target - coneCast.Origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > coneCast.RadiusSquared || distanceSquared < clipRadiusSquared)
            {
                return false;
            }

            var direction = difference * math.rsqrt(distanceSquared);
            var directionLocal = coneCast.Transform.ValueRO.Value.TransformDirection(direction);

            return directionLocal.x / directionLocal.z <= coneCast.AnglesTan.x
                   && directionLocal.y / directionLocal.z <= coneCast.AnglesTan.y;
        }

        private bool IsInsideCone(in ConeCast coneCast)
        {
            var difference = coneCast.Target - coneCast.Origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > coneCast.RadiusSquared)
            {
                return false;
            }

            var direction = difference * math.rsqrt(distanceSquared);
            var directionLocal = coneCast.Transform.ValueRO.Value.TransformDirection(direction);

            return directionLocal.x / directionLocal.z <= coneCast.AnglesTan.x
                   && directionLocal.y / directionLocal.z <= coneCast.AnglesTan.y;
        }

        private bool IsPerceived(Entity entity, DynamicBuffer<BufferSightPerceive> bufferPerceive)
        {
            foreach (var perceive in bufferPerceive)
            {
                if (perceive.Source == entity)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct Buffers
        {
            public readonly BufferLookup<BufferSightPerceive> Perceive;
            public readonly BufferLookup<BufferSightCone> Cone;

            public Buffers(BufferLookup<BufferSightPerceive> perceive, BufferLookup<BufferSightCone> cone)
            {
                Perceive = perceive;
                Cone = cone;
            }
        }

        private readonly struct Receiver
        {
            public readonly float3 Position;
            public readonly Entity Entity;
            public readonly RefRO<LocalToWorld> Transform;
            public readonly RefRO<ComponentSightCone> Cone;

            public Receiver(Entity entity, float3 position, RefRO<LocalToWorld> transform, RefRO<ComponentSightCone> cone)
            {
                Entity = entity;
                Position = position;
                Transform = transform;
                Cone = cone;
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

        private readonly struct ConeCast
        {
            public readonly float3 Origin;
            public readonly float3 Target;
            public readonly float2 AnglesTan;
            public readonly float RadiusSquared;
            public readonly RefRO<LocalToWorld> Transform;

            public ConeCast(in Receiver receiver, RefRO<ComponentSightPosition> sourcePositionRO)
            {
                Origin = receiver.Position;
                Target = sourcePositionRO.ValueRO.Value;
                AnglesTan = receiver.Cone.ValueRO.AnglesTan;
                RadiusSquared = receiver.Cone.ValueRO.RadiusSquared;
                Transform = receiver.Transform;
            }

            public ConeCast(in Receiver receiver, RefRO<ComponentSightPosition> sourcePositionRO, RefRO<ComponentSightExtend> extendRO)
            {
                Origin = receiver.Position;
                Target = sourcePositionRO.ValueRO.Value;
                AnglesTan = extendRO.ValueRO.AnglesTan;
                RadiusSquared = extendRO.ValueRO.RadiusSquared;
                Transform = receiver.Transform;
            }
        }
    }
}