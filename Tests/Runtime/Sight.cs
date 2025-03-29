using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TestTools;

namespace Perception.Tests
{
    public sealed class Sight
    {
        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity1, new TagSightReceiver());
            entityManager.AddComponentData(entity1, new LocalToWorld { Value = float4x4.identity });
            entityManager.AddComponentData(entity1, new ComponentSightOffset { Value = new float3(1, 1, 1) });

            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponentData(entity2, new TagSightSource());
            entityManager.AddComponentData(entity2, new LocalToWorld { Value = float4x4.Translate(new float3(2, 2, 2)) });
            entityManager.AddComponentData(entity2, new ComponentSightPosition());

            yield return null;

            Assert.True(entityManager.HasComponent<ComponentSightPosition>(entity1));
            Assert.False(entityManager.HasComponent<ComponentSightOffset>(entity2));

            Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentSightPosition>(entity1).Value);
            Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentSightPosition>(entity2).Value);

            entityManager.SetComponentData(entity1, new ComponentSightOffset { Value = new float3(3, 3, 3) });
            entityManager.SetComponentData(entity2, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });

            yield return null;

            Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentSightPosition>(entity1).Value);
            Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentSightPosition>(entity2).Value);
        }
    }
}