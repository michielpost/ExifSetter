using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Text.RegularExpressions;

namespace ExifSetter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EXIF Date Setter & File Manager");
            Console.WriteLine("=================================");
            Console.WriteLine();

            // Ask for feature selection
            Console.WriteLine("Select feature:");
            Console.WriteLine("1. Set EXIF dates from folder/file names");
            Console.WriteLine("2. Copy and rename files to EXPORT folder");
            Console.Write("Enter choice (1 or 2): ");
            string? choice = Console.ReadLine();

            if (choice != "1" && choice != "2")
            {
                Console.WriteLine("Error: Invalid choice. Please enter 1 or 2.");
                return;
            }

            Console.WriteLine();

            // Ask for directory
            Console.Write("Please enter the directory path: ");
            string? directoryPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                Console.WriteLine("Error: Directory path cannot be empty.");
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: Directory '{directoryPath}' does not exist.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Scanning directory: {directoryPath}");
            Console.WriteLine();

            // Process based on choice
            if (choice == "1")
            {
                ProcessDirectory(directoryPath);
            }
            else
            {
                ProcessFileCopying(directoryPath);
            }

            Console.WriteLine();
            Console.WriteLine("Processing complete!");
        }

        static void ProcessDirectory(string directoryPath)
        {
            try
            {
                // Get all JPG files in the directory and subdirectories
                var jpgFiles = new List<string>();
                jpgFiles.AddRange(Directory.EnumerateFiles(directoryPath, "*.jpg", SearchOption.AllDirectories));
                jpgFiles.AddRange(Directory.EnumerateFiles(directoryPath, "*.jpeg", SearchOption.AllDirectories));

                Console.WriteLine($"Found {jpgFiles.Count} JPG files.");
                Console.WriteLine();

                // Group files by directory to maintain folder grouping, then sort by filename within each group
                var groupedFiles = jpgFiles
                    .GroupBy(f => Path.GetDirectoryName(f) ?? string.Empty)
                    .OrderBy(g => g.Key)
                    .SelectMany(g => g.OrderBy(f => Path.GetFileName(f)))
                    .ToList();

                int totalFiles = groupedFiles.Count;
                int currentFile = 0;
                int successCount = 0;
                int skipCount = 0;
                int alreadySetCount = 0;
                int errorCount = 0;

                // Track skipped and error files for final report
                List<(string FilePath, string Reason)> skippedFiles = new();
                List<(string FilePath, string Error)> errorFiles = new();

                // Track the last used datetime to ensure unique timestamps
                Dictionary<DateTime, DateTime> lastUsedDateTimes = new();

                foreach (string filePath in groupedFiles)
                {
                    currentFile++;

                    try
                    {
                        // Try to parse date from the file path
                        DateTime? parsedDate = ParseDateFromPath(filePath);

                        if (parsedDate.HasValue)
                        {
                            // Get or initialize the last used datetime for this base date
                            DateTime baseDate = parsedDate.Value.Date;
                            DateTime dateTimeToSet;

                            if (lastUsedDateTimes.TryGetValue(baseDate, out DateTime lastUsed))
                            {
                                // Increment by 1 second to ensure unique timestamp
                                dateTimeToSet = lastUsed.AddSeconds(1);

                                // If we've exceeded the day, increment by minutes instead
                                if (dateTimeToSet.Date != baseDate)
                                {
                                    // Reset and use smaller increments (shouldn't happen with normal file counts)
                                    dateTimeToSet = lastUsed.AddMilliseconds(1);
                                }
                            }
                            else
                            {
                                // First file for this date, start at midnight
                                dateTimeToSet = baseDate;
                            }

                            // Update the last used datetime for this base date
                            lastUsedDateTimes[baseDate] = dateTimeToSet;

                            // Check if EXIF date is already set to the same value
                            DateTime? currentExifDate = GetExifDate(filePath);
                            if (currentExifDate.HasValue && currentExifDate.Value == dateTimeToSet)
                            {
                                Console.WriteLine($"[{currentFile}/{totalFiles}] = Already set {dateTimeToSet:yyyy-MM-dd HH:mm:ss}: {filePath}");
                                alreadySetCount++;
                                continue;
                            }

                            // Set the EXIF date with unique timestamp
                            SetExifDate(filePath, dateTimeToSet);
                            Console.WriteLine($"[{currentFile}/{totalFiles}] ✓ Set date {dateTimeToSet:yyyy-MM-dd HH:mm:ss}: {filePath}");
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"[{currentFile}/{totalFiles}] - Skipped (no date found): {filePath}");
                            skippedFiles.Add((filePath, "No date found in path"));
                            skipCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{currentFile}/{totalFiles}] ✗ Error: {filePath}: {ex.Message}");
                        errorFiles.Add((filePath, ex.Message));
                        errorCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {successCount} updated, {alreadySetCount} already set, {skipCount} skipped, {errorCount} errors");

                // Print detailed report of skipped and error files
                if (skippedFiles.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== SKIPPED FILES ===");
                    foreach (var (file, reason) in skippedFiles)
                    {
                        Console.WriteLine($"  {file}");
                        Console.WriteLine($"    Reason: {reason}");
                    }
                }

                if (errorFiles.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== ERROR FILES ===");
                    foreach (var (file, error) in errorFiles)
                    {
                        Console.WriteLine($"  {file}");
                        Console.WriteLine($"    Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory: {ex.Message}");
            }
        }

        static DateTime? ParseDateFromPath(string filePath)
        {
            // Get the full path parts (directories and filename)
            string[] pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Check each part from the end to the beginning
            for (int i = pathParts.Length - 1; i >= 0; i--)
            {
                string part = pathParts[i];
                DateTime? date = ParseDateFromString(part);
                if (date.HasValue)
                {
                    return date;
                }
            }

            return null;
        }

        static DateTime? ParseDateFromString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            // Pattern for YYYY-MM-DD followed by space or underscore
            var datePatternFull = new Regex(@"^(\d{4})-(\d{2})-(\d{2})[\s_]");
            var match = datePatternFull.Match(text);
            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int day = int.Parse(match.Groups[3].Value);

                if (IsValidDate(year, month, day))
                {
                    return new DateTime(year, month, day);
                }
            }

            // Pattern for YYYY-MM followed by space or underscore
            var datePatternMonth = new Regex(@"^(\d{4})-(\d{2})[\s_]");
            match = datePatternMonth.Match(text);
            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);

                if (IsValidDate(year, month, 1))
                {
                    return new DateTime(year, month, 1);
                }
            }

            // Pattern for YYYY followed by space or underscore
            var datePatternYear = new Regex(@"^(\d{4})[\s_]");
            match = datePatternYear.Match(text);
            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);

                if (year >= 1900 && year <= 2100)
                {
                    return new DateTime(year, 1, 1);
                }
            }

            return null;
        }

        static bool IsValidDate(int year, int month, int day)
        {
            if (year < 1900 || year > 2100)
                return false;
            if (month < 1 || month > 12)
                return false;
            if (day < 1 || day > DateTime.DaysInMonth(year, month))
                return false;
            return true;
        }

        static DateTime? GetExifDate(string filePath)
        {
            try
            {
                using var image = Image.Load(filePath);
                var exifProfile = image.Metadata.ExifProfile;

                if (exifProfile == null)
                    return null;

                exifProfile.TryGetValue(ExifTag.DateTimeOriginal, out var dateTimeOriginal);
                if (dateTimeOriginal?.GetValue() is string dateString)
                {
                    if (DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss",
                        null, System.Globalization.DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        static void SetExifDate(string filePath, DateTime date)
        {
            try
            {
                // Load the image
                using (var image = Image.Load(filePath))
                {
                    // Get or create EXIF profile
                    var exifProfile = image.Metadata.ExifProfile ?? new ExifProfile();

                    // Format the date as required by EXIF: "YYYY:MM:DD HH:MM:SS"
                    string exifDateString = date.ToString("yyyy:MM:dd HH:mm:ss");

                    // Set EXIF date/time tags
                    exifProfile.SetValue(ExifTag.DateTime, exifDateString);
                    exifProfile.SetValue(ExifTag.DateTimeOriginal, exifDateString);
                    exifProfile.SetValue(ExifTag.DateTimeDigitized, exifDateString);

                    // Attach the profile back to the image
                    image.Metadata.ExifProfile = exifProfile;

                    // Save the image with the updated EXIF data
                    image.Save(filePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set EXIF date: {ex.Message}", ex);
            }
        }

        static void ProcessFileCopying(string directoryPath)
        {
            try
            {
                // Get all JPG files in the directory and subdirectories
                var jpgFiles = new List<string>();
                jpgFiles.AddRange(Directory.EnumerateFiles(directoryPath, "*.jpg", SearchOption.AllDirectories));
                jpgFiles.AddRange(Directory.EnumerateFiles(directoryPath, "*.jpeg", SearchOption.AllDirectories));

                Console.WriteLine($"Found {jpgFiles.Count} JPG files.");
                Console.WriteLine();

                // Create EXPORT folder
                string exportPath = Path.Combine(directoryPath, "EXPORT");
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                    Console.WriteLine($"Created EXPORT folder: {exportPath}");
                    Console.WriteLine();
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (string filePath in jpgFiles)
                {
                    try
                    {
                        // Skip files already in EXPORT folder
                        if (filePath.Contains(Path.Combine(directoryPath, "EXPORT")))
                        {
                            Console.WriteLine($"- Skipped (already in EXPORT): {filePath}");
                            continue;
                        }

                        // Get relative path from base directory
                        string relativePath = Path.GetRelativePath(directoryPath, filePath);

                        // Get directory part (without filename)
                        string? relativeDir = Path.GetDirectoryName(relativePath);

                        // Create new filename
                        string originalFileName = Path.GetFileName(filePath);
                        string newFileName;

                        if (!string.IsNullOrEmpty(relativeDir) && relativeDir != ".")
                        {
                            // Replace path separators with underscores
                            string folderPrefix = relativeDir.Replace(Path.DirectorySeparatorChar, '_')
                                                              .Replace(Path.AltDirectorySeparatorChar, '_');
                            newFileName = $"{folderPrefix}_{originalFileName}";
                        }
                        else
                        {
                            newFileName = originalFileName;
                        }

                        // Handle potential duplicate filenames
                        string destinationPath = Path.Combine(exportPath, newFileName);
                        int counter = 1;
                        while (File.Exists(destinationPath))
                        {
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
                            string extension = Path.GetExtension(newFileName);
                            destinationPath = Path.Combine(exportPath, $"{nameWithoutExt}_{counter}{extension}");
                            counter++;
                        }

                        // Copy the file (changed from Move)
                        File.Copy(filePath, destinationPath);
                        Console.WriteLine($"✓ Copied: {relativePath} -> {Path.GetFileName(destinationPath)}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error processing {filePath}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {successCount} copied, {errorCount} errors");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory: {ex.Message}");
            }
        }
    }
}
