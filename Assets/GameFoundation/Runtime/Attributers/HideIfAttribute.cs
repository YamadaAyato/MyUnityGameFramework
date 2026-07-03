using UnityEngine;

namespace GameFoundation.Runtime.Attributers
{
    /// <summary>
    ///     指定した bool 条件が true のとき Inspector から非表示にする属性。
    ///     条件名にはタイプミス防止のため、 nameof() 演算子を使用することを推奨します。
    /// </summary>
    public sealed class HideIfAttribute : PropertyAttribute
    {
        /// <summary> 条件として参照する bool メンバー名です。 </summary>
        public string ConditionName { get; }

        /// <summary>
        ///     HideIfAttribute を生成する。
        /// </summary>
        public HideIfAttribute(string conditionName)
        {
            ConditionName = conditionName;
        }
    }
}