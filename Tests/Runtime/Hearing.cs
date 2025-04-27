using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TestTools;

namespace Perception.Tests
{
    public sealed class Hearing
    {
        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Offset(new float3(1, 1, 1)).Build();
            var source = new EntityBuilder(entityManager, new float3(2, 2, 2)).Source().Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasComponent<ComponentHearingPosition>(receiver));
                Assert.False(entityManager.HasComponent<ComponentHearingOffset>(source));
                Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Current);
                Assert.AreEqual(new float3(), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);
                Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentHearingPosition>(source).Current);
                Assert.AreEqual(new float3(), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);

                entityManager.SetComponentData(receiver, new ComponentHearingOffset { Value = new float3(3, 3, 3) });
                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });
                yield return null;
                Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Current);
                Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);
                Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentHearingPosition>(source).Current);
                Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentHearingPosition>(source).Previous);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        private struct EntityBuilder
        {
            private EntityManager _entityManager;
            private readonly Entity _entity;

            public EntityBuilder(EntityManager entityManager, float3 position = default)
            {
                _entityManager = entityManager;
                _entity = entityManager.CreateEntity();
                _entityManager.AddComponentData(_entity, new LocalToWorld { Value = float4x4.Translate(position) });
            }

            public EntityBuilder Receiver()
            {
                _entityManager.AddComponentData(_entity, new TagHearingReceiver());
                return this;
            }

            public EntityBuilder Source()
            {
                _entityManager.AddComponentData(_entity, new TagHearingSource());
                return this;
            }

            public EntityBuilder Offset(float3 offset = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentHearingOffset { Value = offset });
                return this;
            }

            public Entity Build()
            {
                return _entity;
            }
        }
    }
}