using UnityEngine;

namespace SerializeInterface.Samples
{
    public class GenericIntMonoBehaviour : MonoBehaviour, IGeneric<int>
    {
        [field: SerializeField]
        public int Value { get; set; }
        
        public void Print()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has Generic Value {Value}",this);
        }
    }
}