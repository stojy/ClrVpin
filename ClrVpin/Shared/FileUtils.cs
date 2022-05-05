using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteSizeLib;
using ClrVpin.Logging;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using Utils.Extensions;

namespace ClrVpin.Shared
{
    public static class FileUtils
    {
        static FileUtils()
        {
            _settings = Model.Settings;
        }

        public static string ActiveBackupFolder { get; private set; }

        public static void SetActiveBackupFolder(string rootBackupFolder)
        {
            _rootBackupFolder = rootBackupFolder;
            ActiveBackupFolder = $"{rootBackupFolder}\\{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        }

        public static void Delete(string path, HitTypeEnum hitTypeEnum, string contentType, Action<string> backupAction = null)
        {
            Backup(path, "deleted", backupAction);
            Delete(path);
        }

        public static void DeleteIgnored(string sourcePath, string destinationPath, HitTypeEnum hitTypeEnum, string contentType, Action<string> backupAction = null)
        {
            Backup(sourcePath, "deleted.ignored", backupAction);
            DeleteIgnored(sourcePath, destinationPath);
        }

        public static IEnumerable<FileDetail> DeleteAllExcept(IEnumerable<Hit> hits, Hit hit, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var deleted = new List<FileDetail>();

            // delete all 'real' files except the specified hit, e.g. HitType=Missing won't have any Size
            hits.Except(hit).Where(x => x.Size.HasValue).ForEach(h => deleted.Add(Delete(h, supportedHitTypes)));

            return deleted;
        }

        public static void Merge(string sourcePath, string destinationPath, HitTypeEnum hitTypeEnum, string contentType, bool deleteSource, bool preserveDateModified, IEnumerable<string> kindredExtensions,
            Action<string> backupAction)
        {
            // merge the specific file
            Merge(sourcePath, destinationPath, hitTypeEnum, contentType, deleteSource, preserveDateModified, backupAction);

            // merge any kindred files
            ExecuteForKindred(kindredExtensions, sourcePath, destinationPath, (source, destination) => Merge(source, destination, hitTypeEnum, contentType, deleteSource, preserveDateModified));
        }

        public static FileDetail Rename(Hit hit, Game game, ICollection<HitTypeEnum> supportedHitTypes, IEnumerable<string> kindredExtensions)
        {
            var renamed = false;

            if (supportedHitTypes.Contains(hit.Type))
            {
                renamed = true;

                // determine the correct name - different for media vs pinball
                var newFile = GetCorrectFile(game, _settings.GetContentType(hit.ContentTypeEnum).Category, hit.Directory, hit.Extension);

                // rename specific file
                Rename(hit.Path, newFile, hit.Type, hit.ContentType, backupFile => hit.Path = backupFile);

                // rename any kindred files
                ExecuteForKindred(kindredExtensions, hit.Path, newFile, (source, destination) => Rename(source, destination, hit.Type, hit.ContentType));
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, renamed ? FixFileTypeEnum.Renamed : FixFileTypeEnum.Skipped, hit.Path, hit.Size ?? 0);
        }

        public static string GetCorrectFile(Game game, ContentTypeCategoryEnum category, string path, string extension)
        {
            // determine the correct name - which has a different calculation for media vs pinball :(
            var correctContentName = game.GetContentName(category);

            // use supplied path and extension - i.e. accommodate importing (source path) and scanning (destination path)
            return Path.Combine(path!, $"{correctContentName}{extension}");
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

        public static string GetFileInfoStatistics(string file)
        {
            string details;
            if (File.Exists(file))
            {
                var fileInfo = new FileInfo(file);
                details = $"{ByteSize.FromBytes(fileInfo.Length).ToString("0.#"),-8} {fileInfo.LastWriteTime:dd/MM/yy HH:mm:ss} - {file}";
            }
            else if (file != null)
                details = $"{"(n/a: new file)",-26} - {file}";
            else
                details = $"{"(n/a: unmatched file)",-26}";

            return details;
        }

        public static bool HasInvalidFileNameChars(this string path)
        {
            // empty path has no invalid chars!
            if (path == null)
                return false;

            var fileName = Path.GetFileName(path);
            return fileName.IndexOfAny(_invalidFileNameChars) != -1;
        }

        private static void Rename(string sourcePath, string newPath, HitTypeEnum hitTypeEnum, string contentType, Action<string> backupAction = null)
        {
            //Logger.Info($"Renaming file{GetTrainerWheelsDisclosure()}.. type: {hitTypeEnum.GetDescription()}, content: {contentType}, original: {sourcePath}, new: {newPath}");
            Backup(sourcePath, "renamed", backupAction);
            Rename(sourcePath, newPath);
        }

        private static void Merge(string sourcePath, string destinationPath, HitTypeEnum hitTypeEnum, string contentType, bool deleteSource, bool preserveDateModified, Action<string> backupAction = null)
        {
            // backup the existing file (if any) before overwriting
            Backup(destinationPath, "deleted");

            // backup the source file before merging it
            Backup(sourcePath, "merged", backupAction);

            // copy the source file into the 'merged' destination folder
            Copy(sourcePath, destinationPath);

            // delete the source file if required - no need to backup as this is already done in the "merged" folder
            if (deleteSource)
                Delete(sourcePath);

            // optionally reset date modified if preservation isn't selected
            // - by default windows behaviour when copying file.. last access & creation timestamps are DateTime.Now, but last modified is unchanged!
            if (!preserveDateModified)
            {
                if (File.Exists(destinationPath))
                    File.SetLastWriteTime(destinationPath, DateTime.Now);
            }
        }

        private static void ExecuteForKindred(IEnumerable<string> kindredExtensions, string sourceFile, string destinationFile, Action<string, string> action)
        {
            // merge any kindred files
            var kindredFiles = GetKindredFiles(new FileInfo(sourceFile), kindredExtensions);
            var destinationFolder = Path.GetDirectoryName(destinationFile);

            kindredFiles.ForEach(file =>
            {
                // use source file name (minus extension) instead of kindred file name to ensure the case is correct!
                var fileName = $"{Path.GetFileNameWithoutExtension(destinationFile)}{Path.GetExtension(file)}";

                var destinationFileName = Path.Combine(destinationFolder!, fileName);
                action(file, destinationFileName);
            });
        }

        private static IEnumerable<string> GetKindredFiles(FileInfo fileInfo, IEnumerable<string> kindredExtensionsList)
        {
            var kindredExtensions = kindredExtensionsList.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.TrimStart('*').ToLower());
            var allFiles = Directory.EnumerateFiles(fileInfo.DirectoryName!, $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.*").Select(x => x.ToLower()).ToList();

            var kindredFiles = allFiles.Where(file => kindredExtensions.Any(file.EndsWith)).ToList();
            return kindredFiles;
        }

        private static void Backup(string file, string subFolder, Action<string> backupAction = null)
        {
            if (!_settings.TrainerWheels && File.Exists(file))
            {
                // backup file (aka copy) to the specified sub folder
                // - no logging since the backup is intended to be transparent
                var backupFile = CreateBackupFileName(file, subFolder);
                File.Copy(file, backupFile, true);

                backupAction?.Invoke(backupFile);
            }
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
                Delete(hit.Path, hit.Type, hit.ContentType, newFile => hit.Path = newFile);
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, deleted ? FixFileTypeEnum.Deleted : FixFileTypeEnum.Skipped, hit.Path, hit.Size ?? 0);
        }

        private static void Delete(string sourcePath)
        {
            Logger.Debug($"- deleting{GetTrainerWheelsDisclosure()}..\n  source: {GetFileInfoStatistics(sourcePath)}");

            if (!_settings.TrainerWheels)
                File.Delete(sourcePath);
        }

        // same as delete, but also logging the destination file info (for comparison)
        private static void DeleteIgnored(string sourcePath, string destinationPath)
        {
            Logger.Debug($"- deleting ignored{GetTrainerWheelsDisclosure()}..\n  source: {GetFileInfoStatistics(sourcePath)}\n  dest:   {GetFileInfoStatistics(destinationPath)}");

            if (!_settings.TrainerWheels)
                File.Delete(sourcePath);
        }

        private static void Rename(string sourcePath, string destinationPath)
        {
            string skippedDisclaimer = null;
            var isWarning = false;

            // confirm the new file path is valid before attempting renaming, e.g. no invalid character.. ':', '/', etc
            if (HasInvalidFileNameChars(destinationPath))
            {
                skippedDisclaimer = " (skipped: dest has invalid file name characters)";
                isWarning = true;
            }

            skippedDisclaimer ??= GetTrainerWheelsDisclosure();

            var message = $"- renaming{skippedDisclaimer}..\n  source: {GetFileInfoStatistics(sourcePath)}\n  dest:   {GetFileInfoStatistics(destinationPath)}";
            if (isWarning)
                Logger.Error(message);
            else
                Logger.Debug(message);

            // only perform the rename if their is no skipped required
            if (skippedDisclaimer == null)
                File.Move(sourcePath, destinationPath, true);
        }

        private static void Copy(string sourcePath, string destinationPath)
        {
            Logger.Debug($"- copying{GetTrainerWheelsDisclosure()}..\n  source: {GetFileInfoStatistics(sourcePath)}\n  dest:   {GetFileInfoStatistics(destinationPath)}");

            if (!_settings.TrainerWheels)
                File.Copy(sourcePath, destinationPath, true);
        }

        private static string GetTrainerWheelsDisclosure() => _settings.TrainerWheels ? " (skipped: trainer wheels)" : null;
        private static string _rootBackupFolder;
        private static readonly Models.Settings.Settings _settings;

        private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();
    }
}