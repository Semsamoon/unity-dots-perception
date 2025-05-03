using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Perception
{
    public class SightSenseAuthoring : MonoBehaviour
    {
        [SerializeField] protected bool _isReceiver;

        [Header("Receiver Requirements")]
        [SerializeField] protected float _coneRadius;
        [SerializeField] protected float2 _coneAnglesDegrees;

        [Header("Receiver Modifiers")]
        [SerializeField] protected float _clipRadius;
        [SerializeField] protected float _extendRadius;
        [SerializeField] protected float2 _extendAnglesDegrees;
        [SerializeField] protected float3[] _rayOffsets;
        [SerializeField] protected CollisionFilterSerializable _collisionFilter;

        [Header("Receiver Additions")]
        [SerializeField] protected float3 _receiverOffset;
        [SerializeField] protected float _memoryTime;

        [Space]
        [SerializeField] protected bool _isSource;

        [Header("Source Additions")]
        [SerializeField] protected float3 _sourceOffset;

        [Header("Common Additions")]
        [SerializeField] protected GameObject[] _children;

        public class Baker : Baker<SightSenseAuthoring>
        {
            public override void Bake(SightSenseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring._isReceiver)
                {
                    AddComponent(entity, new TagSightReceiver());
                    AddComponent(entity, new ComponentSightCone
                    {
                        RadiusSquared = authoring._coneRadius * authoring._coneRadius,
                        AnglesCos = math.cos(math.radians(authoring._coneAnglesDegrees / 2)),
                    });
                    AddComponent(entity, new ComponentSightFilter { Value = authoring._collisionFilter });

                    if (authoring._clipRadius > 0)
                    {
                        AddComponent(entity, new ComponentSightClip
                        {
                            RadiusSquared = authoring._clipRadius * authoring._clipRadius
                        });
                    }

                    if (authoring._extendRadius > 0 || math.any(authoring._extendAnglesDegrees > float2.zero))
                    {
                        var extendRadius = authoring._coneRadius + authoring._extendRadius;
                        var extendAnglesDegrees = authoring._coneAnglesDegrees + authoring._extendAnglesDegrees;

                        AddComponent(entity, new ComponentSightExtend
                        {
                            RadiusSquared = extendRadius * extendRadius,
                            AnglesCos = math.cos(math.radians(extendAnglesDegrees / 2)),
                        });
                    }

                    if (authoring._memoryTime > 0)
                    {
                        AddComponent(entity, new ComponentSightMemory { Time = authoring._memoryTime });
                    }

                    if (authoring._rayOffsets is { Length: > 0 })
                    {
                        AddComponent(entity, new TagSightRayMultiple());
                        AddBuffer<BufferSightRayOffset>(entity);

                        foreach (var rayOffset in authoring._rayOffsets)
                        {
                            AppendToBuffer(entity, new BufferSightRayOffset { Value = rayOffset });
                        }
                    }
                    else
                    {
                        AddComponent(entity, new TagSightRaySingle());
                    }
                }

                if (authoring._isSource)
                {
                    AddComponent(entity, new TagSightSource());
                }

                if (authoring._isReceiver || authoring._isSource)
                {
                    if (math.any(authoring._receiverOffset != float3.zero) || math.any(authoring._sourceOffset != float3.zero))
                    {
                        AddComponent(entity, new ComponentSightOffset
                        {
                            Receiver = authoring._isReceiver ? authoring._receiverOffset : float3.zero,
                            Source = authoring._isSource ? authoring._sourceOffset : float3.zero,
                        });
                    }

                    if (authoring._children is { Length: > 0 })
                    {
                        AddBuffer<BufferSightChild>(entity);

                        foreach (var child in authoring._children)
                        {
                            if (child)
                            {
                                AppendToBuffer(entity, new BufferSightChild { Value = GetEntity(child, TransformUsageFlags.Dynamic) });
                            }
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _coneRadius = math.max(0, _coneRadius);
            _coneAnglesDegrees = math.max(new float2(0, 0), _coneAnglesDegrees);
            _coneAnglesDegrees = math.min(new float2(360, 180), _coneAnglesDegrees);

            _clipRadius = math.max(0, _clipRadius);
            _clipRadius = math.min(_coneRadius, _clipRadius);

            _extendRadius = math.max(0, _extendRadius);
            _extendAnglesDegrees = math.max(new float2(0, 0), _extendAnglesDegrees);
            _extendAnglesDegrees = math.min(new float2(360, 180) - _coneAnglesDegrees, _extendAnglesDegrees);

            _memoryTime = math.max(0, _memoryTime);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (_isReceiver)
            {
                var transform = this.transform;
                var coneHalfAngles = math.radians(_coneAnglesDegrees) / 2;
                var extendHalfAngles = math.radians(_coneAnglesDegrees + _extendAnglesDegrees) / 2;
                var position = transform.TransformPoint(_receiverOffset);

                DrawCone(position, transform.rotation, _clipRadius, _coneRadius + _extendRadius, extendHalfAngles, Color.yellow);
                DrawCone(position, transform.rotation, _clipRadius, _coneRadius, coneHalfAngles, Color.green);
                DrawCone(position, transform.rotation, 0, _clipRadius, extendHalfAngles, Color.gray);
            }

            if (_isSource)
            {
                var transform = this.transform;
                var position = transform.TransformPoint(_sourceOffset);
                DebugAdvanced.DrawOctahedron(position, new float3(0.25f, 0.5f, 0.25f), Color.blue);
            }
        }

        public static void DrawCone(float3 position, quaternion rotation, float clip, float radius, float2 halfAnglesRadians, Color color, int sparsity = 1000)
        {
            var halfAngleX = halfAnglesRadians.x;
            var halfAngleY = halfAnglesRadians.y;

            var eulerAngles = new float3x4(
                new float3(halfAngleY, halfAngleX, 0),
                new float3(-halfAngleY, halfAngleX, 0),
                new float3(halfAngleY, -halfAngleX, 0),
                new float3(-halfAngleY, -halfAngleX, 0));

            for (var i = 0; i < 4; i++)
            {
                var quaternion = Unity.Mathematics.quaternion.Euler(eulerAngles[i]);
                var start = math.rotate(rotation, math.rotate(quaternion, new float3(0, 0, clip)));
                var end = math.rotate(rotation, math.rotate(quaternion, new float3(0, 0, radius)));

                Debug.DrawLine(position + start, position + end, color);
            }

            var offsetY = math.sin(halfAngleY) * radius + radius;
            var radiusY = math.sqrt(2 * radius * math.abs(offsetY) - offsetY * offsetY);
            var verticalsOffset = math.rotate(rotation, new Vector3(0, offsetY - radius, 0));

            DebugAdvanced.DrawCurve(position + verticalsOffset, rotation, radiusY, halfAngleX * 2, color, sparsity);
            DebugAdvanced.DrawCurve(position - verticalsOffset, rotation, radiusY, halfAngleX * 2, color, sparsity);

            if (halfAngleY > 0.5f)
            {
                DebugAdvanced.DrawCurve(position, rotation, radius, halfAngleX * 2, color, sparsity);
            }

            var segments = (int)(halfAngleX * radius / 1000) + (int)halfAngleX + 1;
            var segmentsEulerAngles = new float3x2(new float3(0, halfAngleY, 0), new float3(0, -halfAngleY, 0));

            for (var i = 0; i <= segments; i++)
            {
                var angle = math.lerp(-halfAngleX, halfAngleX, (float)i / segments);
                var quaternion = math.mul(rotation, Unity.Mathematics.quaternion.Euler(0, angle, math.PIHALF));

                DebugAdvanced.DrawCurve(position, quaternion, radius, halfAngleY * 2, color, sparsity);

                for (var j = 0; j < 2; j++)
                {
                    var end = math.rotate(quaternion, math.rotate(quaternion.Euler(segmentsEulerAngles[j]), new float3(0, 0, radius)));
                    var start = math.rotate(quaternion, math.rotate(quaternion.Euler(segmentsEulerAngles[j]), new float3(0, 0, clip)));

                    Debug.DrawLine(position + start, position + end, color);
                }
            }
        }
#endif
    }

    [Serializable]
    public struct CollisionFilterSerializable
    {
        public LayerMask BelongsTo;
        public LayerMask CollidesWith;
        public int GroupIndex;

        public static implicit operator CollisionFilter(in CollisionFilterSerializable serializable)
        {
            return new CollisionFilter
            {
                BelongsTo = math.asuint(serializable.BelongsTo.value),
                CollidesWith = math.asuint(serializable.CollidesWith.value),
                GroupIndex = serializable.GroupIndex,
            };
        }

        public static implicit operator CollisionFilterSerializable(in CollisionFilter filter)
        {
            return new CollisionFilterSerializable
            {
                BelongsTo = math.asint(filter.BelongsTo),
                CollidesWith = math.asint(filter.CollidesWith),
                GroupIndex = filter.GroupIndex,
            };
        }
    }
}