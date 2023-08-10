using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializeInterfaceGenerator
{
    public readonly struct UndefinedGenericParentInfo
    {
        public readonly string GenericIdentifier;
        public readonly string GenericValueFullName;
        public readonly string GenericFieldIdentifierName;
        
        public UndefinedGenericParentInfo(string genericIdentifier, string genericValueFullName, string genericFieldIdentifierName)
        {
            this.GenericIdentifier = genericIdentifier;
            this.GenericValueFullName = genericValueFullName;
            this.GenericFieldIdentifierName = genericFieldIdentifierName;
        }

        public override string ToString()
        {
            return
                $"Generic Identifier: {GenericIdentifier}\n " +
                $"Generic Value Full Name: {GenericValueFullName}\n " +
                $"Generic Field Identifier Name: {GenericFieldIdentifierName}";
        }
    }
}