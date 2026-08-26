#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Runtime.Attributers;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Editor.PropertyDrawers
{
    /// <summary>
    ///     <see cref="SubclassSelectorAttribute"/> が指定されたフィールドに、
    ///     派生型を選択するポップアップを表示するDrawer。
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    internal sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, TypeCacheData> s_typeCache = new();

        /// <summary>
        ///     プロパティのGUIを描画する。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Type baseType = GetBaseType(property);
            if (baseType == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            TypeCacheData cacheData = GetCacheData(baseType);

            Rect popupPosition = new(position)
            {
                height = EditorGUIUtility.singleLineHeight
            };

            Rect propertyPosition = new(position)
            {
                y = position.y + EditorGUIUtility.singleLineHeight
                    + EditorGUIUtility.standardVerticalSpacing,
                height = position.height
                    - EditorGUIUtility.singleLineHeight
                    - EditorGUIUtility.standardVerticalSpacing
            };

            int currentTypeIndex = Array.IndexOf(
                cacheData.FullNames,
                property.managedReferenceFullTypename);

            if (currentTypeIndex < 0)
            {
                currentTypeIndex = 0;
            }

            EditorGUI.BeginProperty(position, label, property);

            int selectedTypeIndex = EditorGUI.Popup(
                popupPosition,
                label.text,
                currentTypeIndex,
                cacheData.DisplayNames);

            if (currentTypeIndex != selectedTypeIndex)
            {
                SetManagedReferenceType(
                    property,
                    cacheData.Types[selectedTypeIndex]);
            }

            if (property.managedReferenceValue != null)
            {
                EditorGUI.PropertyField(
                    propertyPosition,
                    property,
                    GUIContent.none,
                    true);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        ///     プロパティの描画に必要な高さを取得する。
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float popupHeight = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue == null)
            {
                return popupHeight;
            }

            float propertyHeight = EditorGUI.GetPropertyHeight(
                property,
                GUIContent.none,
                true);

            return popupHeight
                + EditorGUIUtility.standardVerticalSpacing
                + propertyHeight;
        }

        /// <summary>
        ///     指定された基底型のキャッシュデータを取得する。
        /// </summary>
        private static TypeCacheData GetCacheData(Type baseType)
        {
            if (s_typeCache.TryGetValue(baseType, out TypeCacheData cacheData))
            {
                return cacheData;
            }

            cacheData = CreateCacheData(baseType);
            s_typeCache.Add(baseType, cacheData);

            return cacheData;
        }

        /// <summary>
        ///     指定された基底型から選択可能な派生型情報を作成する。
        /// </summary>
        private static TypeCacheData CreateCacheData(Type baseType)
        {
            Type[] selectableTypes = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(IsSelectableType)
                .OrderBy(type => GetTypePath(baseType, type))
                .ToArray();

            Type[] types = selectableTypes
                .Prepend(null)
                .ToArray();

            string[] displayNames = types
                .Select(type => type == null
                    ? "<null>"
                    : GetTypePath(baseType, type))
                .ToArray();

            string[] fullNames = types
                .Select(GetManagedReferenceFullTypeName)
                .ToArray();

            return new TypeCacheData(
                types,
                displayNames,
                fullNames);
        }

        /// <summary>
        ///     指定された型がInspectorから選択可能か判定する。
        /// </summary>
        private static bool IsSelectableType(Type type)
        {
            return type is
            {
                IsClass: true,
                IsAbstract: false,
                IsGenericTypeDefinition: false
            }
            && !typeof(UnityEngine.Object).IsAssignableFrom(type)
            && type.GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        ///     基底型から対象型までの継承階層をPopup用のパスとして取得する。
        /// </summary>
        private static string GetTypePath(Type baseType, Type type)
        {
            var typeNames = new Stack<string>();

            Type currentType = type;

            while (currentType != null && currentType != baseType)
            {
                typeNames.Push(currentType.Name);
                currentType = currentType.BaseType;
            }

            return string.Join("/", typeNames);
        }

        /// <summary>
        ///     ManagedReferenceが保持しているフィールドの基底型を取得する。
        /// </summary>
        private static Type GetBaseType(SerializedProperty property)
        {
            string fieldTypeName = property.managedReferenceFieldTypename;

            if (string.IsNullOrEmpty(fieldTypeName))
            {
                return null;
            }

            int separatorIndex = fieldTypeName.IndexOf(' ');

            if (separatorIndex < 0)
            {
                return null;
            }

            string assemblyName = fieldTypeName[..separatorIndex];
            string typeName = fieldTypeName[(separatorIndex + 1)..];

            return Type.GetType($"{typeName}, {assemblyName}");
        }

        /// <summary>
        ///     UnityがManagedReferenceで使用する完全な型名を取得する。
        /// </summary>
        private static string GetManagedReferenceFullTypeName(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            return $"{type.Assembly.GetName().Name} {type.FullName}";
        }

        /// <summary>
        ///     ManagedReferenceが保持するインスタンスの型を変更する。
        /// </summary>
        private static void SetManagedReferenceType(
            SerializedProperty property,
            Type type)
        {
            property.managedReferenceValue =
                type == null
                    ? null
                    : Activator.CreateInstance(type);
        }

        /// <summary>
        ///     SubclassSelectorで使用する型情報。
        /// </summary>
        private sealed class TypeCacheData
        {
            public TypeCacheData(
                Type[] types,
                string[] displayNames,
                string[] fullNames)
            {
                Types = types;
                DisplayNames = displayNames;
                FullNames = fullNames;
            }

            /// <summary> 選択可能な型一覧です。 </summary>
            public Type[] Types { get; }

            /// <summary> Popupに表示する型名一覧です。 </summary>
            public string[] DisplayNames { get; }

            /// <summary> ManagedReference用の完全な型名一覧です。 </summary>
            public string[] FullNames { get; }
        }
    }
}

#endif