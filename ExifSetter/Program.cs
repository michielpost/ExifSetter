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
            Console.WriteLine("2. Move and rename files to EXPORT folder");
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
                ProcessFileMoving(directoryPath);
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

                int successCount = 0;
                int skipCount = 0;
                int errorCount = 0;

                foreach (string filePath in jpgFiles)
                {
                    try
                    {
                        // Try to parse date from the file path
                        DateTime? parsedDate = ParseDateFromPath(filePath);

                        if (parsedDate.HasValue)
                        {
                            // Set the EXIF date
                            SetExifDate(filePath, parsedDate.Value);
                            Console.WriteLine($"✓ Set date {parsedDate.Value:yyyy-MM-dd} for: {filePath}");
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"- Skipped (no date found): {filePath}");
                            skipCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error processing {filePath}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {successCount} updated, {skipCount} skipped, {errorCount} errors");
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

        static void ProcessFileMoving(string directoryPath)
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

                        // Move the file
                        File.Move(filePath, destinationPath);
                        Console.WriteLine($"✓ Moved: {relativePath} -> {Path.GetFileName(destinationPath)}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error processing {filePath}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {successCount} moved, {errorCount} errors");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory: {ex.Message}");
            }
        }
    }
}
