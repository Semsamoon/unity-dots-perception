using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Perception.Editor
{
    [CustomPropertyDrawer(typeof(TeamFilterMaskAttribute))]
    public class TeamFilterMaskDrawer : PropertyDrawer
    {
        public static readonly string[] TeamNames =
        {
            "Team 1", "Team 2", "Team 3", "Team 4", "Team 5", "Team 6", "Team 7", "Team 8",
            "Team 9", "Team 10", "Team 11", "Team 12", "Team 13", "Team 14", "Team 15", "Team 16",
            "Team 17", "Team 18", "Team 19", "Team 20", "Team 21", "Team 22", "Team 23", "Team 24",
            "Team 25", "Team 26", "Team 27", "Team 28", "Team 29", "Team 30", "Team 31", "Team 32",
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            property.uintValue = math.asuint(EditorGUI.MaskField(position, label, math.asint(property.uintValue), TeamNames));
            EditorGUI.EndProperty();
        }
    }
}