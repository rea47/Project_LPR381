using Project_LPR381;
using Project_LPR381.Algorithms;
using Project_LPR381.Core;
using Project_LPR381.Exceptions;
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
                        ShowAlgorithmsMenu();
                        break;
                    case "4":
                        ShowDualityMenu();
                        break;
                    case "5":
                        ShowSensitivityMenu();
                        break;
                    case "6":
                        ExportResults();
                        break;
                    case "7":
                        ConsoleHelper.ShowAbout();
                        break;
                    case "8":
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

                var generator = new OutputFileGenerator();
                string reportContent = generator.GenerateOutput(currentModel, filePath);
                outputBuffer.Clear();
                outputBuffer.Append(reportContent);

                Console.WriteLine("File loaded successfully!");
                DisplayModelSummary(currentModel);


                if (currentModel.ParsingErrors.Any(e => !e.StartsWith("Info:")))
                {
                    Console.WriteLine($"\n{currentModel.ParsingErrors.Count(e => !e.StartsWith("Info:"))} parsing warnings detected:");
                    foreach (var error in currentModel.ParsingErrors.Where(e => !e.StartsWith("Info:")))
                    {
                        Console.WriteLine($"• {error}");
                    }
                    Console.WriteLine("Check the output file for detailed error information");
                }
                else
                {
                    Console.WriteLine("\nModel parsed successfully with no non-info warnings.");
                }
                Console.WriteLine($"Model valid for solving: {currentModel.IsValidForSolving()}");
            }
            catch (FormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n--- PARSING FAILED ---");
                Console.WriteLine("A number in your input file is incorrectly formatted.");
                Console.WriteLine("Common causes: using a comma (,) instead of a period (.) for decimals, or accidental letters.");
                Console.WriteLine($"\nDetailed Error: {ex.Message}");
                Console.ResetColor();

                outputBuffer.Clear();
                outputBuffer.AppendLine("MODEL PARSING FAILED");
                outputBuffer.AppendLine("====================");
                outputBuffer.AppendLine("Reason: A number in the input file could not be read.");
                outputBuffer.AppendLine("Please check for typos, commas used as decimal separators, or other non-numeric characters.");
                outputBuffer.AppendLine($"\nDetailed Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                outputBuffer.Clear();
                outputBuffer.AppendLine($"UNEXPECTED ERROR");
                outputBuffer.AppendLine($"================");
                outputBuffer.AppendLine($"Error: {ex.Message}");
            }
        }

        /// Displays the sub-menu for algorithms and handles user selection.
        private static void ShowAlgorithmsMenu()
        {
            if (currentModel == null)
            {
                Console.WriteLine("No model loaded. Please load a model first.");
                return;
            }

            bool back = false;
            while (!back)
            {
                ConsoleHelper.ShowAlgorithmsMenu();
                string choice = Console.ReadLine();
                var log = new IterationLog();

                switch (choice)
                {
                    case "1":
                        var primalSimplex = new PrimalSimplex();
                        primalSimplex.Solve(currentModel, log);
                        break;
                    case "2":
                        var revisedSimplex = new RevisedSimplex();
                        revisedSimplex.Solve(currentModel, log);
                        break;
                    case "3":
                        var cuttingPlane = new CuttingPlaneS();
                        cuttingPlane.Solve(currentModel, log);
                        break;
                    case "4":
                        var bbSimplex = new BranchAndBoundSimplex();
                        bbSimplex.Solve(currentModel, log);
                        break;
                    case "5":
                        // Knapsack requires a different model type, so we'll run a demo.
                        var knapsackSolver = new BranchAndBoundKnapsack();
                        var knapsackModel = new KnapsackModel
                        {
                            Values = new double[] { 10, 40, 30, 50 },
                            Weights = new double[] { 5, 4, 6, 3 },
                            Capacity = 10
                        };

                        // Ensure Solve matches the correct signature
                        knapsackSolver.Solve(knapsackModel, log);

                        break;

                    case "6":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        Console.ReadKey();
                        break;
                }

                if (!back)
                {
                    Console.WriteLine("\nPress any key to return to the algorithms menu...");
                    Console.ReadKey();
                }
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
            Console.WriteLine("║                     CURRENT MODEL                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            DisplayModelSummary(currentModel);

            // Show any errors or warnings
            // Get a list of actual errors and warnings, ignoring informational messages
            var actualErrors = currentModel.ParsingErrors
                .Where(e => !e.StartsWith("Info:"))
                .ToList();

            // Only show the section if there are actual errors or warnings to display
            if (actualErrors.Any())
            {
                Console.WriteLine("\nParsing Errors and Warnings:");
                foreach (var error in actualErrors)
                {
                    Console.WriteLine($"• {error}");
                }
            }

            // Show model validation status
            Console.WriteLine($"\nModel Status:");
            Console.WriteLine($"• Valid Format: {(!currentModel.ParsingErrors.Any(e => e.StartsWith("Warning:") || e.StartsWith("Error:")) ? "Yes" : "No")}"); 
            Console.WriteLine($"• Ready for Solving: {(currentModel.IsValidForSolving() ? "Yes" : "No")}");
        }

        /// Run duality analysis on the current model
        private static void ShowDualityMenu()
        {
            bool back = false;
            var duality = new Project_LPR31.Algorithms.DualityAlgo();

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Duality Analysis Menu ===");
                Console.WriteLine("1. Apply Duality");
                Console.WriteLine("2. Solve Dual Model");
                Console.WriteLine("3. Verify Strong/Weak Duality");
                Console.WriteLine("4. Back to Main Menu");
                Console.Write("Select option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        if (currentModel == null)
                        {
                            Console.WriteLine("No model loaded.");
                        }
                        else
                        {
                            duality.ApplyDuality(currentModel);
                        }
                        Console.WriteLine("\nReturning to Duality Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "2":
                        duality.SolveDualModel();
                        Console.WriteLine("\nReturning to Duality Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "3":
                        duality.VerifyDuality();
                        Console.WriteLine("\nReturning to Duality Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "4":
                        back = true;
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Returning...");
                        System.Threading.Thread.Sleep(1000);
                        break;
                }
            }
        }

        private static void ShowSensitivityMenu()
        {
            if (currentModel == null)
            {
                Console.WriteLine("No model loaded. Please load a model first.");
                System.Threading.Thread.Sleep(1500);
                return;
            }

            // Dummy solution for now
            var dummySolution = new Project_LPR381.Models.SolutionResult
            {
                IsOptimal = true,
                ObjectiveValue = 123.45,
                VariableValues = new double[currentModel.Variables.Count]
            };

            var sensitivity = new Project_LPR31.Algorithms.SensitivityAnalysis(dummySolution, currentModel);
            bool back = false;

            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Sensitivity Analysis Menu ===");
                Console.WriteLine("1. Compute ranges for variables");
                Console.WriteLine("2. Compute ranges for constraints");
                Console.WriteLine("3. Compute shadow prices");
                Console.WriteLine("4. Apply coefficient change");
                Console.WriteLine("5. Apply RHS change");
                Console.WriteLine("6. Back to Main Menu");
                Console.Write("Select option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        sensitivity.ComputeRangesForVariables();
                        Console.WriteLine("\nReturning to Sensitivity Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "2":
                        sensitivity.ComputeRangesForConstraints();
                        Console.WriteLine("\nReturning to Sensitivity Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "3":
                        sensitivity.ComputeShadowPrices();
                        Console.WriteLine("\nReturning to Sensitivity Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "4":
                        Console.Write("Enter variable index: ");
                        int vi = int.Parse(Console.ReadLine());
                        Console.Write("Enter new coefficient: ");
                        double coeff = double.Parse(Console.ReadLine());
                        sensitivity.ApplyChangeToCoefficient(vi, coeff);
                        Console.WriteLine("\nReturning to Sensitivity Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "5":
                        Console.Write("Enter constraint index: ");
                        int ci = int.Parse(Console.ReadLine());
                        Console.Write("Enter new RHS value: ");
                        double rhs = double.Parse(Console.ReadLine());
                        sensitivity.ApplyChangeToRHS(ci, rhs);
                        Console.WriteLine("\nReturning to Sensitivity Menu...");
                        System.Threading.Thread.Sleep(1500);
                        break;

                    case "6":
                        back = true;
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Returning...");
                        System.Threading.Thread.Sleep(1000);
                        break;
                }
            }
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

            // Simple file writing, assuming FileHelper exists
            try
            {
                System.IO.File.WriteAllText(filePath, outputBuffer.ToString());
                Console.WriteLine($"Results exported to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting results: {ex.Message}");
            }
        }
    }
}