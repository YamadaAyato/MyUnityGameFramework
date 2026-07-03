using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFoundation.Editor.Windows
{
    /// <summary>
    ///     プロジェクト用のフォルダ構成を選択して作成する EditorWindow。
    /// </summary>
    public sealed class FolderGeneratorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Folder Generator";
        private const string DEFAULT_ROOT_PATH = "Assets";

        private string _rootFolderName = string.Empty;
        private bool _selectAll = true;
        private Vector2 _scrollPosition;

        // ここにフォルダ構成を定義する。
        // フォルダ名の変更や、子フォルダの追加・削除を行う場合はこのリストを編集する。
        private readonly List<FolderItem> _folderItems = new()
        {
            new FolderItem("Art", true, new List<FolderItem>
            {
                new FolderItem("Materials", true),
                new FolderItem("Models", true),
                new FolderItem("Textures", true),
                new FolderItem("Shaders", true),
                new FolderItem("Sprites", true),
                new FolderItem("Animation", true, new List<FolderItem>
                {
                    new FolderItem("Clips", true),
                    new FolderItem("Controllers", true),
                }),
            }),

            new FolderItem("Audio", true, new List<FolderItem>
            {
                new FolderItem("Music", true),
                new FolderItem("Sound", true),
            }),

            new FolderItem("Level", true, new List<FolderItem>
            {
                new FolderItem("Prefabs", true),
                new FolderItem("Scenes", true),
                new FolderItem("UI", true),
            }),

            new FolderItem("Scripts", true),
            new FolderItem("Docs", true),
            new FolderItem("Resources", true),
            new FolderItem("Settings", true),
            new FolderItem("Editor", true),
            new FolderItem("AssetStoreTools", true),
        };

        /// <summary>
        ///     フォルダ生成ウィンドウを開く。
        /// </summary>
        [MenuItem("Tools/Game Foundation/Folder Generator")]
        private static void Open()
        {
            FolderGeneratorWindow window = GetWindow<FolderGeneratorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawRootFolderNameField();
            DrawSelectAllToggle();
            DrawFolderToggles();

            EditorGUILayout.EndScrollView();
            DrawCreateButton();
        }

        /// <summary>
        ///     ヘッダーを描画する。
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField("Folder Generator", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "チェックしたフォルダだけを作成します。Name が空の場合は Assets 直下に作成し、Name を入力した場合は Assets/{Name}/ の下に作成します。",
                MessageType.Info);

            EditorGUILayout.Space();
        }

        /// <summary>
        ///     作成先ルートフォルダ名の入力欄を描画する。
        /// </summary>
        private void DrawRootFolderNameField()
        {
            EditorGUILayout.LabelField("Root", EditorStyles.boldLabel);

            _rootFolderName = EditorGUILayout.TextField("Name", _rootFolderName);

            EditorGUILayout.LabelField("Create Under", GetRootPath());

            EditorGUILayout.Space();
        }

        /// <summary>
        ///     全選択と全解除のチェック欄を描画する。
        /// </summary>
        private void DrawSelectAllToggle()
        {
            EditorGUI.BeginChangeCheck();

            bool newSelectAll = EditorGUILayout.ToggleLeft(
                "すべて選択 / 解除",
                _selectAll,
                EditorStyles.boldLabel);

            if (EditorGUI.EndChangeCheck())
            {
                _selectAll = newSelectAll;
                SetAllFolderItems(_folderItems, _selectAll);
            }

            EditorGUILayout.Space();
        }

        /// <summary>
        ///     フォルダ選択項目を描画する。
        /// </summary>
        private void DrawFolderToggles()
        {
            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);

            foreach (FolderItem folderItem in _folderItems)
            {
                DrawFolderItem(folderItem, 0, true);
            }

            EditorGUILayout.Space();
        }

        /// <summary>
        ///     フォルダ項目を階層付きで描画する。
        /// </summary>
        private static void DrawFolderItem(FolderItem folderItem, int indentLevel, bool parentEnabled)
        {
            using (new EditorGUI.DisabledScope(!parentEnabled))
            using (new EditorGUI.IndentLevelScope(indentLevel))
            {
                folderItem.IsEnabled = EditorGUILayout.ToggleLeft(folderItem.Name, folderItem.IsEnabled);
            }

            bool childrenEnabled = parentEnabled && folderItem.IsEnabled;

            foreach (FolderItem child in folderItem.Children)
            {
                DrawFolderItem(child, indentLevel + 1, childrenEnabled);
            }
        }

        /// <summary>
        ///     作成ボタンを描画する。
        /// </summary>
        private void DrawCreateButton()
        {
            if (GUILayout.Button("フォルダ生成", GUILayout.Height(32f)))
            {
                GenerateFolders();
            }
        }

        /// <summary>
        ///     選択されたフォルダを作成する。
        /// </summary>
        private void GenerateFolders()
        {
            string rootPath = GetRootPath();

            CreateFolderRecursive(rootPath);

            foreach (FolderItem folderItem in _folderItems)
            {
                CreateFolderItemRecursive(rootPath, folderItem, true);
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "フォルダ生成",
                $"{rootPath} に選択されたフォルダを生成しました。",
                "OK");
        }

        /// <summary>
        ///     フォルダ項目の選択状態に応じてフォルダを作成する。
        /// </summary>
        private static void CreateFolderItemRecursive(string parentPath, FolderItem folderItem, bool parentEnabled)
        {
            bool shouldCreateThisFolder = parentEnabled && folderItem.IsEnabled;
            bool hasEnabledChild = HasEnabledChild(folderItem);

            if (!shouldCreateThisFolder && !hasEnabledChild)
            {
                return;
            }

            string currentPath = $"{parentPath}/{folderItem.Name}";

            // 子フォルダが選択されている場合、親フォルダは自動で作成する。
            CreateFolderRecursive(currentPath);

            foreach (FolderItem child in folderItem.Children)
            {
                CreateFolderItemRecursive(currentPath, child, shouldCreateThisFolder);
            }
        }

        /// <summary>
        ///     子階層に有効なフォルダが存在するか調べる。
        /// </summary>
        private static bool HasEnabledChild(FolderItem folderItem)
        {
            foreach (FolderItem child in folderItem.Children)
            {
                if (child.IsEnabled || HasEnabledChild(child))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     すべてのフォルダ項目の選択状態を変更する。
        /// </summary>
        private static void SetAllFolderItems(List<FolderItem> folderItems, bool isEnabled)
        {
            foreach (FolderItem folderItem in folderItems)
            {
                folderItem.IsEnabled = isEnabled;
                SetAllFolderItems(folderItem.Children, isEnabled);
            }
        }

        /// <summary>
        ///     作成先のルートパスを取得する。
        /// </summary>
        private string GetRootPath()
        {
            if (string.IsNullOrWhiteSpace(_rootFolderName))
            {
                return DEFAULT_ROOT_PATH;
            }

            string safeFolderName = _rootFolderName.Trim();
            return $"{DEFAULT_ROOT_PATH}/{safeFolderName}";
        }

        /// <summary>
        ///     指定されたフォルダが存在しない場合、親フォルダから順番に作成する。
        /// </summary>
        private static void CreateFolderRecursive(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(path)?.Replace("\\", "/");

            if (string.IsNullOrEmpty(parentPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                CreateFolderRecursive(parentPath);
            }

            string folderName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(folderName))
            {
                return;
            }

            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        /// <summary>
        ///     フォルダ生成項目。
        /// </summary>
        private sealed class FolderItem
        {
            public string Name { get; }
            public bool IsEnabled { get; set; }
            public List<FolderItem> Children { get; }

            public FolderItem(string name, bool isEnabled)
                : this(name, isEnabled, new List<FolderItem>())
            {
            }

            public FolderItem(string name, bool isEnabled, List<FolderItem> children)
            {
                Name = name;
                IsEnabled = isEnabled;
                Children = children;
            }
        }
    }
}