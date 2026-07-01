using UnityEngine;

namespace GameFoundation.Runtime.Attributers
{
    /// <summary> シーン名を選択するための属性。 </summary>
    public class SceneNameSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        ///     無効化されているシーンも含めるかどうか。
        /// </summary>
        public bool IncludeDisabledScenes { get; }

        /// <summary>
        ///     シーン名選択機能を生成する。
        /// </summary>
        /// <param name="includeDisabledScenes"> 無効生かされている BuildSettings のシーンも候補に入れるか。 </param>
        public SceneNameSelectorAttribute(bool includeDisabledScenes = false)
        {
            IncludeDisabledScenes = includeDisabledScenes;
        }
    }
}