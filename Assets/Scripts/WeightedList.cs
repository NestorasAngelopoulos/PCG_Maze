using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System;

namespace com.github.NestorasAngelopoulos
{
    /// <summary>A pair of an object of type <typeparamref name="T"/>, alongside a <see cref="float"/> from 0 to 1.</summary>
    /// <remarks>To be used with <see cref="WeightedList{T}"/>.</remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class WeightedItem<T>
    {
        public T item;
        [Range(0f, 1f)] public float weight;
    }
    [CustomPropertyDrawer(typeof(WeightedItem<>))]
    public class WeightedItemDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Hide label if inside a list
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), property.propertyPath.Contains(".Array.data[") ? GUIContent.none : label);
            position.height -= EditorGUIUtility.standardVerticalSpacing;

            // Item
            Rect itemRect = position;
            itemRect.width *= 0.4f;
            itemRect.width -= 5;
            EditorGUI.PropertyField(itemRect, property.FindPropertyRelative("item"), GUIContent.none);

            // Weight
            Rect weightRect = position;
            weightRect.width *= 0.6f;
            weightRect.width -= 5;
            weightRect.x = position.x + position.width - weightRect.width;
            EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("weight"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }

    /// <summary>A list of objects of type <typeparamref name="T"/>, with weights paired to them.<br/></summary>
    /// <remarks>Constains methods for picking random items based on their weights, and balancing the weights to sum to 1.</remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class WeightedList<T>
    {
        [SerializeField] private List<WeightedItem<T>> list = new List<WeightedItem<T>>();
        private float[] lastKnownWeights = new float[0];

        /// <summary>Force sum of weights in list to not exceede 1.</summary>
        /// <remarks>Could break if multiple weights have changed since the last balancing.<br/>
        /// Use every time you change a weight (e.g. inside of OnValidate()).</remarks>
        public void BalanceWeights()
        {
            if (list.Count == 0) return;

            // Make sure new prop entries don't mess with percentage sum
            else if (list.Count > lastKnownWeights.Length && Time.time != 0) list[list.Count - 1].weight = 0f;

            // Check to see if weights have changed
            bool propChanceChanged = false;
            int changedIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (lastKnownWeights.Length == list.Count && list[i].weight != lastKnownWeights[i])
                {
                    propChanceChanged = true;
                    changedIndex = i;
                    break;
                }
            }

            // Subtract from other weights to maintain a sum of 1
            if (propChanceChanged)
            {
                float chanceSum = 0f;
                foreach (WeightedItem<T> prop in list) chanceSum += prop.weight;
                if (chanceSum > 1f) // Sum exceeds 1 
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i != changedIndex)
                        {
                            list[i].weight -= list[changedIndex].weight - lastKnownWeights[changedIndex];
                            if (list[i].weight <= 1e-5f) list[i].weight = 0;
                        }
                    }
                }
            }

            // Update stored weights
            lastKnownWeights = new float[list.Count];
            for (int i = 0; i < list.Count; i++) lastKnownWeights[i] = list[i].weight;
        }

        /// <summary>Picks a random item from the list. Higher weight equals higher chance to be picked.<br/>
        /// Always returns a value and the draw is based on the items' relative weights, whether they add up to 1 or not.<br/>
        /// If all items have a weight of 0, the first one will be returned.</summary>
        /// <returns>A random item from the list.</returns>
        public T RandomItem()
        {
            float weightsSum = 0f;
            foreach (WeightedItem<T> item in list) weightsSum += item.weight;
            return PickItem(weightsSum);
        }
        /// <summary>Picks a random item from the list. Higher weight equals higher chance to be picked.<br/>
        /// This variant of RandomItem() allows the return of <typeparamref name="T"/>'s default value when weights don't add up to 1.<br/></summary>
        /// <returns>A random item from the list, or <typeparamref name="T"/>'s default value.</returns>
        public T RandomItemOrDefault() => PickItem(1);
        private T PickItem(float maxSum)
        {
            float seed = UnityEngine.Random.Range(0f, maxSum);
            float cumulativeWeight = 0f;
            foreach (WeightedItem<T> item in list)
            {
                cumulativeWeight += item.weight;
                if (seed <= cumulativeWeight) return item.item;
            }
            return default;
        }

        public IEnumerator GetEnumerator() => list.GetEnumerator();
        public WeightedItem<T> this[int index]
        {
            get
            {
                if (index < 0 || index >= list.Count) throw new IndexOutOfRangeException();
                return list[index];
            }
            set
            {
                if (index < 0 || index >= list.Count) throw new IndexOutOfRangeException();
                list[index] = value;
            }
        }
        /// <summary>The number of elements in the list.</summary>
        public int Count => list.Count;

        /// <summary>Adds <paramref name="itemToAdd"/> at the end of the list.</summary>
        /// <param name="itemToAdd"></param>
        public void Add(WeightedItem<T> itemToAdd) => list.Add(itemToAdd);
        /// <summary>Removes the <see cref="WeightedItem{T}"/> at <paramref name="index"/>.</summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => list.RemoveAt(index);
        /// <summary>Empties the list.</summary>
        public void Clear() => list.Clear();
        /// <param name="itemToSearch">The <see cref="WeightedItem{T}"/> to search for.</param>
        /// <returns>Whether or not the give <see cref="WeightedItem{T}"/> is contained in the list.</returns>
        public bool Contains(WeightedItem<T> itemToSearch) => list.Contains(itemToSearch);
        /// <param name="itemToSearch">The <see cref="WeightedItem{T}"/> to search for.</param>
        /// <returns>The index of the given <see cref="WeightedItem{T}"/>.</returns>
        public int FindIndex(Predicate<WeightedItem<T>> match) => list.FindIndex(match);
        /// <summary>Finds the index of a <see cref="WeightedItem{T}"/> in the list using the given comparer</summary>
        /// <param name="comparer">The <see cref="IComparer{WeightedItem{T}}"/> to use for the search.</param>
        /// <param name="itemToSearch">The <see cref="WeightedItem{T}"/> to search for.</param>
        /// <returns>The index of the given <see cref="WeightedItem{T}"/>.</returns>
        public int BinarySearch(WeightedItem<T> itemToSearch, IComparer<WeightedItem<T>> comparer) => list.BinarySearch(itemToSearch, comparer);
    }
    [CustomPropertyDrawer(typeof(WeightedList<>))]
    public class WeightedListDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.isExpanded = true;
            SerializedProperty list = property.FindPropertyRelative("list");
            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Handle Drag & Drop
            Event evt = Event.current;
            if (labelRect.Contains(evt.mousePosition))
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            Type T = GetListElementType(property);
                            if (T != null && T.IsAssignableFrom(DragAndDrop.objectReferences[0].GetType()))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                                if (evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();

                                    int insertIndex = list.arraySize;
                                    foreach (var obj in DragAndDrop.objectReferences)
                                    {
                                        if (T.IsAssignableFrom(obj.GetType()))
                                        {
                                            list.arraySize++;

                                            // Access the newly added element
                                            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);

                                            // Set the 'item' field to dragged object
                                            SerializedProperty item = element.FindPropertyRelative("item");
                                            item.objectReferenceValue = obj;

                                            // Set weight initially to zero
                                            SerializedProperty weight = element.FindPropertyRelative("weight");
                                            weight.floatValue = 0f;
                                        }
                                    }

                                    list.serializedObject.ApplyModifiedProperties();
                                }
                                evt.Use();
                            }
                        }
                        break;
                }
            }

            // Draw list
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(list, true)), list, GUIContent.none, true);

            // Draw label
            GUI.Label(labelRect, property.displayName, new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;
            if (property.isExpanded)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("list"), true);
            }
            return height;
        }

        private Type GetListElementType(SerializedProperty property)
        {
            if (property == null) return null;

            object target = property.serializedObject.targetObject;
            Type targetType = target.GetType();
            FieldInfo field = null;

            // Find correct field (even nested paths)
            Type currentType = targetType;
            object currentObject = target;
            string[] pathParts = property.propertyPath.Split('.');
            for (int i = 0; i < pathParts.Length; i++)
            {
                string fieldName = pathParts[i];
                field = currentType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null) return null;

                if (i < pathParts.Length - 1)
                {
                    currentObject = field.GetValue(currentObject);
                    if (currentObject == null) return null;
                    currentType = currentObject.GetType();
                }
            }

            // Get generic argument type
            if (field != null && field.FieldType.IsGenericType)
            {
                Type listType = field.FieldType;
                return listType.GetGenericArguments()[0];
            }

            return null;
        }
    }
}