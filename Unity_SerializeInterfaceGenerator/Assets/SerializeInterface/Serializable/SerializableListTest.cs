using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Serializable
{
    public partial class SerializableListTest : MonoBehaviour
    {
        public List<ITest> TestList;
    }
    
    public partial class SerializableListTest : ISerializationCallbackReceiver
    {
        [SerializeReference, DrawInterface] public List<ITest> testListSerialized;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            TestList = testListSerialized;
        }
    }
}