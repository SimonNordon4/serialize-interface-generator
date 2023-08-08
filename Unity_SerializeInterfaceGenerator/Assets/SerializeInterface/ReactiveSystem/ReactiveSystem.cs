using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ReactiveSystem<T> : MonoBehaviour
{
    protected T Value;
    [SerializeInterface]private List<IHandler<T>> m_Handlers = new List<IHandler<T>>();
}

public interface IHandler<in T>
{
    void Handle(T value);
}
