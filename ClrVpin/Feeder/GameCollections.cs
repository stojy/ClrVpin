using System.Collections.Generic;

namespace ClrVpin.Feeder;

public interface IGameCollections
{
    IList<string> Manufacturers { get; }
    IList<string> Types { get; }
    IList<string> Years { get; }
    IList<string> Players { get; }
    IList<string> Roms { get; }
    IList<string> Themes { get; }
    IList<string> Authors { get; }

    public void UpdateCollections();
}

public abstract class GameCollections : IGameCollections
{
    // IGameCollections
    public IList<string> Manufacturers { get; protected set; } = new List<string>();
    public IList<string> Types { get; protected set; } = new List<string>();
    public IList<string> Years { get; protected set; } = new List<string>();
    public IList<string> Players { get; protected set; } = new List<string>();
    public IList<string> Roms { get; protected set; } = new List<string>();
    public IList<string> Themes { get; protected set; } = new List<string>();
    public IList<string> Authors { get; protected set; } = new List<string>();

    public abstract void UpdateCollections();
}