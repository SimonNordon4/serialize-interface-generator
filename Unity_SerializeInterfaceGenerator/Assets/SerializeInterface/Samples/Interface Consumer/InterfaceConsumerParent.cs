using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceConsumerParent<T> : MonoBehaviour
    {
        [SerializeInterface]protected IGeneric<T> _generic;
    }
}
