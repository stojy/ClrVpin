using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyDetails
{
    public FuzzyNameDetails TableDetails { get; set; }
    public FuzzyNameDetails DescriptionDetails { get; set; }

    public static void Init(LocalGame localGame)
    {
        localGame.Fuzzy.TableDetails = Fuzzy.GetNameDetails(localGame.Game.Name, false);
        localGame.Fuzzy.DescriptionDetails = Fuzzy.GetNameDetails(localGame.Game.Description, false);
    }
}