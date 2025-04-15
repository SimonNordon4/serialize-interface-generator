using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceGenericConsumer : MonoBehaviour
    {
        [SerializeInterface]
        private IGeneric<int> _intGeneric;
        
        [SerializeInterface]
        private IGeneric<string> _stringGeneric;

        [SerializeInterface]
        private List<IGeneric<int>> _listGeneric;

        [SerializeInterface] 
        private IGeneric<IGeneric<int>> _nestedGeneric;

        private void Start()
        {
            Debug.Log("Int generic value: " + _intGeneric.Value);
            Debug.Log("String generic value: " + _stringGeneric.Value);
            Debug.Log("List generic value: " + _listGeneric[0].Value);
            Debug.Log("Nested generic value: " + _nestedGeneric.Value);
        }
    }
}