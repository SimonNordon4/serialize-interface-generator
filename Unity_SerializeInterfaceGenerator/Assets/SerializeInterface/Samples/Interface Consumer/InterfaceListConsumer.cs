using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        [SerializeInterface] private List<IFoo> _fooList = new List<IFoo>();

        [SerializeInterface][Tooltip("This is a list")] private List<IBar> _barList = new List<IBar>();
        
        [SerializeField] private List<Object> _obj;
        private void Start()
        {
            foreach (var foo in _fooList)
            {
                foo.PrintFooValue();
            }
            
            foreach (var bar in _barList)
            {
                bar.PrintBarMessage();
            }
        }
    }

    // public partial class InterfaceListConsumer : ISerializationCallbackReceiver
    // {
    //     [SerializeField,ValidateInterface(typeof(IFoo))]private List<UnityEngine.Object> fooListSerialized;
    //     [SerializeField,ValidateInterface(typeof(IBar))]private List<UnityEngine.Object> barListSerialized;
    //     
    //
    //     void ISerializationCallbackReceiver.OnBeforeSerialize()
    //     {
    //     }
    //
    //
    //     void ISerializationCallbackReceiver.OnAfterDeserialize()
    //     {
    //         if(_fooList == null) _fooList = new List<IFoo>();
    //         else _fooList.Clear();
    //         
    //         foreach (var foo in fooListSerialized)
    //             _fooList.Add(foo as IFoo);
    //
    //     }
    // }
}