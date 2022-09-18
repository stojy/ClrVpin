using System;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Importer;

public class EnumOption<TEnum> : Option where TEnum: Enum
{
    public TEnum Enum { get; init; }
}