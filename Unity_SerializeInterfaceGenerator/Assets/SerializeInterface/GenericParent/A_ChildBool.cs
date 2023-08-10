using UnityEngine;

namespace SerializeInterface.GenericParent
{
    public partial class A_ChildBool : Parent<bool>
    {
        
    }
    
    public partial class A_ChildBool : Parent<bool>, ISerializationCallbackReceiver
    {
        [SerializeField, ValidateInterface(typeof(SerializeInterface.Samples.IGeneric<System.Boolean>))] 
        private UnityEngine.Object m_ValueSerialized;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_Value = m_ValueSerialized as SerializeInterface.Samples.IGeneric<System.Boolean>;
        }
    }
}