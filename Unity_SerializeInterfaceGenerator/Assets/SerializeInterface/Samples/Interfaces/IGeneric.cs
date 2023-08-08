namespace SerializeInterface.Samples
{
    public interface IGeneric<T>
    {
        public T Value { get; set; }
        public void Print();
    }
}