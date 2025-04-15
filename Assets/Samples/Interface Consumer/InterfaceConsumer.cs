using System;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceConsumer : MonoBehaviour
    {
        [SerializeInterface] private IFoo _foo;
        [SerializeInterface] private IBar _bar;
        //Test
        
        private void Start()
        { 
            _foo.PrintFooValue();
            _bar.PrintBarMessage();
        }
    }
}