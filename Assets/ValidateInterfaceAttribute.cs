using System;
using UnityEngine;

namespace SerializeInterface
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class ValidateInterfaceAttribute : PropertyAttribute
    {
        public Type RequiredType { get; private set; }

        public ValidateInterfaceAttribute(Type requiredType)
        {
            RequiredType = requiredType;
        }
    }
}
