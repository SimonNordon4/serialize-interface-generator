using UnityEngine;

namespace SerializeInterface.Samples
{
    public class BarMonoBehaviour : MonoBehaviour, IBar
    {

        [field:SerializeField]
        public string BarMessage { get; private set; }
        public void PrintBarMessage()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has BarMessage {BarMessage}",this);
        }
    }
}