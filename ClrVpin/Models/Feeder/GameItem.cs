using System;
using System.Linq;
using System.Windows.Input;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;

namespace ClrVpin.Models.Feeder;

// a common access class to harmonize access to the underlying OnlineGame or LocalGame properties
[AddINotifyPropertyChangedInterface]
public class GameItem
{
    public GameItem(OnlineGame onlineGame)
    {
        OnlineGame = onlineGame;
        Update(onlineGame.Hit?.LocalGame);
    }

    public GameItem(LocalGame localLocalGame)
    {
        Update(localLocalGame);
    }

    public int Index { get; set; } // 1 based index of every game in the list
    
    public string Name => Names.FirstOrDefault(x => x != null);
    public string[] Names => new[] { OnlineGame?.Name, LocalGame?.Fuzzy.TableDetails?.ActualNameWithoutManufacturerOrYear }; // for unmatched, default to the fuzzy split table name to avoid manufacturer/year info

    public string Manufacturer => Manufacturers.FirstOrDefault(x => x != null);
    public string[] Manufacturers => new[] { OnlineGame?.Manufacturer, LocalGame?.Game.Manufacturer };

    public string Year => Years.FirstOrDefault(x => x != null);
    public string[] Years => new[] { OnlineGame?.YearString, LocalGame?.Game.Year };

    public string Type => Types.FirstOrDefault(x => x != null);
    public string[] Types => new[] { OnlineGame?.Type, LocalGame?.Game.Type };

    public string[] Themes => new[] { string.Join(", ", OnlineGame?.Themes ?? new[] { "" }), LocalGame?.Game.Theme };
    public string[] Players => new[] { OnlineGame?.Players.ToString(), LocalGame?.Game.Players };
    public string[] Roms => new[] { OnlineGame?.RomFiles.FirstOrDefault()?.Name, LocalGame?.Game.Rom };
    public string[] Authors => new[] { string.Join(", ", OnlineGame?.TableFiles.FirstOrDefault()?.Authors ?? new [] {""}), LocalGame?.Game.Author };

    // online feed doesn't contain a rating (yet)
    public double? Rating => LocalGame?.Game.Rating;

    public DateTime? UpdatedAt => OnlineGame?.UpdatedAt ?? LocalGame?.Content.UpdatedAt;
    public bool IsOriginal => OnlineGame?.IsOriginal ?? LocalGame.Derived.IsOriginal;
    public TableStyleOptionEnum TableStyleOption => OnlineGame?.TableStyleOption ?? LocalGame.Derived.TableStyleOption;

    public LocalGame LocalGame { get; private set; }
    public OnlineGame OnlineGame { get; }
   
    public TableMatchOptionEnum TableMatchType { get; private set; }

    // view model properties
    public bool IsMatchingEnabled { get; set; }

    public ICommand UpdateDatabaseEntryCommand { get; set; }
    public ICommand CreateDatabaseEntryCommand { get; set; }

    public string UpdateDatabaseMatchedEntryTooltip { get; set; } = "Update matched table";
    public string UpdateDatabaseUnmatchedEntryTooltip { get; set; } = "Update unmatched table";
    public string CreateDatabaseEntryTooltip { get; set; } = "Add missing table";

    public void Update(LocalGame localGame)
    {
        LocalGame = localGame;
        
        TableMatchType = GetTableMatchEnum();
    }

    private TableMatchOptionEnum GetTableMatchEnum()
    {
        if (OnlineGame != null && LocalGame != null)
            return TableMatchOptionEnum.LocalAndOnline;
        if (OnlineGame != null)
            return TableMatchOptionEnum.OnlineOnly;
        return TableMatchOptionEnum.LocalOnly;
    }
}