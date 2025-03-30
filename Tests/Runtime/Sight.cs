using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Perception.Tests
{
    public sealed class Sight
    {
        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var receiver = entityManager.CreateEntity();
            entityManager.AddComponentData(receiver, new TagSightReceiver());
            entityManager.AddComponentData(receiver, new LocalToWorld { Value = float4x4.identity });
            entityManager.AddComponentData(receiver, new ComponentSightOffset { Value = new float3(1, 1, 1) });

            var source = entityManager.CreateEntity();
            entityManager.AddComponentData(source, new TagSightSource());
            entityManager.AddComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(2, 2, 2)) });
            entityManager.AddComponentData(source, new ComponentSightPosition());

            yield return null;

            Assert.True(entityManager.HasComponent<ComponentSightPosition>(receiver));
            Assert.False(entityManager.HasComponent<ComponentSightOffset>(source));

            Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentSightPosition>(receiver).Value);
            Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentSightPosition>(source).Value);

            entityManager.SetComponentData(receiver, new ComponentSightOffset { Value = new float3(3, 3, 3) });
            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });

            yield return null;

            Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentSightPosition>(receiver).Value);
            Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentSightPosition>(source).Value);
        }

        [UnityTest]
        public IEnumerator Cone()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var receiver = entityManager.CreateEntity();
            entityManager.AddComponentData(receiver, new TagSightReceiver());
            entityManager.AddComponentData(receiver, new LocalToWorld { Value = float4x4.identity });
            entityManager.AddComponentData(receiver, new ComponentSightCone { AnglesTan = float2.zero, RadiusSquared = 4 });

            var source = entityManager.CreateEntity();
            entityManager.AddComponentData(source, new TagSightSource());
            entityManager.AddComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 2)) });

            yield return null;
            Assert.True(entityManager.HasBuffer<BufferSightInsideCone>(receiver));
            var buffer = entityManager.GetBuffer<BufferSightInsideCone>(receiver);

            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(source, buffer[0].Source);
            Assert.AreEqual(new float3(0, 0, 2), buffer[0].Position);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(1, 1, 1)) });
            yield return null;
            buffer = entityManager.GetBuffer<BufferSightInsideCone>(receiver);
            Assert.AreEqual(0, buffer.Length);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 3)) });
            yield return null;
            buffer = entityManager.GetBuffer<BufferSightInsideCone>(receiver);
            Assert.AreEqual(0, buffer.Length);

            entityManager.SetComponentData(receiver, new ComponentSightCone { AnglesTan = new float2(1, 1), RadiusSquared = 16 });

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(2, 2, 2)) });
            yield return null;
            buffer = entityManager.GetBuffer<BufferSightInsideCone>(receiver);
            Assert.AreEqual(1, buffer.Length);
        }

        [UnityTest]
        public IEnumerator Perceive()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var awaitPhysics = new WaitForSeconds(0.1f);
            var collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Orientation = quaternion.identity, Size = new float3(1, 1, 1)
            }, CollisionFilter.Default);

            var receiver = entityManager.CreateEntity();
            entityManager.AddComponentData(receiver, new TagSightReceiver());
            entityManager.AddComponentData(receiver, new LocalToWorld { Value = float4x4.identity });
            entityManager.AddComponentData(receiver, new PhysicsCollider { Value = collider });
            entityManager.AddSharedComponent(receiver, new PhysicsWorldIndex());

            var source = entityManager.CreateEntity();
            entityManager.AddComponentData(source, new TagSightSource());
            entityManager.AddComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 5)) });
            entityManager.AddComponentData(source, new PhysicsCollider { Value = collider });
            entityManager.AddSharedComponent(source, new PhysicsWorldIndex());

            var obstacle = entityManager.CreateEntity();
            entityManager.AddComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 3)) });
            entityManager.AddComponentData(obstacle, new PhysicsCollider { Value = collider });
            entityManager.AddSharedComponent(obstacle, new PhysicsWorldIndex());

            yield return awaitPhysics;
            Assert.True(entityManager.HasBuffer<BufferSightPerceive>(receiver));
            var buffer = entityManager.GetBuffer<BufferSightPerceive>(receiver);
            Assert.AreEqual(0, buffer.Length);

            entityManager.SetComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 7)) });
            yield return awaitPhysics;
            buffer = entityManager.GetBuffer<BufferSightPerceive>(receiver);
            Assert.AreEqual(1, buffer.Length);

            Assert.AreEqual(source, buffer[0].Source);
            Assert.AreEqual(new float3(0, 0, 5), buffer[0].Position);

            entityManager.AddComponentData(receiver, new ComponentSightCone { AnglesTan = new float2(1, 1), RadiusSquared = 25 });
            yield return awaitPhysics;
            buffer = entityManager.GetBuffer<BufferSightPerceive>(receiver);
            Assert.AreEqual(1, buffer.Length);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 5, 5)) });
            yield return awaitPhysics;
            buffer = entityManager.GetBuffer<BufferSightPerceive>(receiver);
            Assert.AreEqual(0, buffer.Length);
        }
    }
}