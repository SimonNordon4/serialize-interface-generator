using UnityEngine;

namespace SerializeInterface.Samples
{
    public class GenericStringMonoBehaviour : MonoBehaviour, IGeneric<string>
    {
        [field: SerializeField]
        public string Value { get; set; }
        public void Print()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has Generic Value {Value}",this);
        }
    }
}