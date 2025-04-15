using UnityEngine;

namespace SerializeInterface.Samples
{
    public class FooChildMonoBehaviour : MonoBehaviour, IFooChild
    {
        [SerializeField] private int value = 5;
        
        public void PrintFooValue()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has BarMessage {value}",this);
        }
    }
}