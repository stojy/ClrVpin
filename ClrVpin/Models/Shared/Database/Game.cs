using System;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Xml.Serialization;
using ClrVpin.Controls;
using ClrVpin.Shared;

namespace ClrVpin.Models.Shared.Database;

// view model info..
public class Game : GameBase
{
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    [XmlIgnore]
    public Content Content { get; } = new Content();

    [XmlIgnore]
    public string TableFileWithExtension => Name + ".vpx";

    [XmlIgnore]
    public int Number { get; set; }

    [XmlIgnore]
    public string Ipdb { get; set; }

    [XmlIgnore]
    public string IpdbUrl { get; set; }

    [XmlIgnore]
    public bool IsExpanded { get; set; }

    [XmlIgnore]
    public bool IsSelected { get; set; }

    [XmlIgnore]
    public string NameLowerCase { get; set; }

    [XmlIgnore]
    public string DescriptionLowerCase { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ICommand NavigateToIpdbCommand { get; set; }

    [XmlIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyTableDetails { get; set; }

    [XmlIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyDescriptionDetails { get; set; }

    [XmlIgnore]
    public bool IsOriginal { get; set; }
    
    // todo; move below into DatabaseItem?
    [XmlIgnore]
    [JsonIgnore]
    public ICommand ChangedCommand { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ICommand LoadedCommand { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ICommand UnloadedCommand { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> ManufacturersView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> YearsView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> TypesView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<int?> PlayersView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> RomsView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> ThemesView { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ListCollectionView<string> AuthorsView { get; set; }
    
    [XmlIgnore]
    [JsonIgnore]
    public DateTime MaxDateTime { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public DateTime? DateModified { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    // date only portion to accommodate the DatePicker which resets the time portion when a date is selected
    public DateTime? DateModifiedDateOnly { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public DateTime? DateAdded { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    // date only portion to accommodate the DatePicker which resets the time portion when a date is selected
    public DateTime? DateAddedDateOnly { get; set; }



    public string GetContentName(ContentTypeCategoryEnum category) =>
        // determine the correct name - different for media vs pinball
        category == ContentTypeCategoryEnum.Media ? Description : Name;

    public override string ToString() => $"Table: {TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";
}