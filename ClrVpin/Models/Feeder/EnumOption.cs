using System;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Feeder;

// todo; move into shared
public class EnumOption<TEnum> : Option where TEnum: Enum
{
    public TEnum Enum { get; init; }
}