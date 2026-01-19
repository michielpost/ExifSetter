# ExifSetter
Set exif date for old digitized photos based on date in file or folder name

## Description

ExifSetter is a C# console application that automatically sets EXIF date metadata for JPG files based on dates found in their file or directory names. This is particularly useful for organizing and dating old digitized photos that may not have proper EXIF data.

## Features

- Recursively scans directories for JPG files (*.jpg and *.jpeg)
- Parses dates from file and directory names in multiple formats:
  - `YYYY` (e.g., `1980` → becomes `1980-01-01`)
  - `YYYY-MM` (e.g., `1980-05` → becomes `1980-05-01`)
  - `YYYY-MM-DD` (e.g., `1980-05-12` → stays `1980-05-12`)
- Automatically fills missing date components with `01`
- Sets EXIF DateTime, DateTimeOriginal, and DateTimeDigitized fields
- Provides progress feedback and summary statistics

## Requirements

- .NET 10.0 or later
- Visual Studio 2022 or later (for development)

## Building

```bash
dotnet build
```

## Usage

Run the application:

```bash
dotnet run --project ExifSetter/ExifSetter.csproj
```

Or run the compiled executable:

```bash
cd ExifSetter/bin/Debug/net10.0
./ExifSetter
```

The application will prompt you for a directory path. Enter the path to the directory containing your photos, and the application will:

1. Scan for all JPG files recursively
2. Parse dates from file and directory names
3. Set EXIF dates for files where dates are found
4. Display progress and summary

## Example

Given a directory structure like:

```
Photos/
  1980 Family Photos/
    photo1.jpg
    photo2.jpg
  1985-07 Summer Vacation/
    beach.jpg
  1990-12-25 Christmas/
    presents.jpg
  1982 oldphoto.jpg
  nodatephoto.jpg
```

The application will:
- Set `1980-01-01` for photos in "1980 Family Photos/"
- Set `1985-07-01` for photos in "1985-07 Summer Vacation/"
- Set `1990-12-25` for photos in "1990-12-25 Christmas/"
- Set `1982-01-01` for "1982 oldphoto.jpg"
- Skip "nodatephoto.jpg" (no date found)

## Date Format Requirements

- Dates must be at the beginning of the file or directory name
- Must be followed by a space or underscore
- Valid year range: 1900-2100
- Supports formats: `YYYY`, `YYYY-MM`, `YYYY-MM-DD`

## Dependencies

- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/) - For EXIF data manipulation

## License

See LICENSE file for details.

