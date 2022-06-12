using System;

namespace ClrVpin.Models.Shared.Database
{
    public class GameDerived
    {
        public int Number { get; private set; }

        public string Ipdb { get; private set; }

        public string IpdbUrl { get; private set; }

        public string NameLowerCase { get; private set; }

        public string DescriptionLowerCase { get; private set; }

        public bool IsOriginal { get; private set; }

        public string TableFileWithExtension { get; private set; }


        public static void Init(Game game, int? number = null)
        {
            var derived = game.Derived;

            derived.Number = number ?? derived.Number;

            derived.IsOriginal = CheckIsOriginal(game.Manufacturer);

            if (derived.IsOriginal)
            {
                derived.Ipdb = null;
                derived.IpdbUrl = null;
                //derived.IpdbNr = null;

                // don't assign null as this will result in the tag being removed from serialization.. which is valid, but inconsistent with the original xml file that always defines <ipdbid>
                //derived.IpdbId = "";
            }
            else
            {
                derived.Ipdb = game.IpdbId ?? game.IpdbNr ?? derived.Ipdb;
                derived.IpdbUrl = derived.Ipdb == null ? null : $"https://www.ipdb.org/machine.cgi?id={derived.Ipdb}";
            }

            // memory optimisation to perform this operation once on database read instead of multiple times during fuzzy comparison (refer Fuzzy.GetUniqueMatch)
            derived.NameLowerCase = game.Name.ToLower();
            derived.DescriptionLowerCase = game.Description.ToLower();

            derived.TableFileWithExtension = game.Name + ".vpx";
        }

        // assign isOriginal based on the manufacturer
        public static bool CheckIsOriginal(string manufacturer) => manufacturer?.StartsWith("Original", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                                   manufacturer?.StartsWith("Zen Studios", StringComparison.InvariantCultureIgnoreCase) == true;
    }
}