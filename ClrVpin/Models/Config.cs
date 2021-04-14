using System.Collections.Generic;

namespace ClrVpin.Models
{
    public class Config
    {
        public static string VpxFrontendFolder { get; set; } = @"C:\vp\apps\PinballX";
        public static string VpxTablesFolder { get; set; } = @"C:\vp\tables\vpx";

        public static readonly List<string> CheckContentTypes = new List<string>(Content.Types);
        public static readonly List<HitType> CheckHitTypes = new List<HitType>(Hit.Types);
        public static readonly List<HitType> FixHitTypes = new List<HitType>(Hit.Types);
    }
}