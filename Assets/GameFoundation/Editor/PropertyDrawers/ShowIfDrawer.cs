using GameFoundation.Runtime.Attributers;
using UnityEditor;

namespace GameFoundation.Editor.PropertyDrawers
{
    /// <summary>
    ///     ShowIfAttribute 用の Inspector 表示制御。
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public sealed class ShowIfDrawer : ConditionalVisibilityDrawerBase
    {
        /// <summary>
        ///     条件として参照するメンバー名を取得する。
        /// </summary>
        protected override string GetConditionName()
        {
            ShowIfAttribute showIfAttribute = (ShowIfAttribute)attribute;
            return showIfAttribute.ConditionName;
        }

        /// <summary>
        ///     条件値が true のとき対象プロパティを表示するか。
        /// </summary>
        protected override bool ShouldShowWhenConditionIsTrue()
        {
            return true;
        }
    }
}