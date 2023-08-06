using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        [SerializeInterface] private readonly List<IFoo> _fooList = new();
        [SerializeInterface] private List<IBar> _barList;
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
}