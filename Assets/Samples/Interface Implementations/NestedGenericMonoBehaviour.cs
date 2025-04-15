using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class NestedGenericMonoBehaviour : MonoBehaviour, IGeneric<IGeneric<int>>
    {
        [SerializeInterface] private IGeneric<int> _value;
        public IGeneric<int> Value
        {
            get => _value;
            set => _value = value;
        }

        public void Print()
        {
            Debug.Log($"{name} with component {GetType().UnderlyingSystemType.Name} has Generic Value {Value}",this);
        }
    }
}