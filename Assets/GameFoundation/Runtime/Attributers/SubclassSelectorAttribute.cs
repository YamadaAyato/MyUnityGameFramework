using System;
using UnityEngine;

namespace GameFoundation.Runtime.Attributers
{
    /// <summary>
    ///     <see cref="SerializeReference"/> で保持する型を、派生型から選択可能にする属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}