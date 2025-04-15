using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public class BarScriptableObject : ScriptableObject, IBar
    {
        [field: SerializeField]
        public string BarMessage { get; private set; }
        public void PrintBarMessage()
        {
            Debug.Log($"{name} ScriptableObject of Type {GetType().UnderlyingSystemType.Name} has BarMessage {BarMessage}",this);
        }
    }
}