using System;

namespace ClrVpin.Models.Shared;

public class EnumOption<TEnum> : Option where TEnum: Enum
{
    public TEnum Enum { get; init; }
}