using GameFoundation.Runtime.Attributers;
using UnityEditor;

namespace GameFoundation.Editor.PropertyDrawers
{
    /// <summary>
    ///     HideIfAttribute 用の Inspector 表示制御。
    /// </summary>
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public sealed class HideIfDrawer : ConditionalVisibilityDrawerBase
    {
        /// <summary>
        ///     条件として参照するメンバー名を取得する。
        /// </summary>
        protected override string GetConditionName()
        {
            HideIfAttribute hideIfAttribute = (HideIfAttribute)attribute;
            return hideIfAttribute.ConditionName;
        }

        /// <summary>
        ///     条件値が true のとき対象プロパティを表示するか。
        /// </summary>
        protected override bool ShouldShowWhenConditionIsTrue()
        {
            return false;
        }
    }
}