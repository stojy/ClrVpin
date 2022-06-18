namespace ClrVpin.Shared;

public class Notification
{
    public string Title { get; set; }
    public string Detail { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsWarning { get; set; }
    public bool IsError { get; set; }
}