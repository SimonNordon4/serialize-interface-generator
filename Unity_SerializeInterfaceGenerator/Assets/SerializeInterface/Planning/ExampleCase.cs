using SerializeInterface.Planning;
using UnityEngine;

// This
public partial class ExampleCase : MonoBehaviour
{
    [SerializeInterface] private ITestInterface _testInterface;

}

// Will generate this where ever there is a [SerializeInterface] Attribute.
public partial class ExampleCase : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField, ValidateInterface(typeof(ITestInterface))]private Object _testInterface_Object;
    
    public void OnBeforeSerialize()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        _testInterface = _testInterface_Object as ITestInterface;
    }

    public void OnAfterDeserialize()
    {
        
    }
}

