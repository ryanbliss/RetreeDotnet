using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;


namespace RetreeCore.Unity.Editor
{
    [CustomPropertyDrawer(typeof(SerializedRetreeDictionary<,>), true)]
    public class SerializedRetreeDictionaryDrawer : PropertyDrawer
    {
        private bool _expanded = true;
        private static readonly float HeaderHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float Padding = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty entries = property.FindPropertyRelative("entries");

            // 1. Draw Header & Add Button
            Rect headerRect = new Rect(position.x, position.y, position.width, HeaderHeight);
            _expanded = EditorGUI.Foldout(headerRect, _expanded, label, true);

            if (_expanded)
            {
                Rect addBtnRect = new Rect(position.xMax - 30, position.y + HeaderHeight, 30, HeaderHeight);
                if (GUI.Button(addBtnRect, new GUIContent("+", "Add Entry")))
                {
                    // Insert at the very end
                    int newIndex = entries.arraySize;
                    entries.InsertArrayElementAtIndex(newIndex);

                    // IMPORTANT: When you insert, Unity copies the values from the previous 
                    // element into the new one. You usually want to clear them so the 
                    // new entry is "fresh".
                    var newElement = entries.GetArrayElementAtIndex(newIndex);
                    var keyProp = newElement.FindPropertyRelative("key");
                    var valProp = newElement.FindPropertyRelative("value");

                    // Reset to defaults (e.g., 0, empty string, or null)
                    ResetSerializedProperty(keyProp);
                    ResetSerializedProperty(valProp);

                    if (entries.serializedObject.targetObject is IHasDirty dictionary)
                    {
                        dictionary.IsDirty = true;
                    }
                    
                    if (entries.arraySize != newIndex + 1)
                    {
                        throw new Exception("Unexpected");
                    }

                    property.serializedObject.ApplyModifiedProperties();

                    if (entries.arraySize != newIndex + 1)
                    {
                        throw new Exception("Unexpected");
                    }
                }
                EditorGUI.indentLevel++;
                float currentY = position.y + (HeaderHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
                HashSet<object> keysFound = new HashSet<object>();

                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty element = entries.GetArrayElementAtIndex(i);
                    SerializedProperty keyProp = element.FindPropertyRelative("key");
                    SerializedProperty valueProp = element.FindPropertyRelative("value");

                    float keyHeight = EditorGUI.GetPropertyHeight(keyProp);
                    float valHeight = EditorGUI.GetPropertyHeight(valueProp);
                    float totalElementHeight = keyHeight + valHeight + EditorGUIUtility.standardVerticalSpacing;

                    // Draw a subtle background box for the pair
                    Rect boxRect = new Rect(position.x, currentY - 2, position.width, totalElementHeight + 4);
                    GUI.Box(boxRect, "");

                    // Check for duplicate keys
                    object keyVal = GetPropertyValue(keyProp);
                    bool isDuplicate = keyVal != null && keysFound.Contains(keyVal);
                    keysFound.Add(keyVal);

                    // --- KEY ROW ---
                    Rect kRect = new Rect(position.x + 5, currentY, position.width - 40, keyHeight);
                    Rect rRect = new Rect(position.xMax - 25, currentY, 25, keyHeight);

                    if (isDuplicate) GUI.color = Color.red;
                    EditorGUI.PropertyField(kRect, keyProp, new GUIContent("Key " + i), true);
                    GUI.color = Color.white;

                    if (GUI.Button(rRect, new GUIContent("x", "Remove")))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    currentY += keyHeight + EditorGUIUtility.standardVerticalSpacing;

                    // --- VALUE ROW ---
                    Rect vRect = new Rect(position.x + 5, currentY, position.width - 10, valHeight);
                    EditorGUI.PropertyField(vRect, valueProp, new GUIContent("Value"), true);

                    currentY += valHeight + (EditorGUIUtility.standardVerticalSpacing * 2);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_expanded) return HeaderHeight;

            SerializedProperty entries = property.FindPropertyRelative("entries");
            float totalHeight = (HeaderHeight * 2) + EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty element = entries.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("key");
                SerializedProperty valueProp = element.FindPropertyRelative("value");

                // Dynamic height: Key height + Value height + spacings
                totalHeight += EditorGUI.GetPropertyHeight(keyProp)
                             + EditorGUI.GetPropertyHeight(valueProp)
                             + (EditorGUIUtility.standardVerticalSpacing * 3);
            }

            // Add a bit of bottom padding
            return totalHeight + Padding;
        }

        private object GetPropertyValue(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.intValue,
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.Enum => prop.enumValueIndex,
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue,
                SerializedPropertyType.Boolean => prop.boolValue,
                _ => prop.propertyPath
            };
        }

        private void ResetSerializedProperty(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = 0; break;
                case SerializedPropertyType.Float: prop.floatValue = 0f; break;
                case SerializedPropertyType.String:
                    {
                        prop.stringValue = Guid.NewGuid().ToString();
                        break;
                    }
                case SerializedPropertyType.Boolean: prop.boolValue = false; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
                case SerializedPropertyType.ManagedReference: prop.managedReferenceValue = null; break;
                case SerializedPropertyType.Generic:
                    {
                        var obj = prop.boxedValue;
                        if (obj != null)
                            JsonUtility.FromJsonOverwrite("{}", obj);
                        break;
                    }
                default:
                    {
                         throw new ArgumentException($"{nameof(prop.propertyType)} type {prop.propertyType} is not valid", nameof(prop));
                    }
                    // Add more types if your keys/values use them
            }
        }
    }
}