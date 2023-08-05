using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeInterface.Samples
{
    public partial class InterfaceListConsumer : MonoBehaviour
    {
        [SerializeInterface] private List<IFoo> _fooList = new();
        [SerializeInterface] private List<IBar> _barList = new();
        [SerializeField]private List<Object> objectList = new();
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