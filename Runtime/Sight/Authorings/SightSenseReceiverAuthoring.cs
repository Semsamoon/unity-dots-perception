using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public class SightSenseReceiverAuthoring : MonoBehaviour
    {
        [SerializeField] protected float _coneRadius;
        [SerializeField] protected float2 _coneAnglesDegrees;

        [SerializeField] protected float _clipRadius;
        [SerializeField] protected float _extendRadius;
        [SerializeField] protected float2 _extendAnglesDegrees;
        [SerializeField] protected float3 _offset;

        [SerializeField] protected float _memoryTime;
        [SerializeField] protected GameObject[] _children;
        [SerializeField] protected float3[] _rayOffsets;

        public class Baker : Baker<SightSenseReceiverAuthoring>
        {
            public override void Bake(SightSenseReceiverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TagSightReceiver());
                AddComponent(entity, new ComponentSightCone
                {
                    RadiusSquared = authoring._coneRadius * authoring._coneRadius,
                    AnglesCos = math.cos(math.radians(authoring._coneAnglesDegrees / 2)),
                });

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

                if (math.any(authoring._offset != float3.zero))
                {
                    AddComponent(entity, new ComponentSightOffset { Receiver = authoring._offset });
                }

                if (authoring._memoryTime > 0)
                {
                    AddComponent(entity, new ComponentSightMemory { Time = authoring._memoryTime });
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

            var transform = this.transform;
            var coneHalfAngles = math.radians(_coneAnglesDegrees) / 2;
            var extendHalfAngles = math.radians(_coneAnglesDegrees + _extendAnglesDegrees) / 2;
            var position = transform.TransformPoint(_offset);

            DrawCone(position, transform.rotation, 0, _coneRadius + _extendRadius, extendHalfAngles, Color.yellow);
            DrawCone(position, transform.rotation, _clipRadius, _coneRadius, coneHalfAngles, Color.green);
            DrawCone(position, transform.rotation, 0, _clipRadius, extendHalfAngles, Color.gray);
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

            var segments = (int)(halfAngleX * radius / 1000) + (halfAngleX > 1 ? 2 : 1);

            for (var i = 0; i <= segments; i++)
            {
                var angle = math.lerp(-halfAngleX, halfAngleX, (float)i / segments);
                var quaternion = math.mul(rotation, Unity.Mathematics.quaternion.Euler(0, angle, math.PIHALF));

                DebugAdvanced.DrawCurve(position, quaternion, radius, halfAngleY * 2, color, sparsity);
            }
        }
#endif
    }
}