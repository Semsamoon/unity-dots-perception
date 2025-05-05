using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public class SightSenseAuthoring : MonoBehaviour
    {
        [SerializeField] protected bool _isReceiver;

        [Header("Receiver Requirements")]
        [SerializeField] protected float _coneRadius;
        [SerializeField] protected float2 _coneAnglesDegrees;
        [SerializeField] protected float _clipRadius;

        [Header("Receiver Modifiers")]
        [SerializeField] protected float _extendRadius;
        [SerializeField] protected float2 _extendAnglesDegrees;
        [SerializeField] protected float _extendClipRadius;
        [SerializeField] protected float3[] _rayOffsets;
        [SerializeField] protected CollisionFilterSerializable _collisionFilter;

        [Header("Receiver Additions")]
        [SerializeField] protected float3 _receiverOffset;
        [SerializeField] protected float _memoryTime;

        [Space]
        [SerializeField] protected bool _isSource;

        [Header("Source Additions")]
        [SerializeField] protected float3 _sourceOffset;

        [Header("Common")]
        [SerializeField] protected GameObject[] _children;
        [SerializeField] protected TeamFilterSerializable _teamFilter;

        public class Baker : Baker<SightSenseAuthoring>
        {
            public override void Bake(SightSenseAuthoring authoring)
            {
                if (!authoring._isReceiver && !authoring._isSource)
                {
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring._isReceiver)
                {
                    AddComponent(entity, new TagSightReceiver());
                    AddComponent(entity, new ComponentSightCone
                    {
                        Filter = authoring._collisionFilter,
                        AnglesCos = math.cos(math.radians(authoring._coneAnglesDegrees / 2)),
                        RadiusSquared = authoring._coneRadius * authoring._coneRadius,
                        ClipSquared = authoring._clipRadius * authoring._clipRadius,
                    });

                    if (authoring._extendRadius > 0 || math.any(authoring._extendAnglesDegrees > float2.zero))
                    {
                        var extendAnglesDegrees = authoring._coneAnglesDegrees + authoring._extendAnglesDegrees;
                        var extendRadius = authoring._coneRadius + authoring._extendRadius;
                        var extendClipRadius = authoring._clipRadius - authoring._extendClipRadius;

                        AddComponent(entity, new ComponentSightExtend
                        {
                            AnglesCos = math.cos(math.radians(extendAnglesDegrees / 2)),
                            RadiusSquared = extendRadius * extendRadius,
                            ClipSquared = extendClipRadius * extendClipRadius,
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

                if ((authoring._isReceiver && math.any(authoring._receiverOffset != float3.zero))
                    || (authoring._isSource && math.any(authoring._sourceOffset != float3.zero)))
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

                AddComponent<ComponentTeamFilter>(entity, authoring._teamFilter);
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

            _extendClipRadius = math.max(0, _extendClipRadius);
            _extendClipRadius = math.min(_clipRadius, _extendClipRadius);

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

                DrawCone(position, transform.rotation, _clipRadius - _extendClipRadius, _coneRadius + _extendRadius, extendHalfAngles, Color.yellow);
                DrawCone(position, transform.rotation, 0, _clipRadius - _extendClipRadius, extendHalfAngles, Color.gray);
                DrawCone(position, transform.rotation, _clipRadius, _coneRadius, coneHalfAngles, Color.green);
                DrawCone(position, transform.rotation, 0, _clipRadius, coneHalfAngles, Color.gray);
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

            DebugAdvanced.DrawCurve(position + verticalsOffset, in rotation, radiusY, halfAngleX * 2, in color, sparsity);
            DebugAdvanced.DrawCurve(position - verticalsOffset, in rotation, radiusY, halfAngleX * 2, in color, sparsity);

            if (halfAngleY > 0.5f)
            {
                DebugAdvanced.DrawCurve(in position, in rotation, radius, halfAngleX * 2, in color, sparsity);
            }

            var segments = (int)(halfAngleX * radius / 1000) + (int)halfAngleX + 1;
            var segmentsEulerAngles = new float3x2(new float3(0, halfAngleY, 0), new float3(0, -halfAngleY, 0));

            for (var i = 0; i <= segments; i++)
            {
                var angle = math.lerp(-halfAngleX, halfAngleX, (float)i / segments);
                var quaternion = math.mul(rotation, Unity.Mathematics.quaternion.Euler(0, angle, math.PIHALF));

                DebugAdvanced.DrawCurve(in position, in quaternion, radius, halfAngleY * 2, in color, sparsity);

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
}