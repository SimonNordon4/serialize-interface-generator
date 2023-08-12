    [SerializeInterface]private IInterface _interfacePrefab;
    
    private void Start()
    {
        var interface = InstantiateInterface(_interfacePrefab);
    }
}
```
___
### How it Works
It achieves this using a Source Generator. The above example will generate into something like this:
```csharp
public partial class Example : MonoBehaviour : ISerializeCallbackReceiver
{
    [SerializeField, ValidateInterface(typeof(IInterface))]
    private UnityEngine.Object _interface_Object;
    
    private void OnBeforeSerialize()
    {
    }
    
    private void OnAfterDeserialize()
    {
        _interface = _interface_Object as IInterface;
    }
    
    private IInterface InstantiateInterface(IInterface instance)
    {
        if(instance is MonoBehaviour instanceMono)
            return Instantiate(instanceMono) as IInterface;
            
        Debug.LogError("Cannot Instantiate instance as it does not implement IInterface");
        return null;
    }
}
``` 
The only drawback is that our MonoBehaviour has to be a partial class, as is the case with all Classes that use Source Generators.
___
### Future Plans
Ideally I would like to add support for Properties so that we can do something like this:
```csharp
public partial class Example : MonoBehaviour 
{
    [field:SerializeInterface]
    public IInterface _interface { get; private set; }
}
```
