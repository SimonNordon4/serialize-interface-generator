using SerializeInterface.Samples;
using UnityEngine;

    public partial class PrefabInterfaceConsumer : MonoBehaviour
    {
        //[SerializeInterface]
        private IFoo _fooPrefab;
        //[SerializeInterface]
        private IBar _barPrefab;
        private void Start()
        {
            // You can choose to instantiate an interface the traditional way,
            // but you will need to check if it's a mono behaviour first.
            if (_fooPrefab is MonoBehaviour fooMono)
            {
                var foo = Instantiate(fooMono) as IFoo;
                foo.PrintFooValue();
            }
        }
    }
