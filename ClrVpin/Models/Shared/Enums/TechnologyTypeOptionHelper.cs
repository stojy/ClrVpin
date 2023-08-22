using System;
using System.Collections.Generic;
using System.Linq;

namespace ClrVpin.Models.Shared.Enums;

public static class TechnologyTypeOptionHelper
{
    public static TechnologyTypeOptionEnum? GetEnum(string stringType)
    {
        _typeDictionary ??= Enum.GetValues<TechnologyTypeOptionEnum>().ToDictionary(value => value.ToString().ToLower(), value => value);

        return stringType != null && _typeDictionary.TryGetValue(stringType.ToLower(), out var technologyType) ? technologyType : null;
    }

    private static Dictionary<string, TechnologyTypeOptionEnum> _typeDictionary;
}