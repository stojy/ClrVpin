using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyDetails
{
    public FuzzyNameDetails TableDetails { get; set; }
    public FuzzyNameDetails DescriptionDetails { get; set; }

    public static void Init(GameDetail gameDetail)
    {
        gameDetail.Fuzzy.TableDetails = Fuzzy.GetNameDetails(gameDetail.Game.Name, false);
        gameDetail.Fuzzy.DescriptionDetails = Fuzzy.GetNameDetails(gameDetail.Game.Description, false);
    }
}