using GameFoundation.Runtime.Attributers;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SubClassSelectorTest", menuName = "GameFoundation/SubClassSelectorTest")]
public class SubClassSelectorTest : ScriptableObject
{
    [SerializeReference, SubclassSelector] 
    private SubClassSelectorTestBase _baseField;
}

[Serializable]
public class SubClassSelectorTestBase
{
}

[Serializable]
public class SubClassSelectorTestDerived1 : SubClassSelectorTestBase
{
}

[Serializable]
public class SubClassSelectorTestDerived2 : SubClassSelectorTestBase
{
}
