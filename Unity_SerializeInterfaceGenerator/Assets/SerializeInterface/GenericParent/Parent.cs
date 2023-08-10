using SerializeInterface.Samples;
using UnityEngine;

namespace SerializeInterface.GenericParent
{
    public class Parent<T> : MonoBehaviour
    {
        [SerializeInterface] protected IGeneric<T> m_Value;
    }
}