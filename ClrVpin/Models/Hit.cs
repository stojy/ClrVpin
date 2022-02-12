using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ByteSizeLib;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Models
{
    public class Hit
    {
        public Hit(ContentTypeEnum contentTypeEnum, string path, HitTypeEnum type)
        {
            ContentTypeEnum = contentTypeEnum;
            ContentType = contentTypeEnum.GetDescription();
            Path = path;    // full path + file name
            File = System.IO.Path.GetFileName(path);
            FileInfo = type == HitTypeEnum.Missing ? null : new FileInfo(path);
            Size = FileInfo?.Length;
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

        public string Path { get; set; }
        public string File { get; }
        public FileInfo FileInfo { get; }
        public string SizeString { get; }
        public long? Size { get; set; }
        public HitTypeEnum Type { get; }
        public string Description { get; }
        public bool IsPresent { get; set; }
        public string ContentType { get; }
        public ContentTypeEnum ContentTypeEnum { get; set; }

        public ICommand OpenFileCommand { get; set; }
        public ICommand ExplorerCommand { get; set; }
        public ICommand CopyPathCommand { get; set; }

        private void OpenFile()
        {
            try
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
            catch (Exception e)
            {
                Logging.Logger.Error(e, $"failed to open file: {Path}");

                // don't rethrow
            }
        }

        private void ShowInExplorer() => Process.Start("explorer.exe", $"/select,{Path}");

        private void CopyPath() => Clipboard.SetText(Path);

        public sealed override string ToString() => $"{ContentType} - {Type.GetDescription()}: {Path}";
    }
}