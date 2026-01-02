#if UNITY_EDITOR
using RabbitVsMole.GameData.Mutator;
using RabbitVsMole.GameData;
using UnityEditor;
using UnityEngine;

namespace RabbitVsMole.GameData.Editor
{
    [CustomPropertyDrawer(typeof(IMutatorEffect), true)]
    public class MutatorEffectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the actual type of the effect
            var typeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(typeName))
            {
                // No effect assigned, show dropdown to add one
                var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                if (EditorGUI.DropdownButton(rect, new GUIContent("Add Effect"), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Game Rules Bool Modifier"), false, () => SetEffect(property, typeof(GameRulesBoolModifier)));
                    menu.AddItem(new GUIContent("Game Objects Int Modifier"), false, () => SetEffect(property, typeof(GameObjectsIntModifier)));
                    menu.AddItem(new GUIContent("Game Objects Float Modifier"), false, () => SetEffect(property, typeof(GameObjectsFloatModifier)));
                    menu.AddItem(new GUIContent("Fight Int Modifier"), false, () => SetEffect(property, typeof(FightIntModifier)));
                    menu.AddItem(new GUIContent("Fight Float Modifier"), false, () => SetEffect(property, typeof(FightFloatModifier)));
                    menu.AddItem(new GUIContent("Economy Int Modifier"), false, () => SetEffect(property, typeof(EconomyIntModifier)));
                    menu.AddItem(new GUIContent("Economy Float Modifier"), false, () => SetEffect(property, typeof(EconomyFloatModifier)));
                    menu.AddItem(new GUIContent("Backpack Int Modifier"), false, () => SetEffect(property, typeof(BackpackIntModifier)));
                    menu.AddItem(new GUIContent("Avatar Float Modifier"), false, () => SetEffect(property, typeof(AvatarFloatModifier)));
                    menu.AddItem(new GUIContent("Action Times Float Modifier"), false, () => SetEffect(property, typeof(ActionTimesFloatModifier)));
                    menu.ShowAsContext();
                }
            }
            else
            {
                // Effect is assigned, show foldout with properties
                var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GetTypeDisplayName(typeName), true);

                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // Draw all child properties
                    var iterator = property.Copy();
                    var end = property.GetEndProperty();
                    var first = true;

                    while (iterator.NextVisible(first) && !SerializedProperty.EqualContents(iterator, end))
                    {
                        var height = EditorGUI.GetPropertyHeight(iterator, true);
                        var propRect = new Rect(position.x, y, position.width, height);
                        EditorGUI.PropertyField(propRect, iterator, true);
                        y += height + EditorGUIUtility.standardVerticalSpacing;
                        first = false;
                    }

                    // Add remove button
                    var buttonRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(buttonRect, "Remove Effect"))
                    {
                        property.managedReferenceValue = null;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded || string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var iterator = property.Copy();
            var end = property.GetEndProperty();
            var first = true;

            while (iterator.NextVisible(first) && !SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                first = false;
            }

            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Remove button
            return height;
        }

        private void SetEffect(SerializedProperty property, System.Type type)
        {
            var instance = System.Activator.CreateInstance(type);
            property.managedReferenceValue = instance;
            property.serializedObject.ApplyModifiedProperties();
        }

        private string GetTypeDisplayName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "None";

            var parts = fullTypeName.Split('.');
            var typeName = parts[parts.Length - 1];
            
            // Remove generic parameters if any
            var genericIndex = typeName.IndexOf('`');
            if (genericIndex >= 0)
                typeName = typeName.Substring(0, genericIndex);

            return typeName;
        }
    }
}
#endif

