using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace PhotoImporter
{
    class Program
    {
        private static DirectoryInfo localRoot = new DirectoryInfo("/Users/markotway/Pictures");

        private class FileInfoComparer : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo x, FileInfo y)
            {
                return x == null ? y == null : (x.Name.Equals(y.Name, StringComparison.CurrentCultureIgnoreCase) && x.Name.Length == y.Name.Length);
            }

            public int GetHashCode(FileInfo obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        static void Main(string[] args)
        {
            try
            {
                readSDcard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Thread.Sleep(6 * 1000);
            }
        }


        public static void readSDcard()
        {
            Console.WriteLine("Starting photo import - looking for SD card...");

            var sdCardRootDirectory = GetSDCardRootDir();

            var dir = new DirectoryInfo( Path.Combine(sdCardRootDirectory, "DCIM" ) );

            Console.WriteLine($"Scanning SD card {sdCardRootDirectory} for pictures...");

            // Find all the image files on the SD card
            var allSDCardPics = dir.GetFiles("*.*", SearchOption.AllDirectories)
                              .Where(x => IsImageFile(x))
                              .ToList();

            Console.WriteLine($"Found {allSDCardPics.Count()} images on SD Card. Building list of existing pics in {localRoot.FullName}...");

            // Find all the image files on the local drive
            var localFiles = localRoot.GetFiles("*.*", SearchOption.AllDirectories)
                                       .Where(x => IsImageFile(x))
                                       .ToList();

            // Now, find any files which aren't on the local disk. 
            var newFiles = allSDCardPics.Except(localFiles, new FileInfoComparer()).ToList();

            var dayFolders = newFiles.GroupBy(x => GetFolderDate(x)).ToList();

            Console.WriteLine($"Importing {newFiles.Count()} new files into {dayFolders.Count()} folders...");

            foreach ( var daygroup in dayFolders )
            {
                var newFolder = Path.Combine(localRoot.FullName, daygroup.Key);

                if( FolderExists(newFolder) )
                    daygroup.ToList().ForEach(x => MoveFromSDCard(x, newFolder));
            }

            Console.WriteLine("Import process complete.");
        }

        /// <summary>
        /// Create a folder if necessary. 
        /// </summary>
        /// <param name="newFolder"></param>
        /// <returns>True if the folder exists when complete.</returns>
        private static bool FolderExists( string newFolder )
        {
            bool created = true;

            try
            {
                if (!Directory.Exists(newFolder))
                    Directory.CreateDirectory(newFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Unable to create folder {newFolder}: {ex.Message}.");
                created = false;
            }

            return created;
        }

        /// <summary>
        /// Discovery the SD card location and find it's root folder
        /// </summary>
        /// <returns></returns>
        private static string GetSDCardRootDir()
        {
            var removableDives = DriveInfo.GetDrives()
                //Take only removable drives into consideration as a SD card candidates
                .Where(drive => drive.DriveType == DriveType.Removable || drive.DriveType == DriveType.Fixed)
                .Where(drive => drive.IsReady)
                .Where(drive => drive.Name.StartsWith("/Volumes"))
                //If volume label of SD card is always the same, you can identify
                //SD card by uncommenting following line
                //.Where(drive => drive.VolumeLabel == "MySdCardVolumeLabel")
                .ToList();

            if (removableDives.Count == 0)
                throw new Exception("No SD card found!");

            string sdCardRootDirectory;

            if (removableDives.Count == 1)
            {
                sdCardRootDirectory = removableDives[0].RootDirectory.FullName;
            }
            else
            {
                //Let the user select, which drive to use
                Console.Write($"Please select SD card drive letter ({String.Join(", ", removableDives.Select(drive => drive.Name[0]))}): ");
                var driveLetter = Console.ReadLine().Trim();
                sdCardRootDirectory = driveLetter + ":\\";
            }

            return sdCardRootDirectory;
        }

        /// <summary>
        /// Move the file from the SD card into the pictures folder
        /// </summary>
        /// <param name="f"></param>
        /// <param name="destinationFolder"></param>
        private static void MoveFromSDCard(FileInfo f, string destinationFolder)
        {
            string destFileName = Path.Combine(destinationFolder, f.Name);

            Console.WriteLine($"  Moving {f.Name} to {destFileName}...");
            try
            {
                File.Move(f.FullName, destFileName);
            }
            catch (Exception exMove)
            {
                Console.WriteLine($"Error moving file {f.Name} from SD card: {exMove.Message}");

                try
                {
                    File.Copy(f.FullName, destFileName);

                    Console.WriteLine("File copied instead.");
                }
                catch( Exception exCopy )
                {
                    Console.WriteLine($"Unable to copy file {f.Name} from SD card: {exCopy.Message}");
                }
            }
        }

        /// <summary>
        /// Generate a new date-based folder name
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static string GetFolderDate(FileInfo x)
        {
            return $"SD Card Import {x.CreationTimeUtc:dd-MMM-yyyy}";
        }

        /// <summary>
        /// See if a file is hidden, or is in a hidden directory structure
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsHidden(FileInfo file)
        {
            // Ignore all hidden files.
            if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;

            // Files are considered hidden if they're in a hidden folder too
            var dir = file.Directory;

            while (dir != null)
            {
                if (dir.Name == "Volumes")
                    break;

                if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    return true;

                dir = dir.Parent;
            }

            return false;
        }

        /// <summary>
        /// Determine if the file is a visible image file.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private static bool IsImageFile( FileInfo f )
        {
            if (IsHidden(f))
                return false;

            var extensions = new List<string> { ".jpg", ".jpeg", ".png" };

            return extensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
