using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Util
{
    /// Helper class for file operations with error handling
    public static class FileHelper
    {
        /// Check if file exists with proper error handling
        public static bool FileExists(string filePath)
        {
            try
            {
                return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        /// Write content to file with comprehensive error handling
        public static void WriteToFile(string filePath, string content)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, content);
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Access denied writing to: {filePath}");
            }
            catch (DirectoryNotFoundException)
            {
                throw new InvalidOperationException($"Directory not found for: {filePath}");
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"IO Error writing file: {ex.Message}");
            }
        }

        /// Get file size safely
        public static long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// Read all text from file with error handling
        public static string ReadAllText(string filePath)
        {
            if (!FileExists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            try
            {
                return File.ReadAllText(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Access denied reading from: {filePath}");
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"IO Error reading file: {ex.Message}");
            }
        }

        /// Get safe file name from user input
        public static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "output.txt";
            }

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }
}