﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ClrVpin.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("hello")]
        public string Test {
            get {
                return ((string)(this["Test"]));
            }
            set {
                this["Test"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\vp\\apps\\PinballX")]
        public string FrontendFolder {
            get {
                return ((string)(this["FrontendFolder"]));
            }
            set {
                this["FrontendFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\vp\\tables\\vpx")]
        public string TableFolder {
            get {
                return ((string)(this["TableFolder"]));
            }
            set {
                this["TableFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>Table Audio</string>
  <string>Launch Audio</string>
  <string>Table Videos</string>
  <string>Backglass Videos</string>
  <string>Wheel Images</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection CheckContentTypes {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["CheckContentTypes"]));
            }
            set {
                this["CheckContentTypes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[1,2,3,4,5,6,7]")]
        public string CheckHitTypes {
            get {
                return ((string)(this["CheckHitTypes"]));
            }
            set {
                this["CheckHitTypes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FixHitTypes {
            get {
                return ((string)(this["FixHitTypes"]));
            }
            set {
                this["FixHitTypes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FrontendFoldersJson {
            get {
                return ((string)(this["FrontendFoldersJson"]));
            }
            set {
                this["FrontendFoldersJson"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string BackupFolder {
            get {
                return ((string)(this["BackupFolder"]));
            }
            set {
                this["BackupFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("26")]
        public int RequiredVersion {
            get {
                return ((int)(this["RequiredVersion"]));
            }
            set {
                this["RequiredVersion"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int ActualVersion {
            get {
                return ((int)(this["ActualVersion"]));
            }
            set {
                this["ActualVersion"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool TrainerWheels {
            get {
                return ((bool)(this["TrainerWheels"]));
            }
            set {
                this["TrainerWheels"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[1,2,3,4,5,6,7]")]
        public string MatchHitTypes {
            get {
                return ((string)(this["MatchHitTypes"]));
            }
            set {
                this["MatchHitTypes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[0, 1,2,3,4]")]
        public string OverwriteOptions {
            get {
                return ((string)(this["OverwriteOptions"]));
            }
            set {
                this["OverwriteOptions"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string SourceFolder {
            get {
                return ((string)(this["SourceFolder"]));
            }
            set {
                this["SourceFolder"] = value;
            }
        }
    }
}
