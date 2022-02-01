using PropertyChanged;

namespace ClrVpin.About;

[AddINotifyPropertyChangedInterface]
public class Contributor
{
    public Contributor(string url, string note = null)
    {
        Url = url;
        Note = note;
    }

    public string Url { get; set; }
    public string Note { get; set; }
}