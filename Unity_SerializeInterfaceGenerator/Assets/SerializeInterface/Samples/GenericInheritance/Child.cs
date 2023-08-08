namespace SerializeInterface.Samples.GenericInheritance
{
    public class Child : Parent<int>
    {
        [SerializeInterface] private IFoo m_TestFoo;
    }
}