using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Perception
{
    [BurstCompile]
    public partial struct SystemSightPosition : ISystem
    {
        private EntityQuery _queryWithoutPosition;
        private EntityQuery _queryWithOffset;
        private EntityQuery _query;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightOffset> _handleOffset;
        private ComponentTypeHandle<LocalToWorld> _handleTransform;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPosition = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld>()
                .WithAny<TagSightReceiver, TagSightSource>()
                .WithNone<ComponentSightPosition>()
                .Build();
            _queryWithOffset = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ComponentSightPosition, ComponentSightOffset>()
                .WithAny<TagSightReceiver, TagSightSource>()
                .Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ComponentSightPosition>()
                .WithAny<TagSightReceiver, TagSightSource>()
                .WithNone<ComponentSightOffset>()
                .Build();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>();
            _handleOffset = SystemAPI.GetComponentTypeHandle<ComponentSightOffset>(isReadOnly: true);
            _handleTransform = SystemAPI.GetComponentTypeHandle<LocalToWorld>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var entity in _queryWithoutPosition.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new ComponentSightPosition());
            }

            commands.Playback(state.EntityManager);

            _handlePosition.Update(ref state);
            _handleOffset.Update(ref state);
            _handleTransform.Update(ref state);

            var jobWithOffset = new JobUpdatePositionWithOffset
            {
                HandlePosition = _handlePosition,
                HandleOffset = _handleOffset,
                HandleTransform = _handleTransform,
            }.ScheduleParallel(_queryWithOffset, state.Dependency);

            var job = new JobUpdatePosition
            {
                HandlePosition = _handlePosition,
                HandleTransform = _handleTransform,
            }.ScheduleParallel(_query, jobWithOffset);

            state.Dependency = job;
        }

        [BurstCompile]
        private struct JobUpdatePositionWithOffset : IJobChunk
        {
            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<ComponentSightOffset> HandleOffset;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positions = chunk.GetNativeArray(ref HandlePosition);
                var offsets = chunk.GetNativeArray(ref HandleOffset);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var offset = offsets[i];
                    var transform = transforms[i];

                    positions[i] = new ComponentSightPosition
                    {
                        Receiver = transform.Value.TransformPoint(in offset.Receiver),
                        Source = transform.Value.TransformPoint(in offset.Source),
                    };
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePosition : IJobChunk
        {
            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positions = chunk.GetNativeArray(ref HandlePosition);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var position = transforms[i].Position;
                    positions[i] = new ComponentSightPosition { Receiver = position, Source = position };
                }
            }
        }
    }
}