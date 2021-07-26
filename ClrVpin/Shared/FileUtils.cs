using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClrVpin.Logging;
using ClrVpin.Models;
using Utils;

namespace ClrVpin.Shared
{
    public static class FileUtils
    {
        static FileUtils()
        {
            _settings = Model.Settings;
        }

        public static string ActiveBackupFolder { get; private set; }

        public static string SetActiveBackupFolder(string rootBackupFolder)
        {
            _rootBackupFolder = rootBackupFolder;
            return ActiveBackupFolder = $"{rootBackupFolder}\\{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }

        public static void Delete(string file, HitTypeEnum hitTypeEnum, string contentType)
        {
            var prefix = _settings.TrainerWheels ? "Ignored (trainer wheels are on) " : "";
            Logger.Warn($"{prefix}Deleting file.. type: {hitTypeEnum.GetDescription()}, content: {contentType ?? "n/a"}, file: {file}");

            if (!_settings.TrainerWheels)
            {
                Backup(file, "deleted");
                File.Delete(file);
            }
        }

        public static void Rename(string originalFile, string newFile, HitTypeEnum hitTypeEnum, string contentType)
        {
            var prefix = _settings.TrainerWheels ? "Ignored (trainer wheels are on) " : "";
            Logger.Info($"{prefix}Renaming file.. type: {hitTypeEnum.GetDescription()}, content: {contentType}, original: {originalFile}, new: {newFile}");

            if (!_settings.TrainerWheels)
            {
                Backup(originalFile, "renamed");
                File.Move(originalFile, newFile, true);
            }
        }

        public static void Merge(string sourceFile, string destinationFile, HitTypeEnum hitTypeEnum, string contentType, bool deleteSource, bool preserveDateModified)
        {
            var prefix = _settings.TrainerWheels ? "Ignored (trainer wheels are on) " : "";

            Logger.Info($"{prefix}Merging file.. type: {hitTypeEnum.GetDescription()}, content: {contentType}, source: {sourceFile}, destination: {destinationFile}");

            if (!_settings.TrainerWheels)
            {
                // backup the existing file (if any) before overwriting
                if (File.Exists(destinationFile))
                {
                    Logger.Info($"- deleting.. file: {destinationFile}");
                    Backup(destinationFile, "deleted");
                }

                // backup the source file that is to be used
                Backup(sourceFile, "merged");
                File.Copy(sourceFile, destinationFile, true);
                Logger.Info($"- copying.. source: {sourceFile}, destination: {destinationFile}");

                // delete the source file if required - no need to backup as this is already done in the "merged" folder
                if (deleteSource)
                {
                    Logger.Info($"- deleting.. file: {sourceFile}");
                    File.Delete(sourceFile);
                }

                // optionally reset date modified if preservation isn't selected
                // - by default windows behaviour when copying file.. last access & creation timestamps are DateTime.Now, but last modified is unchanged!
                if (!preserveDateModified)
                    File.SetLastWriteTime(destinationFile, DateTime.Now);
            }
        }

        public static IEnumerable<FileDetail> DeleteAllExcept(IEnumerable<Hit> hits, Hit hit, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var deleted = new List<FileDetail>();

            // delete all 'real' files except the specified hit
            hits.Except(hit).Where(x => x.Size.HasValue).ForEach(h => deleted.Add(Delete(h, supportedHitTypes)));

            return deleted;
        }

        public static FileDetail Rename(Hit hit, Game game, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var renamed = false;

            if (supportedHitTypes.Contains(hit.Type))
            {
                renamed = true;

                // determine the correct name - different for media vs pinball
                var correctName = game.GetContentName(_settings.GetContentType(hit.ContentTypeEnum).Category);

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{correctName}{extension}");

                Rename(hit.Path, newFile, hit.Type, hit.ContentType);
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, renamed ? FixFileTypeEnum.Renamed : null, hit.Path, hit.Size ?? 0);
        }

        public static void DeleteActiveBackupFolderIfEmpty()
        {
            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            if (Directory.Exists(ActiveBackupFolder))
            {
                var files = Directory.EnumerateFiles(ActiveBackupFolder, "*", SearchOption.AllDirectories);
                if (!files.Any())
                {
                    Logger.Info($"Deleting empty backup folder: '{ActiveBackupFolder}'");
                    Directory.Delete(ActiveBackupFolder, true);
                }
            }

            // if directory doesn't exist (e.g. deleted as per above OR never existed), then assign the active folder back to the root folder, i.e. a valid folder that exists
            if (!Directory.Exists(ActiveBackupFolder))
                ActiveBackupFolder = _rootBackupFolder;
        }

        private static void Backup(string file, string subFolder = "")
        {
            // backup file (aka copy) to the specified sub folder
            var backupFile = CreateBackupFileName(file, subFolder);
            File.Copy(file, backupFile, true);
        }

        public static IEnumerable<string> GetKindredFiles(FileInfo fileInfo, IEnumerable<string> kindredExtensionsList)
        {
            var kindredExtensions = kindredExtensionsList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimStart('*').ToLower());
            var allFiles = Directory.EnumerateFiles(fileInfo.DirectoryName!, $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.*").Select(x => x.ToLower()).ToList();

            var kindredFiles = allFiles.Where(file => kindredExtensions.Any(file.EndsWith)).ToList();
            return kindredFiles;
        }

        private static string CreateBackupFileName(string file, string subFolder = "")
        {
            var contentFolder = Path.GetDirectoryName(file)!.Split("\\").Last();
            var folder = Path.Combine(ActiveBackupFolder, subFolder, contentFolder);
            var destFileName = Path.Combine(folder, Path.GetFileName(file));

            // store backup file in the same folder structure as the source file
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return destFileName;
        }

        private static FileDetail Delete(Hit hit, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var deleted = false;

            // only delete file if configured to do so
            if (supportedHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Delete(hit.Path, hit.Type, hit.ContentType);
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, deleted ? FixFileTypeEnum.Deleted : null, hit.Path, hit.Size ?? 0);
        }

        private static string _rootBackupFolder;

        private static readonly Models.Settings.Settings _settings;
    }
}