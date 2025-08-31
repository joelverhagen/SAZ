namespace System.Runtime.CompilerServices;

public class ExtensionAttribute : Attribute
{
}

public class RequiredMemberAttribute : Attribute
{
}

public class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name)
    {
    }
}
