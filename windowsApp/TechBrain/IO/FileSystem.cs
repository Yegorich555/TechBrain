using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using TechBrain.Extensions;

namespace TechBrain.IO
{
    public enum FileCopyMode
    {
        CopyIfNotExist,
        ReplaceExisting,
        SaveExisting
    }

    public class FileSystem
    {
        public static void MoveFile(string path, string destPath, FileCopyMode copyMode = FileCopyMode.SaveExisting)
        {
            if (!File.Exists(destPath))
            {
                File.Move(path, destPath);
                return;
            }

            if (copyMode == FileCopyMode.ReplaceExisting)
            {
                File.Delete(destPath);
                File.Move(path, destPath);
                return;
            }

            if (copyMode == FileCopyMode.SaveExisting)
            {
                var i = 0;
                var ext = Path.GetExtension(destPath);
                var index = destPath.Length - ext.Length;
                while (true)
                {
                    ++i;
                    var nPath = destPath.Insert(index, "_" + i);
                    if (!File.Exists(nPath))
                    {
                        File.Move(path, nPath);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Return location of the current application
        /// </summary>
        public static string Location
        {
            get
            {
                var fullPath = Assembly.GetExecutingAssembly().Location;
                string location = Path.GetDirectoryName(fullPath);
                return location;
            }
        }

        /// <summary>
        /// Check write-rights to path
        /// </summary>
        public static bool HasWritePermission(string path)
        {
            return HasPermission(path, FileSystemRights.Write);
        }

        /// <summary>
        /// Check full-rights for path
        /// </summary>
        public static bool HasPermission(string path, FileSystemRights fileSystemRights)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var wrAllow = false;
            var wrDeny = false;
            var accessControlList = Directory.GetAccessControl(path);
            if (accessControlList == null)
                return false;
            var accessRules = accessControlList.GetAccessRules(true, true, typeof(SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((fileSystemRights & rule.FileSystemRights) != fileSystemRights)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    wrAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    wrDeny = true;
            }

            return wrAllow && !wrDeny;
        }

        /// <summary>
        /// Append line to file (if directory or file doesn't exist then try create it)
        /// </summary>
        public static void AppendLine(string fileName, object text)
        {
            string path = Path.GetDirectoryName(fileName);
            Directory.CreateDirectory(path);
            File.AppendAllText(fileName, text.ToStringNull() + Environment.NewLine);
        }


        /// <summary>
        /// Copy file or directory (with inside files) in new directory
        /// </summary>
        /// <param name="sourcePath">Full name of source directory</param>
        /// <param name="destDirectory">Path of destination directory (only name directory)</param>
        public static void Copy(string sourcePath, string destDirectory)
        {
            Copy(sourcePath, destDirectory, null, null);
        }

        /// <summary>
        /// Copy file or directory (with inside files) in new directory
        /// </summary>
        /// <param name="sourcePath">Full name of source directory</param>
        /// <param name="destDirectory">Path of destination directory (only name directory)</param>
        /// <param name="regexFilterNoFiles">Regex pattern for none copyied files</param>
        public static void Copy(string sourcePath, string destDirectory, string regexFilterNoFiles)
        {
            Copy(sourcePath, destDirectory, regexFilterNoFiles, null);
        }

        /// <summary>
        /// Copy file or directory (with inside files) in new directory
        /// </summary>
        /// <param name="sourcePath">Full name of source directory</param>
        /// <param name="destDirectory">Path of destination directory (only name directory)</param>
        /// <param name="regexFilterNoFiles">Regex pattern for none copyied files</param>
        /// <param name="regexFilterNoDirs">Regex pattern for none copyied folders</param>
        public static void Copy(string sourcePath, string destDirectory, string regexFilterNoFiles, string regexFilterNoDirs)
        {
            if (IsFile(sourcePath))
                FileCopy(sourcePath, destDirectory);
            else
                DirectoryCopy(sourcePath, destDirectory, regexFilterNoFiles, regexFilterNoDirs);
        }

        static void FileCopy(string sourcePath, string destDirectory)
        {
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            var destPath = Path.Combine(destDirectory, Path.GetFileName(sourcePath));
            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Copy(sourcePath, destPath);
        }

        /// <summary>
        /// Delete directory including files and subdirs
        /// </summary>
        public static void DirectoryDelete(string path)
        {
            DirectoryInfo dirs = new DirectoryInfo(path);
            if (dirs.Exists)
                dirs.Delete(true);
        }

        /// <summary>
        /// Copy directory with inside files in new path
        /// </summary>
        /// <param name="sourcePath">Full name of source directory</param>
        /// <param name="destDirectory">Path of destination directory (only name directory)</param>
        /// <param name="regexFilterNoFiles">Regex pattern for none copyied files</param>
        /// <param name="regexFilterNoDirs">Regex pattern for none copyied folders</param>
        static void DirectoryCopy(string sourcePath, string destDirectory, string regexFilterNoFiles, string regexFilterNoDirs)
        {
            if (sourcePath.IsNull())
                throw new ArgumentNullException("Exception DirectoryCopy(): sourceDirName is empty");
            if (destDirectory.IsNull())
                throw new ArgumentNullException("Exception DirectoryCopy(): destDirName is empty");
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException("Exception DirectoryCopy(): directory \'" + sourcePath + "\" not exist");

            //define end directory name
            var treeDir = sourcePath.Split('\\');
            var createName = treeDir[treeDir.LastIndex()];
            if (createName.IsNull())
                createName = treeDir[treeDir.Length - 2];
            var endDirName = destDirectory + "\\" + createName;

            //remove old directory
            //if (replaceDirectory)
            DirectoryDelete(endDirName);

            //create new directory
            if (!Directory.Exists(endDirName))
                Directory.CreateDirectory(endDirName);

            //get and copy all files
            var dir = new DirectoryInfo(sourcePath);
            var dirs = dir.GetDirectories();
            FileInfo[] files = dir.GetFiles();

            //Create regex
            Regex regex = null;
            if (regexFilterNoFiles != null)
                regex = new Regex(regexFilterNoFiles, RegexOptions.IgnoreCase);

            foreach (FileInfo file in files)
            {
                if (regex != null && regex.IsMatch(file.Name))
                    continue;
                var temppath = Path.Combine(endDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            regex = null;
            if (regexFilterNoDirs != null)
                regex = new Regex(regexFilterNoDirs, RegexOptions.IgnoreCase);

            //copy subDirectories
            foreach (DirectoryInfo subdir in dirs)
            {
                var temppath = Path.Combine(endDirName, subdir.Name);
                if (regex != null && regex.IsMatch(subdir.Name))
                    continue;
                DirectoryCopy(subdir.FullName, temppath, regexFilterNoFiles, regexFilterNoDirs);
            }
        }

        /// <summary>
        /// Check path is file or dir
        /// </summary>
        public static bool IsFile(string path)
        {
            if (File.Exists(path))
                return true;

            if (Directory.Exists(path))
                return false;

            return !Path.GetFileName(path).IsNull();
        }

        /// <summary>
        /// Return existable path
        /// </summary>
        public static bool ExistPath(string path)
        {
            if (IsFile(path))
                return File.Exists(path);
            else
                return Directory.Exists(path);
        }

        /// <summary>
        /// Return path without file name
        /// </summary>
        public static string GetDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Get pathes for all files/folders in sourceDirName (including subdirs)
        /// </summary>
        public static List<string> Map(string sourceDirName, string excludeRegex = @"\.(tmp|dmp)")
        {
            var lst = new List<string>();
            GetAllFiles(lst, sourceDirName, "", excludeRegex);
            return lst;
        }

        static void GetAllFiles(List<string> lst, string sourceDirName, string parentDirName, string excludeRegex)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
                throw new DirectoryNotFoundException();

            DirectoryInfo[] dirs = dir.GetDirectories();

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                var extension = Path.GetExtension(file.Name);
                if (!Regex.IsMatch(extension, excludeRegex))
                    lst.Add(parentDirName + file.Name);
            }

            foreach (DirectoryInfo subdir in dirs)
                GetAllFiles(lst, subdir.FullName, parentDirName + subdir + "\\", excludeRegex);
        }

        public static bool TryRead(string path, out string str)
        {
            str = null;
            if (File.Exists(path))
            {
                str = File.ReadAllText(path);
                return true;
            }
            return false;
        }

    }

}
