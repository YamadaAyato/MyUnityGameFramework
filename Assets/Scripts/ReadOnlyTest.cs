using UnityEngine;

public class ReadOnlyTest : MonoBehaviour
{
    [SerializeField, ReadOnly] private int _readOnlyInt = 42;
    [SerializeField] private Color _color;
}
