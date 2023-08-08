namespace SerializeInterface.ReactiveSystem
{
    public class SpeedSystem : ReactiveSystem<int>, ISpeed
    {
        public int Speed => Value;
        

        private int x;
    }

    public interface ISpeed
    {
        public int Speed { get; }
    }
}