using System;
using ClrVpin.Models.Feeder;
using PropertyChanged;
// ReSharper disable MemberCanBePrivate.Global - public setters required to support json deserialization, refer DatabaseItem

namespace ClrVpin.Models.Shared.Game
{
    [AddINotifyPropertyChangedInterface]
    public class GameDerived
    {
        public int Number { get; set; }
        public string Ipdb { get; set; }
        public string IpdbUrl { get; set; }
        public string NameLowerCase { get; set; }
        public string NameWithoutParenthesis { get; set; }
        public string DescriptionLowerCase { get; set; }
        public bool IsOriginal { get; set; }
        public string TableFileWithExtension { get; set; }
        public TableStyleOptionEnum TableStyleOption { get; private set; }

        public static void Init(LocalGame localGame, int? number = null)
        {
            var derived = localGame.Derived;

            // update number if one is provided, else keep the existing value
            derived.Number = number ?? derived.Number;

            derived.IsOriginal = CheckIsOriginal(localGame.Game.Manufacturer, localGame.Game.Name);

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
                derived.Ipdb = localGame.Game.IpdbId ?? localGame.Game.IpdbNr ?? derived.Ipdb;
                derived.IpdbUrl = derived.Ipdb == null ? null : $"https://www.ipdb.org/machine.cgi?id={derived.Ipdb}";
            }

            derived.TableStyleOption = derived.IsOriginal ? TableStyleOptionEnum.Original : TableStyleOptionEnum.Manufactured;

            // memory optimisation to perform this operation once on database read (or update) instead of multiple times during fuzzy comparison (refer Fuzzy.GetUniqueMatch)
            // - null check to cater for scenario where the value can be null, e.g. when cleared via feeder's database update dialog
            derived.NameLowerCase = localGame.Game.Name?.ToLower();
            derived.DescriptionLowerCase = localGame.Game.Description?.ToLower();
            derived.TableFileWithExtension = localGame.Game.Name  + ".vpx";
        }

        public static bool CheckIsOriginal(string manufacturer, string name)
        {
            // determine isOriginal based on manufacturer
            var isManufacturerOriginal = manufacturer?.StartsWith("Original", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("OrbitalPin", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("HorsePin", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("Zen Studios", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("Professional Pinball", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("Cunning Developments", StringComparison.InvariantCultureIgnoreCase) == true ||
                   manufacturer?.StartsWith("Dream Pinball 3D", StringComparison.InvariantCultureIgnoreCase) == true;
            
            // determine isOriginal based on table name
            var isNameOriginal =  name?.Equals("Jurassic park - Limited Edition", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Kiss Live", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Dream Pinball 3D", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Sharpshooter", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Silver Line", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Space Cadet", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Yamanobori", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Siggi's Spider-Man Classic", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Mad Scientist", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Midnight Magic", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Pro Pinball The Web", StringComparison.InvariantCultureIgnoreCase) == true || 
                                  name?.Equals("Octopus", StringComparison.InvariantCultureIgnoreCase) == true ;

            return isManufacturerOriginal || isNameOriginal;
        }
    }
}