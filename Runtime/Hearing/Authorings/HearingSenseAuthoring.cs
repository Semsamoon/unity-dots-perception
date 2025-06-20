﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public class HearingSenseAuthoring : MonoBehaviour
    {
        [SerializeField] protected bool _isReceiver;

        [Header("Receiver Additions")]
        [SerializeField] protected float _memoryTime;

        [Space]
        [SerializeField] protected bool _isSource;

        [Header("Source Requirements")]
        [SerializeField] protected float _speed;
        [SerializeField] protected float _maxRange;

        [Header("Source Modifiers")]
        [SerializeField] protected float _duration;

        [Header("Common")]
        [SerializeField] protected float3 _offset;
        [SerializeField] protected TeamFilterSerializable _teamFilter;

        public class Baker : Baker<HearingSenseAuthoring>
        {
            public override void Bake(HearingSenseAuthoring authoring)
            {
                if (!authoring._isReceiver && !authoring._isSource)
                {
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring._isReceiver)
                {
                    AddComponent(entity, new TagHearingReceiver());

                    if (authoring._memoryTime > 0)
                    {
                        AddComponent(entity, new ComponentHearingMemory { Time = authoring._memoryTime });
                    }
                }

                if (authoring._isSource)
                {
                    AddComponent(entity, new TagHearingSource());
                    AddComponent(entity, new ComponentHearingSphere
                    {
                        Speed = authoring._speed,
                        RangeSquared = authoring._maxRange * authoring._maxRange,
                        Duration = authoring._duration == -1 ? float.PositiveInfinity : authoring._duration,
                    });
                }

                if (math.any(authoring._offset != float3.zero))
                {
                    AddComponent(entity, new ComponentHearingOffset { Value = authoring._offset });
                }

                AddComponent<ComponentHearingFilter>(entity, authoring._teamFilter);
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _memoryTime = math.max(0, _memoryTime);

            _speed = math.max(0, _speed);
            _maxRange = math.max(0, _maxRange);

            _duration = _duration is >= 0 or > -1 and < -0.5f ? math.max(0, _duration) : -1;
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
                var position = transform.TransformPoint(_offset);
                DebugAdvanced.DrawOctahedron(position, Constants.StretchedShape * 0.5f, Constants.ColorPurple);
            }

            if (_isSource)
            {
                var transform = this.transform;
                var position = transform.TransformPoint(_offset);
                DebugAdvanced.DrawSphere(position, transform.rotation, math.max(0, _maxRange - _speed * math.max(0, _duration)), Constants.ColorBlue);
                DebugAdvanced.DrawSphere(position, transform.rotation, _maxRange, Constants.ColorCyan);
            }
        }
#endif
    }
}