#if UNITY_EDITOR

using System;
using SerializeInterface.Serializable;
using UnityEditor;
using UnityEngine;


    // [CustomEditor(typeof(SerializableTest))]
    // public class SerializableTestEditor : Editor
    // {
    //     private SerializedProperty m_TestSerializedProperty;
    //     
    //     private void OnEnable()
    //     {
    //         m_TestSerializedProperty = serializedObject.FindProperty("TestSerialized");
    //     }
    //
    //     public override void OnInspectorGUI()
    //     {
    //         DrawDefaultInspector();
    //
    //
    //         serializedObject.Update();
    //         EditorGUI.BeginChangeCheck();
    //         EditorGUILayout.PropertyField(m_TestSerializedProperty, new GUIContent("Your Property Name"), false);
    //         
    //         if (EditorGUI.EndChangeCheck())
    //         {
    //             serializedObject.ApplyModifiedProperties();
    //         }
    //         
    //         if (GUILayout.Button("Test"))
    //         {
    //             var property = serializedObject.FindProperty("TestSerialized");
    //             var instance = new TestA();
    //             property.managedReferenceValue = instance;
    //         }
    //         
    //         // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
    //         serializedObject.ApplyModifiedProperties();
    //     }
    // }


[CustomPropertyDrawer(typeof(DrawInterfaceAttribute))]
public class DrawInterfaceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // draw a box around the property
        GUI.Box(position, GUIContent.none);
        // get the property height
        var height = EditorGUI.GetPropertyHeight(property, label, true);
        // draw the property field
        EditorGUI.PropertyField(position, property, label, true);
        
        if (property.managedReferenceValue == null)
        {
            if (GUILayout.Button("Add New"))
            {
                var newValue = Activator.CreateInstance(typeof(TestA));
                property.managedReferenceValue = newValue;
            }

            return;
        }
        
        if (GUILayout.Button("Remove"))
        {
            property.managedReferenceValue = null;
        }
        // apply
        property.serializedObject.ApplyModifiedProperties();
    }
}

#endif