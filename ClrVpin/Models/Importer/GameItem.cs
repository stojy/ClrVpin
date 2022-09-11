using System;
using System.Windows.Input;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;

namespace ClrVpin.Models.Importer;

// a common access class to harmonize access to the underlying OnlineGame or LocalGame properties
[AddINotifyPropertyChangedInterface]
public class GameItem
{
    public GameItem(OnlineGame onlineGame)
    {
        OnlineGame = onlineGame;
        UpdateGameDetail(onlineGame.Hit?.GameDetail);
    }

    public GameItem(GameDetail localGame)
    {
        UpdateGameDetail(localGame);
    }

    public int Index { get; set; }                                                                               // 1 based index of every game in the list
    public string Name => OnlineGame != null ? OnlineGame.Name : GameDetail.Fuzzy.TableDetails.NameOriginalCase; // for unmatched, default to the fuzzy parsed table name
    public string Manufacturer => OnlineGame != null ? OnlineGame.Manufacturer : GameDetail.Game.Manufacturer;
    public string Year => OnlineGame != null ? OnlineGame.YearString : GameDetail.Game.Year;
    public string Type => OnlineGame != null ? OnlineGame.Type : GameDetail.Game.Type;
    public DateTime? UpdatedAt => OnlineGame?.UpdatedAt; // not supported in local DB GameDetail
    public bool IsOriginal => OnlineGame?.IsOriginal ?? GameDetail.Derived.IsOriginal;
    public TableStyleOptionEnum TableStyleOption { get; private set; }

    public GameDetail GameDetail { get; private set; }
    public OnlineGame OnlineGame { get; }
   
    public TableMatchOptionEnum TableMatchType { get; private set; }

    // view model properties
    public bool IsMatchingEnabled { get; set; }

    public ICommand UpdateDatabaseEntryCommand { get; set; }
    public ICommand CreateDatabaseEntryCommand { get; set; }

    public string UpdateDatabaseMatchedEntryTooltip { get; set; } = "Update matched table";
    public string UpdateDatabaseUnmatchedEntryTooltip { get; set; } = "Update unmatched table";
    public string CreateDatabaseEntryTooltip { get; set; } = "Add missing table";

    public void UpdateGameDetail(GameDetail localGame)
    {
        GameDetail = localGame;
        
        TableMatchType = GetTableMatchEnum();
        TableStyleOption = IsOriginal ? TableStyleOptionEnum.Original : TableStyleOptionEnum.Manufactured;
    }

    private TableMatchOptionEnum GetTableMatchEnum()
    {
        if (OnlineGame != null && GameDetail != null)
            return TableMatchOptionEnum.LocalAndOnline;
        if (OnlineGame != null)
            return TableMatchOptionEnum.OnlineOnly;
        return TableMatchOptionEnum.LocalOnly;
    }
}