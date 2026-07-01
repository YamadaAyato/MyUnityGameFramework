using GameFoundation.Runtime.Attributers;
using UnityEngine;

public class ReadOnlyTest : MonoBehaviour
{
    [SerializeField, ReadOnly] private int _readOnlyInt = 42;
    [SerializeField] private Color _color;
    [SerializeField, SceneNameSelector] private string _sceneName;
}
