using UnityEngine;

namespace SerializeInterface.Samples
{
    public class BarMonoBehaviour : MonoBehaviour, IBar
    {

        [SerializeField] private string barMessage;
        public void PrintBarMessage()
        {
            Debug.Log($"{gameObject.name} with component {GetType().UnderlyingSystemType.Name} has BarMessage {barMessage}",this);
        }
    }
}