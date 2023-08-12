using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class SerializeInterfaceAttribute : Attribute
{
    TargetType TargetType { get; }
    
    public SerializeInterfaceAttribute(TargetType targetType = TargetType.Object)
    {
        TargetType = targetType;
    }
}

public enum TargetType
{
    Object,
    MonoBehaviour,
    ScriptableObject,
    Serializable
}
