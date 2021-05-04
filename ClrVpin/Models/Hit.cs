using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ByteSizeLib;
using Utils;

namespace ClrVpin.Models
{
    public class Hit
    {
        public Hit(string contentType, string path, HitTypeEnum type)
        {
            ContentType = contentType;
            Path = path;
            File = System.IO.Path.GetFileName(path);
            Size = type == HitTypeEnum.Missing ? null : new FileInfo(path).Length;
            SizeString = type == HitTypeEnum.Missing ? null : ByteSize.FromBytes(new FileInfo(path).Length).ToString("#");
            Type = type;

            // performance tweak - explicitly assign a property instead of relying on ToString during subsequent binding
            Description = ToString();

            // viewmodel
            IsPresent = Type != HitTypeEnum.Missing;
            OpenFileCommand = new ActionCommand(OpenFile, _ => IsPresent);
            ExplorerCommand = new ActionCommand(ShowInExplorer);
            CopyPathCommand = new ActionCommand(CopyPath);
        }

        public static HitTypeEnum[] Types = {HitTypeEnum.Missing, HitTypeEnum.TableName, HitTypeEnum.DuplicateExtension, HitTypeEnum.WrongCase, HitTypeEnum.Fuzzy, HitTypeEnum.Unknown};

        public string Path { get; }
        public string File { get; }
        public string SizeString { get; }
        public long? Size { get; set; }
        public HitTypeEnum Type { get; }
        public string ContentType { get; }
        public string Description { get; }
        public bool IsPresent { get; set; }

        public ICommand OpenFileCommand { get; set; }
        public ICommand ExplorerCommand { get; set; }
        public ICommand CopyPathCommand { get; set; }

        private void OpenFile()
        {
            // launch file via the shell to open via the associated application
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(Path)
                {
                    UseShellExecute = true
                }
            };
            process.Start();
        }

        private void ShowInExplorer() => Process.Start("explorer.exe", $"/select,{Path}");

        private void CopyPath() => Clipboard.SetText(Path);

        public sealed override string ToString() => $"{ContentType} - {Type.GetDescription()}: {Path}";
    }
}