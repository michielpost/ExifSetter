using System;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace ExifSetter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EXIF Date Setter");
            Console.WriteLine("================");
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

            // Process the directory
            ProcessDirectory(directoryPath);

            Console.WriteLine();
            Console.WriteLine("Processing complete!");
        }

        static void ProcessDirectory(string directoryPath)
        {
            try
            {
                // Get all JPG files in the directory and subdirectories
                var jpgFiles = new List<string>();
                jpgFiles.AddRange(Directory.GetFiles(directoryPath, "*.jpg", SearchOption.AllDirectories));
                jpgFiles.AddRange(Directory.GetFiles(directoryPath, "*.jpeg", SearchOption.AllDirectories));
                jpgFiles.AddRange(Directory.GetFiles(directoryPath, "*.JPG", SearchOption.AllDirectories));
                jpgFiles.AddRange(Directory.GetFiles(directoryPath, "*.JPEG", SearchOption.AllDirectories));

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
                            Console.WriteLine($"✓ Set date {parsedDate.Value:yyyy-MM-dd} for: {Path.GetFileName(filePath)}");
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"- Skipped (no date found): {Path.GetFileName(filePath)}");
                            skipCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error processing {Path.GetFileName(filePath)}: {ex.Message}");
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
    }
}
