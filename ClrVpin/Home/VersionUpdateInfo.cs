using System;

namespace ClrVpin.Home
{
    public class VersionUpdateInfo
    {
        public string Title { get; init; }
        public string ExistingVersion { get; init; }
        public string NewVersion { get; init; }
        public DateTime CreatedAt { get; init; }
        public string ReleaseNotes { get; init; }
    }
}