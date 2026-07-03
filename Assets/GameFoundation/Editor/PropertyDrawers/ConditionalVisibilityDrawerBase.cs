using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Editor.PropertyDrawers
{
    /// <summary>
    ///     bool 条件による Inspector 表示制御 Drawer の基底クラス。
    /// </summary>
    public abstract class ConditionalVisibilityDrawerBase : PropertyDrawer
    {
        private const BindingFlags MEMBER_BINDING_FLAGS =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly Dictionary<string, MemberInfo> _memberInfoCache = new();

        /// <summary>
        ///     条件として参照するメンバー名を取得する。
        /// </summary>
        protected abstract string GetConditionName();

        /// <summary>
        ///     条件値が true のとき対象プロパティを表示するか。
        /// </summary>
        protected abstract bool ShouldShowWhenConditionIsTrue();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetConditionValue(property, GetConditionName(), out bool conditionValue, out string errorMessage))
            {
                EditorGUI.HelpBox(position, errorMessage, MessageType.Error);
                return;
            }

            if (!ShouldShowProperty(conditionValue))
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!TryGetConditionValue(property, GetConditionName(), out bool conditionValue, out _))
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            if (!ShouldShowProperty(conditionValue))
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>
        ///     条件値から対象プロパティを表示するか判定する。
        /// </summary>
        private bool ShouldShowProperty(bool conditionValue)
        {
            if (ShouldShowWhenConditionIsTrue())
            {
                return conditionValue;
            }

            return !conditionValue;
        }

        /// <summary>
        ///     条件値を取得する。
        /// </summary>
        private static bool TryGetConditionValue(
            SerializedProperty property,
            string conditionName,
            out bool conditionValue,
            out string errorMessage)
        {
            conditionValue = false;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(conditionName))
            {
                errorMessage = "条件名が空です。";
                return false;
            }

            SerializedProperty conditionProperty = FindConditionSerializedProperty(property, conditionName);

            if (conditionProperty != null)
            {
                if (conditionProperty.propertyType != SerializedPropertyType.Boolean)
                {
                    errorMessage = $"条件 '{conditionName}' は bool 型である必要があります。";
                    return false;
                }

                conditionValue = conditionProperty.boolValue;
                return true;
            }

            object targetObject = GetParentObject(property);

            if (targetObject == null)
            {
                errorMessage = $"条件 '{conditionName}' を取得できませんでした。";
                return false;
            }

            if (TryGetBoolByReflection(targetObject, conditionName, out conditionValue, out errorMessage))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     対象プロパティと同じ階層から SerializedProperty を探す。
        /// </summary>
        private static SerializedProperty FindConditionSerializedProperty(
            SerializedProperty property,
            string conditionName)
        {
            string propertyPath = property.propertyPath;
            int lastDotIndex = propertyPath.LastIndexOf('.');

            string conditionPath = lastDotIndex >= 0
                ? $"{propertyPath[..(lastDotIndex + 1)]}{conditionName}"
                : conditionName;

            return property.serializedObject.FindProperty(conditionPath);
        }

        /// <summary>
        ///     対象プロパティの親オブジェクトを取得する。
        /// </summary>
        private static object GetParentObject(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;

            string propertyPath = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = propertyPath.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                targetObject = GetObjectValue(targetObject, elements[i]);

                if (targetObject == null)
                {
                    return null;
                }
            }

            return targetObject;
        }

        /// <summary>
        ///     指定したメンバー名からオブジェクトの値を取得する。
        /// </summary>
        private static object GetObjectValue(object source, string memberName)
        {
            if (source == null)
            {
                return null;
            }

            int bracketIndex = memberName.IndexOf('[');

            if (bracketIndex >= 0)
            {
                string collectionName = memberName[..bracketIndex];
                int startIndex = memberName.IndexOf('[', bracketIndex) + 1;
                int endIndex = memberName.IndexOf(']', startIndex);
                string indexText = memberName[startIndex..endIndex];

                if (!int.TryParse(indexText, out int index))
                {
                    return null;
                }

                object collectionObject = GetObjectValue(source, collectionName);

                if (collectionObject is not IEnumerable collection)
                {
                    return null;
                }

                IEnumerator enumerator = collection.GetEnumerator();

                for (int i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext())
                    {
                        return null;
                    }
                }

                return enumerator.Current;
            }

            Type type = source.GetType();

            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(memberName, MEMBER_BINDING_FLAGS);

                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(source);
                }

                PropertyInfo propertyInfo = type.GetProperty(memberName, MEMBER_BINDING_FLAGS);

                if (propertyInfo != null)
                {
                    return propertyInfo.GetValue(source);
                }

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        ///     Reflection を使って bool 値を取得する。
        /// </summary>
        private static bool TryGetBoolByReflection(
            object targetObject,
            string conditionName,
            out bool conditionValue,
            out string errorMessage)
        {
            conditionValue = false;
            errorMessage = string.Empty;

            Type targetType = targetObject.GetType();
            string cacheKey = $"{targetType.FullName}.{conditionName}";

            if (!_memberInfoCache.TryGetValue(cacheKey, out MemberInfo memberInfo))
            {
                memberInfo = FindMemberInfo(targetType, conditionName);

                if (memberInfo != null)
                {
                    _memberInfoCache.Add(cacheKey, memberInfo);
                }
            }

            if (memberInfo == null)
            {
                errorMessage = $"条件 '{conditionName}' が見つかりません。";
                return false;
            }

            object value = GetMemberValue(memberInfo, targetObject);

            if (value is bool boolValue)
            {
                conditionValue = boolValue;
                return true;
            }

            errorMessage = $"条件 '{conditionName}' は bool 型である必要があります。";
            return false;
        }

        /// <summary>
        ///     指定した型からメンバー情報を探す。
        /// </summary>
        private static MemberInfo FindMemberInfo(Type targetType, string memberName)
        {
            Type currentType = targetType;

            while (currentType != null)
            {
                FieldInfo fieldInfo = currentType.GetField(memberName, MEMBER_BINDING_FLAGS);

                if (fieldInfo != null)
                {
                    return fieldInfo;
                }

                PropertyInfo propertyInfo = currentType.GetProperty(memberName, MEMBER_BINDING_FLAGS);

                if (propertyInfo != null)
                {
                    return propertyInfo;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        ///     メンバー情報から値を取得する。
        /// </summary>
        private static object GetMemberValue(MemberInfo memberInfo, object targetObject)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(targetObject);
            }

            if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(targetObject);
            }

            return null;
        }
    }
}