using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyDetails
{
    public FuzzyItemDetails TableDetails { get; set; }
    public FuzzyItemDetails DescriptionDetails { get; set; }

    public static void UpdateLocalGameFuzzyDetails(LocalGame localGame)
    {
        localGame.Fuzzy.TableDetails = Fuzzy.GetTableDetails(localGame.Game.Name, false);
        localGame.Fuzzy.DescriptionDetails = Fuzzy.GetTableDetails(localGame.Game.Description, false);
    }
}