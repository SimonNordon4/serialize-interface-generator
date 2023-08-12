using System;
using UnityEngine;

namespace SerializeInterface.Serializable
{
    public class DrawInterfaceAttribute : PropertyAttribute
    {
    }

    public partial class SerializableTest : MonoBehaviour
    {
        public int testInt;
        
        public ITest Test;

        private void Start()
        {
            Test.Test();
        }
    }

    public partial class SerializableTest : ISerializationCallbackReceiver
    {
        [SerializeReference,DrawInterface] public ITest TestSerialized = new TestA();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Test = TestSerialized;
        }
    }
    
    public interface ITest
    {
        void Test();
    }
    
    [Serializable]
    public class TestA : ITest
    {
        public int testInt = 1;
        public void Test()
        {
            Debug.Log("TestA");
        }
    }
}