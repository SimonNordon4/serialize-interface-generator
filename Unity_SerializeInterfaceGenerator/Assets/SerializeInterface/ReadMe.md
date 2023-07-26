# Serialize Interface

___

This package will allow you to add the Serialize Interface Attribute to your MonoBehaviour classes, granting the ability
to drag and drop MonoBehaviours and Scriptable Objects that implement the desired interface into the editor window, just like
a normal Serialized Field.

```csharp
public partial class Example : MonoBehaviour 
{
    [SerializeInterface]private IInterface _interface;
}
```

It accepts MonoBehaviours, ScriptableObjects and Prefabs so long as they implement the target Interface.
___
### Prefabs

This package also adds helper functions for Instantiating Interfaces (so long as their concrete class is a MonoBehaviour)

```csharp
public partial class Example : MonoBehaviour 
{
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

