using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        private List<IFoo> _fooList;

        private void Start()
        {
            foreach (var foo in _fooList)
            {
                foo.PrintFooValue();
            }
        }
    }

    public partial class InterfaceListConsumer : ISerializationCallbackReceiver
    {
        [SerializeField,ValidateInterface(typeof(IFoo))]private List<UnityEngine.Object> fooListSerialized;
        [SerializeField,ValidateInterface(typeof(IBar))]private List<UnityEngine.Object> barListSerialized;
        

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }


        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if(_fooList == null) _fooList = new List<IFoo>();
            else _fooList.Clear();
            
            foreach (var foo in fooListSerialized)
                _fooList.Add(foo as IFoo);
   
        }
    }
}