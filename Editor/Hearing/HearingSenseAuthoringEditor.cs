using UnityEditor;
using UnityEngine;

namespace Perception.Editor
{
    [CustomEditor(typeof(HearingSenseAuthoring)), CanEditMultipleObjects]
    public class HearingSenseAuthoringEditor : UnityEditor.Editor
    {
        protected HearingSenseAuthoring _authoring;
        protected SerializedObject _serializedAuthoring;

        protected virtual void OnEnable()
        {
            _authoring = target as HearingSenseAuthoring;
            _serializedAuthoring = new SerializedObject(_authoring);
        }

        public override void OnInspectorGUI()
        {
            _serializedAuthoring.Update();

            var property = _serializedAuthoring.GetIterator();
            property.NextVisible(true);

            while (property.name != "_isReceiver")
            {
                property.NextVisible(false);
            }

            property.boolValue = EditorGUILayout.Toggle(property.displayName, property.boolValue);
            var isReceiver = property.boolValue;
            property.NextVisible(false);

            while (property.name != "_isSource")
            {
                if (isReceiver)
                {
                    EditorGUILayout.PropertyField(property);
                }

                property.NextVisible(false);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            property.boolValue = EditorGUILayout.Toggle(property.displayName, property.boolValue);
            var isSource = property.boolValue;
            property.NextVisible(false);

            while (property.name != "_offset")
            {
                if (isSource)
                {
                    EditorGUILayout.PropertyField(property);
                }

                property.NextVisible(false);
            }

            if (isReceiver || isSource)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            do
            {
                if (isReceiver || isSource)
                {
                    EditorGUILayout.PropertyField(property);
                }
            } while (property.NextVisible(false));

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_authoring);
                _serializedAuthoring.ApplyModifiedProperties();
            }
        }
    }
}