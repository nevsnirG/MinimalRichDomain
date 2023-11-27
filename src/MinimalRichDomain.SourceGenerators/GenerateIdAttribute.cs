using System;

namespace MinimalRichDomain.SourceGenerators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateIdAttribute : Attribute { }
}