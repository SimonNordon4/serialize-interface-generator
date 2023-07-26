using UnityEngine;

namespace SerializeInterface.Samples
{
    public class FooMonoBehaviour : MonoBehaviour, IFoo
    {
        [field: SerializeField]
        public int FooValue { get; private set; }
        public void PrintFooValue()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has FooValue {FooValue}",this);
        }
    }
}