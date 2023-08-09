    using UniRx;
    using UnityEngine;

    public partial class IntSystem : ReactiveSystem<int>, IInt
    {
        public IReadOnlyReactiveProperty<int> Value => property;
        
        
        private int x;
    }

    public interface IInt
    {
        public IReadOnlyReactiveProperty<int> Value { get; }
    }
