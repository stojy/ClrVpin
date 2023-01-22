using System;
using System.Collections.Generic;
using System.Linq;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Shared;

public interface IGameCollections
{
    // ReSharper disable once UnusedMemberInSuper.Global
    IList<string> TableNames { get; }
    IList<string> Manufacturers { get; }
    IList<string> Types { get; }
    IList<string> Years { get; }
    IList<string> Players { get; }
    IList<string> Roms { get; }
    IList<string> Themes { get; }
    IList<string> Authors { get; }
    // ReSharper disable once UnusedMemberInSuper.Global
    IList<string> Formats { get; }

    public void Update();
}

[AddINotifyPropertyChangedInterface]
public class GameCollections : IGameCollections
{
    public GameCollections(IList<GameItem> gameItems, Action updatedAction)
    {
        _gameItems = gameItems;
        Update();

        // don't assign the updated action to after the collection is created to avoid the initial callback from firing
        // - required because the view models are unlikely to be ready, i.e. not fully initialized
        _updatedAction = updatedAction;
    }

    public IList<string> TableNames { get; private set; }
    public IList<string> Manufacturers { get; private set; }
    public IList<string> Types { get; private set; }
    public IList<string> Years { get; private set; }
    public IList<string> Players { get; private set; }
    public IList<string> Roms { get; private set; }
    public IList<string> Themes { get; private set; }
    public IList<string> Authors { get; private set; }
    public IList<string> Formats { get; private set; }

    public void Update()
    {
        // the collections consist of all the possible permutations from BOTH the online source and the local source
        // - this is to ensure the maximum possible options are presented AND that the active item (from the local DB in the case of the update dialog) is actually in the list,
        //   otherwise it will be assigned to null via the ListCollectionView when the SelectedItem is assigned (either explicitly or via binding)
        
        TableNames = _gameItems.Select(x => x.Names).SelectManyUnique();
        Manufacturers = _gameItems.Select(x => x.Manufacturers).SelectManyUnique();

        Years = _gameItems.Select(x => x.Years).SelectManyUnique();
        Types = _gameItems.Select(x => x.Types).SelectManyUnique();

        // table formats - vpx, fp, etc.. only available via online
        Formats = _gameItems.SelectMany(x => x.OnlineGame?.TableFormats ?? new List<string>()).Distinct().Where(x => x != null).OrderBy(x => x).ToList();

        Themes = _gameItems.Select(x => x.Themes).SelectManyUnique();
        Players = _gameItems.Select(x => x.Players).SelectManyUnique();
        Roms = _gameItems.Select(x => x.Roms).SelectManyUnique();
        Authors = _gameItems.Select(x => x.Authors).SelectManyUnique();

        // invoke the callback for any addition processing, e.g. updating GameFiltersViewModel
        _updatedAction?.Invoke();
    }

    private readonly IList<GameItem> _gameItems;
    private readonly Action _updatedAction;
}