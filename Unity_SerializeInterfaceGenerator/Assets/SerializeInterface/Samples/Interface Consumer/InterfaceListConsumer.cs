using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        private readonly List<IFoo> _fooList = new();
        private readonly List<IBar> _barList = new();

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
            Debug.Log("Deserializing");
            
            _fooList?.Clear(); // Clear list before repopulating
            _barList?.Clear(); // Clear list before repopulating
            
            foreach (var foo in fooListSerialized)
            {
                // Cast and check for nullity before adding
                if (foo is IFoo deserializedFoo)
                {
                    _fooList.Add(deserializedFoo);
                }
            }

            foreach (var bar in barListSerialized)
            {
                // Cast and check for nullity before adding
                IBar deserializedBar = bar as IBar;
                if (deserializedBar != null)
                {
                    _barList.Add(deserializedBar);
                }
            }
        }
    }
}