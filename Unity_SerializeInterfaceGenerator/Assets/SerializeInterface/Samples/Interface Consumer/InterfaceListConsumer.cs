using System.Collections.Generic;
using UnityEngine;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        [SerializeInterface] private readonly List<IFoo> _fooList;
        [SerializeInterface][Tooltip("This is a list of IBar interfaces")] private List<IBar> _barList = new();
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