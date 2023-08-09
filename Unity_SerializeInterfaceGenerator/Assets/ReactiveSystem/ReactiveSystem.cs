using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class ReactiveSystem<T> : MonoBehaviour
{
    [SerializeField] protected ReactiveProperty<T> property = new();

    [SerializeInterface] private readonly List<IHandler<T>> _handlers = new List<IHandler<T>>();

    public void Awake()
    {
        foreach (var handler in _handlers)
        {
            handler.Handle(property);
        }
    }
}

public interface IHandler<T>
{
    public void Handle(ReactiveProperty<T> value);
}
