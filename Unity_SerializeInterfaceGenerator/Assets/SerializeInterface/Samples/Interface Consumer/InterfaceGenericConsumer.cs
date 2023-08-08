using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

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

        [SerializeInterface] private IGeneric<IGeneric<int>> _nestedGeneric;
    }
}