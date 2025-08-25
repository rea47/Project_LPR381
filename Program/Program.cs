using Project_LPR31.Core;
using Project_LPR31.Exceptions;
using Project_LPR381;
using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Linq;
using System.Text;

namespace Project_LPR381
{
    /// Main program class for Operations Research Linear Programming Solver
    /// Handles menu interface, file operations, and program flow
    class Program
    {
        private static LinearProgrammingModel currentModel;
        private static string outputFilePath = "output.txt";
        private static StringBuilder outputBuffer = new StringBuilder();

        static void Main(string[] args)
        {
            Console.Title = "Operations Research Solver v1.0";
            ConsoleHelper.ShowWelcomeScreen();

            while (true)
            {
                ConsoleHelper.ShowMainMenu(currentModel);
                string choice = Console.ReadLine();

                switch (choice?.ToUpper())
                {
                    case "1":
                        LoadInputFile();
                        break;
                    case "2":
                        ViewCurrentModel();
                        break;
                    case "3":
                        ExportResults();
                        break;
                    case "4":
                        ConsoleHelper.ShowAbout();
                        break;
                    case "5":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        /// Load and parse input file with comprehensive error handling
        private static void LoadInputFile()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      LOAD INPUT FILE                          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.Write("Enter input file path: ");
            string filePath = Console.ReadLine();

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("No file path provided!");
                return;
            }

            if (!FileHelper.FileExists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                var parser = new InputFileParser();
                currentModel = parser.ParseFile(filePath);

                // Generate output content
                outputBuffer.Clear();
                var outputGenerator = new OutputFileGenerator();
                var outputContent = outputGenerator.GenerateOutput(currentModel, filePath);
                outputBuffer.Append(outputContent);

                Console.WriteLine("File loaded successfully!");
                DisplayModelSummary(currentModel);

                if (currentModel.ParsingErrors.Count > 0)
                {
                    Console.WriteLine($"\n{currentModel.ParsingErrors.Count} parsing warnings detected");
                    Console.WriteLine("Check the output file for detailed error information");
                }
            }
            catch (InvalidModelException ex)
            {
                Console.WriteLine($"Invalid model format: {ex.Message}");
                outputBuffer.Clear();
                outputBuffer.AppendLine($"CRITICAL ERROR");
                outputBuffer.AppendLine($"==============");
                outputBuffer.AppendLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                outputBuffer.Clear();
                outputBuffer.AppendLine($"UNEXPECTED ERROR");
                outputBuffer.AppendLine($"================");
                outputBuffer.AppendLine($"Error: {ex.Message}");
            }
        }

        /// Display comprehensive model summary
        private static void DisplayModelSummary(LinearProgrammingModel model)
        {
            Console.WriteLine($"Model type: {model.ObjectiveType}");
            Console.WriteLine($"Variables: {model.Variables.Count}");
            Console.WriteLine($"Constraints: {model.Constraints.Count}");

            Console.WriteLine("\nModel Summary:");

            // Display objective function
            var objTerms = model.ObjectiveCoefficients
                .Select((c, i) => $"{(c >= 0 && i > 0 ? "+" : "")}{c:F3}x{i + 1}")
                .ToArray();
            Console.WriteLine($"Objective: {model.ObjectiveType} {string.Join(" ", objTerms)}");

            // Display constraints
            Console.WriteLine("Constraints:");
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];
                var coeffs = constraint.Coefficients
                    .Select((c, j) => $"{(c >= 0 && j > 0 ? "+" : "")}{c:F3}x{j + 1}")
                    .ToArray();
                Console.WriteLine($"  {string.Join(" ", coeffs)} {constraint.Relation} {constraint.RightHandSide:F3}");
            }

            // Display sign restrictions
            var restrictions = model.SignRestrictions
                .Select((s, i) => $"x{i + 1}: {s}")
                .ToArray();
            Console.WriteLine($"Sign Restrictions: {string.Join(", ", restrictions)}");

            // Display model analysis
            Console.WriteLine($"\nModel Analysis:");
            Console.WriteLine($"• Integer Programming: {(model.IsIntegerProgramming() ? "Yes" : "No")}");
            Console.WriteLine($"• Binary Programming: {(model.IsBinaryProgramming() ? "Yes" : "No")}");
            Console.WriteLine($"• Mixed Variables: {(model.HasMixedVariables() ? "Yes" : "No")}");
        }

        /// View current model details with error information
        private static void ViewCurrentModel()
        {
            if (currentModel == null)
            {
                Console.WriteLine("No model loaded.");
                return;
            }

            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     CURRENT MODEL                            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            DisplayModelSummary(currentModel);

            // Show any errors or warnings
            if (currentModel.ParsingErrors.Count > 0)
            {
                Console.WriteLine("\nParsing Errors and Warnings:");
                foreach (var error in currentModel.ParsingErrors)
                {
                    Console.WriteLine($"• {error}");
                }
            }

            // Show model validation status
            Console.WriteLine($"\nModel Status:");
            Console.WriteLine($"• Valid Format: {(!currentModel.HasErrors ? "Yes" : "No")}");
            Console.WriteLine($"• Ready for Solving: {(currentModel.IsValidForSolving() ? "Yes" : "No")}");
        }

        /// Export results to output file with comprehensive formatting
        private static void ExportResults()
        {
            if (outputBuffer.Length == 0)
            {
                Console.WriteLine("No results to export. Please load a model first.");
                return;
            }

            Console.Write($"Enter output file path (default: {outputFilePath}): ");
            string filePath = Console.ReadLine();

            if (string.IsNullOrEmpty(filePath))
                filePath = outputFilePath;

            try
            {
                FileHelper.WriteToFile(filePath, outputBuffer.ToString());
                Console.WriteLine($"Results exported to {filePath}");
                Console.WriteLine($"File size: {FileHelper.GetFileSize(filePath)} bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting results: {ex.Message}");
            }
        }
    }
}