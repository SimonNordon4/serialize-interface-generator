using UnityEngine;

namespace SerializeInterface.Samples
{
    public class FooMonoBehaviour : MonoBehaviour, IFoo
    {
        [SerializeField] private int fooValue;
        public void PrintFooValue()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has FooValue {fooValue}",this);
        }
    }
}