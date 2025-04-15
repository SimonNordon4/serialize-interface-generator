using UnityEngine;

namespace SerializeInterface.Samples
{
    public class FooScriptableObject : ScriptableObject, IFoo
    {
        [field: SerializeField]
        public int FooValue { get; private set; }
        public void PrintFooValue()
        {
            Debug.Log($"{name} ScriptableObject of Type {GetType().UnderlyingSystemType.Name} has FooValue {FooValue}",this);
        }
    }
}