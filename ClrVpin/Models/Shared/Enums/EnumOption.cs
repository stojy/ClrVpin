using System;

namespace ClrVpin.Models.Shared.Enums;

public class EnumOption<TEnum> : Option where TEnum: Enum
{
    public TEnum Enum { get; init; }
}