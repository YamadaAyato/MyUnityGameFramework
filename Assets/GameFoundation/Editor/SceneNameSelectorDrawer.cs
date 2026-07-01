using GameFoundation.Runtime.Attributers;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Editor
{
    /// <summary> SceneNameSelectorAttribute 用の Inspector 描画制御。 </summary>
    [CustomPropertyDrawer(typeof(SceneNameSelectorAttribute))]
    public class SceneNameSelectorDrawer : PropertyDrawer
    {
        private const string NONE_OPTION = "<None>";
        private const string MISSING_PREFIX = "<Missing> ";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // SceneNameSelectorAttribute は string 型にのみ使用できるため、型が違う場合はエラーを表示する。
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(
                    position,
                    $"{nameof(SceneNameSelectorAttribute)} は string 型にのみ使用できます。",
                    MessageType.Error);

                EditorGUI.EndProperty();
                return;
            }

            // SceneNameSelectorAttribute の IncludeDisabledScenes プロパティを取得する。
            SceneNameSelectorAttribute sceneNameAttribute = (SceneNameSelectorAttribute)attribute;
            List<string> scenePaths = GetScenePaths(sceneNameAttribute.IncludeDisabledScenes);

            // Build Settings にシーンが登録されていない場合は警告を表示する。
            if (scenePaths.Count <= 0)
            {
                EditorGUI.HelpBox(
                    position,
                    "Build Settings に対象のシーンが登録されていません。",
                    MessageType.Warning);

                EditorGUI.EndProperty();
                return;
            }

            DrawScenePopup(position, property, label, scenePaths);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            return EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        ///     Build Settings からシーンパス一覧を取得する。
        /// </summary>
        private static List<string> GetScenePaths(bool includeDisabledScenes)
        {
            List<string> scenePaths = new List<string>();

            // Build Settings に登録されているシーンを取得する。
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!includeDisabledScenes && !scene.enabled)
                {
                    continue;
                }

                // シーンパスが空の場合はスキップする。
                if (string.IsNullOrEmpty(scene.path))
                {
                    continue;
                }

                // 重複するシーンパスはスキップする。
                if (scenePaths.Contains(scene.path))
                {
                    continue;
                }

                // シーンパスを追加する。
                // シーン名だけでなく、パスも表示することで、同名のシーンが複数存在する場合に区別できるようにする。
                scenePaths.Add(scene.path);
            }

            return scenePaths;
        }

        /// <summary>
        ///     シーンパス選択用のプルダウンを描画する。
        /// </summary>
        private static void DrawScenePopup(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            List<string> scenePaths)
        {
            // プルダウンの表示用と値用のリストを作成する。
            List<string> displayOptions = new List<string>();
            List<string> valueOptions = new List<string>();

            displayOptions.Add(NONE_OPTION);
            valueOptions.Add(string.Empty);

            // Build Settings に登録されているシーンをプルダウンに追加する。
            foreach (string scenePath in scenePaths)
            {
                string displayName = CreateDisplayName(scenePath, scenePaths);

                displayOptions.Add(displayName);
                valueOptions.Add(scenePath);
            }

            // 現在の値が Build Settings に登録されていない場合は、Missing として表示する。
            int selectedIndex = valueOptions.IndexOf(property.stringValue);

            if (selectedIndex < 0)
            {
                displayOptions.Insert(0, $"{MISSING_PREFIX}{property.stringValue}");
                valueOptions.Insert(0, property.stringValue);
                selectedIndex = 0;
            }

            // 変更があった場合に値を更新するためのチェックを開始する。
            EditorGUI.BeginChangeCheck();

            int newSelectedIndex = EditorGUI.Popup(
                position,
                label.text,
                selectedIndex,
                displayOptions.ToArray());

            // 選択が変更された場合は、プロパティの値を更新する。
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = valueOptions[newSelectedIndex];
            }
        }

        /// <summary>
        ///     シーンパスからプルダウン表示名を作成する。
        /// </summary>
        private static string CreateDisplayName(string scenePath, List<string> scenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (HasSameSceneName(sceneName, scenePath, scenePaths))
            {
                return CreateDuplicateDisplayName(sceneName, scenePath, scenePaths);
            }

            return sceneName;
        }

        /// <summary>
        ///     同名シーン用のプルダウン表示名を作成する。
        /// </summary>
        private static string CreateDuplicateDisplayName(string sceneName, string currentScenePath, List<string> scenePaths)
        {
            string parentFolderName = Path.GetFileName(Path.GetDirectoryName(currentScenePath));

            if (!HasSameDuplicateDisplayName(sceneName, parentFolderName, currentScenePath, scenePaths))
            {
                return $"{sceneName} [{parentFolderName}]";
            }

            return $"{sceneName} [{ToMenuSafePath(currentScenePath)}]";
        }

        /// <summary>
        ///     同じシーン名を持つ別のシーンパスが存在するか調べる。
        /// </summary>
        private static bool HasSameSceneName(string sceneName, string currentScenePath, List<string> scenePaths)
        {
            foreach (string scenePath in scenePaths)
            {
                if (scenePath == currentScenePath)
                {
                    continue;
                }

                string otherSceneName = Path.GetFileNameWithoutExtension(scenePath);

                if (otherSceneName == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     同じシーン名と同じ親フォルダ名を持つ別のシーンパスが存在するか調べる。
        /// </summary>
        private static bool HasSameDuplicateDisplayName(
            string sceneName,
            string parentFolderName,
            string currentScenePath,
            List<string> scenePaths)
        {
            foreach (string scenePath in scenePaths)
            {
                if (scenePath == currentScenePath)
                {
                    continue;
                }

                string otherSceneName = Path.GetFileNameWithoutExtension(scenePath);
                string otherParentFolderName = Path.GetFileName(Path.GetDirectoryName(scenePath));

                if (otherSceneName == sceneName && otherParentFolderName == parentFolderName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Unity の Popup で階層扱いされないように、シーンパスを表示用に変換する。
        /// </summary>
        private static string ToMenuSafePath(string scenePath)
        {
            return scenePath.Replace("/", " > ");
        }
    }
}