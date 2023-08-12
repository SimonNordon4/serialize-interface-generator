using System;
using UnityEngine;

namespace SerializeInterface.Serializable
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DrawInterfaceAttribute : PropertyAttribute
    {
    }
}