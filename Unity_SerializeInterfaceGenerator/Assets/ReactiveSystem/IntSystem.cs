    using System.Collections.Generic;
    using UniRx;
    using UnityEngine;

    public partial class IntSystem : ReactiveSystem<int>, IInt
    {
        public IReadOnlyReactiveProperty<int> Value => property;
    }

    // public partial class IntSystem : ISerializationCallbackReceiver
    // {
    //     [SerializeField]private List<UnityEngine.Object> _handlersSerialized = new();
    //     public void OnBeforeSerialize()
    //     {
    //     }
    //
    //     public void OnAfterDeserialize()
    //     {
    //         foreach (var obj in _handlersSerialized)
    //         {
    //             _handlers.Add(_handlersSerialized as IHandler<int>);
    //         }
    //     }
    // }

    public interface IInt
    {
        public IReadOnlyReactiveProperty<int> Value { get; }
    }
