using UnityEngine;

namespace SerializeInterface.Samples.GenericInheritance
{
    public class Parent<T> : MonoBehaviour
    {
        [SerializeInterface] protected IGeneric<T> m_Generic;
    }
}