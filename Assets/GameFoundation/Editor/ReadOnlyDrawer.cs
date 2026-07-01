using GameFoundation.Runtime.Attributers;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Editor
{
    /// <summary> ReadOnlyAttribute 用の Inspector 描画制御。 </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        private static readonly Color BackgroundColor = new Color(1f, 1f, 1f, 0.3f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Prefab override などの表示状態を正しく扱うために開始する。
            EditorGUI.BeginProperty(position, label, property);

            DrawBackground(position);

            // 指定した範囲だけGUIを無効化する。
            // 入力だけを無効化して、値を書き換えられないようにする。
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 配列やクラスなど、複数行になるプロパティにも対応する。
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary> 読み取り専用であることが分かるように薄い背景を描画する。 </summary>
        private static void DrawBackground(Rect position)
        {
            Rect backgroundRect = new Rect(
                position.x,
                position.y + 1f,
                position.width,
                position.height - 2f);

            EditorGUI.DrawRect(backgroundRect, BackgroundColor);
        }
    }
}