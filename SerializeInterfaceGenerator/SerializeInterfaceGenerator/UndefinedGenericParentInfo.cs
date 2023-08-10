using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct UndefinedGenericParentInfo
    {
        public readonly string FieldName;
        public readonly string GenericFullName;
        
        public UndefinedGenericParentInfo(string fieldName, string genericFullName)
        {
            this.FieldName = fieldName;
            this.GenericFullName = genericFullName;
        }

        public override string ToString()
        {
            return
                $"Generic Identifier: {FieldName}\n " +
                $"Generic Value Full Name: {GenericFullName}";
        }
    }
}