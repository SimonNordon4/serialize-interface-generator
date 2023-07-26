using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestInterfaceScriptableObject", menuName = "ScriptableObjects/TestInterfaceScriptableObject")]
public class TestInterfaceScriptableObject : ScriptableObject, ITestInterface
{

}
