using SerializeInterface.Samples;
using UnityEngine;

    public partial class InterfacePrefabConsumer : MonoBehaviour
    {
        [SerializeInterface][Tooltip("This is a tooltip")]
        private IFoo _fooPrefab;
        [SerializeInterface]
        private IBar _barPrefab;
        private void Start()        
        {
            // var foo = InstantiateInterface(_fooPrefab);
            // var bar = InstantiateInterface(_barPrefab);
            //
            // foo.PrintFooValue();
            // bar.PrintBarMessage();
        }
    }
