using UnityEngine;
using UnityEngine.UI;

namespace SerializeInterface.Samples
{
    public class UIDebugLogger : MonoBehaviour
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
    }
}