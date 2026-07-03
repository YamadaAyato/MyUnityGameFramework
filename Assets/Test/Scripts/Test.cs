using GameFoundation.Runtime.Attributers;
using UnityEngine;

/// <summary>
///     テスト用。
/// </summary>
public class Test : MonoBehaviour
{
    [SerializeField, ReadOnly] private int _readOnlyInt = 42;
    [SerializeField, ShowIf(nameof(_isDebug))] private Color _color;
    [SerializeField, SceneNameSelector] private string _sceneName;
    [SerializeField] private bool _isDebug;
}
