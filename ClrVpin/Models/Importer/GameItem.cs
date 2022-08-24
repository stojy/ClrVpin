using System;
using System.Windows.Input;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;

namespace ClrVpin.Models.Importer;

[AddINotifyPropertyChangedInterface]
public class GameItem
{
    public GameItem(OnlineGame onlineGame)
    {
        OnlineGame = onlineGame;
        GameDetail = onlineGame.Hit?.GameDetail;
    }

    public GameItem(GameDetail localGame)
    {
        GameDetail = localGame;
    }

    public int Index { get; set; } // 1 based index of every game in the list
    public string Name => OnlineGame != null ? OnlineGame.Name : GameDetail.Fuzzy.TableDetails.NameOriginalCase; // for unmatched, default to the fuzzy parsed table name
    public string Manufacturer => OnlineGame != null ? OnlineGame.Manufacturer : GameDetail.Game.Manufacturer;
    public string Year => OnlineGame != null ? OnlineGame.YearString : GameDetail.Game.Year;
    public string Type => OnlineGame != null ? OnlineGame.Type : GameDetail.Game.Type;
    public DateTime? UpdatedAt => OnlineGame?.UpdatedAt; // not supported in local DB GameDetail
    public bool IsOriginal => OnlineGame?.IsOriginal ?? GameDetail.Derived.IsOriginal;

    public GameDetail GameDetail { get; set; }
    public OnlineGame OnlineGame { get; }

    // view model properties
    public bool IsMatchingEnabled { get; set; }
    
    public ICommand UpdateDatabaseEntryCommand { get; set; }
    public ICommand CreateDatabaseEntryCommand { get; set; }
    
    public string UpdateDatabaseEntryTooltip { get; set; } = "Update existing local database entry";
    public string CreateDatabaseEntryTooltip { get; set; } = "Create new local database entry";

}