### Update 4
(10/08/2023)

- Serializable classes can now use [SerializeInterface].

```chsarp
[Serializable]
public partial class PlainClass
{
    [SerializeInterface] private IInterface _interface;
}
```

### Update 3
(07/08/2023)
New:
- Added support for Generic Interfaces.
- Added support for Derived Interfaces.
 

  Breaking Changes:
- Removed InstantiateInterface() helper function.
### Update 2
(05/08/2023)
Fixes:
- No longer attempts to serialize readonly fields.
- No longer attempts to serialize static fields.
- No longer attempts to serialize fields that are not interfaces.
- Attributes that don't belong in the global namespace should now work.
  New:
- Added support for Lists.
- Added support for readonly Lists so long as their value is not null.
### Update 1
(28/07/2023)
- Classes using [SerializeInterface] can now implement ISerializationCallbackReceiver.
- Other attributes used along side [SerializeInterface] should now appear in the inspector (like [Tooltip()] for example)
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