using UnityEngine;
using UnityEngine.UI;

namespace SerializeInterface.Samples
{
    public partial class UIDebugLogger : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private Text _debugText;
        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        } 
        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        } 
        private void HandleLog(string condition, string stacktrace, LogType type)
        {
            _debugText.text += "\n" + condition;
        }
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            
        }
    }

    public partial class UIDebugLogger : MonoBehaviour, ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize()
        {
        }
        
        public void OnAfterDeserialize()
        {
            
        }
    }
}