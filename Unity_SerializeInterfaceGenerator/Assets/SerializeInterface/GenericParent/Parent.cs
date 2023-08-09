using SerializeInterface.Samples;
using UnityEngine;

namespace SerializeInterface.GenericParent
{
    public class Parent<T> : MonoBehaviour
    {
        [SerializeInterface] private IGeneric<T> m_Value;
    }
}