using System;
using OperationsResearchSolver;

namespace Project_LPR381
{
    class Program
    {
        // Store the loaded model in memory
        private static LpModel currentModel = null;

        static void Main(string[] args)
        {
            Console.Title = "Operations Research Solver";
            bool keepRunning = true;

            while (keepRunning)
            {
                DisplayMainMenu();
                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        LoadModelFromFile();
                        break;
                    case "2":
                        SelectAlgorithmToSolve();
                        break;
                    case "3":
                        Console.WriteLine("\nSensitivity Analysis menu is not yet implemented.");
                        Pause();
                        break;
                    case "4":
                        keepRunning = false;
                        break;
                    default:
                        Console.WriteLine("\nInvalid option. Please try again.");
                        Pause();
                        break;
                }
            }
            Console.WriteLine("\nExiting program. Goodbye!");
        }

        private static void DisplayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("=====================================");
            Console.WriteLine("  Operations Research Solver Menu");
            Console.WriteLine("=====================================");
            if (currentModel != null)
            {
                Console.WriteLine($"  Model Loaded: {currentModel.VariableCount} variables, {currentModel.ConstraintCount} constraints.");
            }
            else
            {
                Console.WriteLine("  No model loaded.");
            }
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("  1. Load Model from Input File");
            Console.WriteLine("  2. Select Algorithm to Solve");
            Console.WriteLine("  3. Perform Sensitivity Analysis");
            Console.WriteLine("  4. Exit");
            Console.WriteLine("-------------------------------------");
            Console.Write("Enter your choice: ");
        }

        private static void LoadModelFromFile()
        {
            Console.Write("\nEnter the path to the input file (e.g., model.txt): ");
            string filePath = Console.ReadLine();

            try
            {
                currentModel = ModelParser.ParseFile(filePath);
                Console.WriteLine("\nSuccess! Model parsed and loaded.");
                Console.WriteLine($" -> Variables: {currentModel.VariableCount}");
                Console.WriteLine($" -> Constraints: {currentModel.ConstraintCount}");
            }
            catch (Exception ex)
            {
                // Catch any errors from the parser (file not found, format errors, etc.)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError loading model: {ex.Message}");
                Console.ResetColor();
                currentModel = null; // Ensure no partially loaded model is kept
            }
            Pause();
        }

        private static void SelectAlgorithmToSolve()
        {
            if (currentModel == null)
            {
                Console.WriteLine("\nPlease load a model first (Option 1).");
                Pause();
                return;
            }

            Console.Clear();
            Console.WriteLine("--- Select an Algorithm ---");
            Console.WriteLine("1. Primal Simplex Algorithm");
            Console.WriteLine("2. Branch and Bound Simplex Algorithm");
            // Add other algorithms here as they are implemented
            Console.WriteLine("---------------------------");
            Console.Write("Enter your choice: ");
            string choice = Console.ReadLine();

            Console.Write("\nEnter the path for the output file (e.g., results.txt): ");
            string outputPath = Console.ReadLine();

            SolveResult result;

            // This is where the actual algorithm implementations will be called.
            // We use the stubs for now.
            switch (choice)
            {
                case "1":
                    Console.WriteLine("\nSolving with Primal Simplex (DEMO)...");
                    result = PrimalSimplexSolver.Solve(currentModel, outputPath);
                    HandleSolveResult(result);
                    break;
                case "2":
                    Console.WriteLine("\nSolving with Branch and Bound (DEMO)...");
                    result = BranchAndBoundSolver.Solve(currentModel, outputPath);
                    HandleSolveResult(result);
                    break;
                default:
                    Console.WriteLine("\nInvalid algorithm choice.");
                    break;
            }
            Pause();
        }

        /// Handles reporting special cases like infeasible or unbounded models.
        /// The actual detection is the responsibility of the algorithm.
        private static void HandleSolveResult(SolveResult result)
        {
            switch (result)
            {
                case SolveResult.Optimal:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nModel solved. Check the output file for detailed iterations.");
                    Console.ResetColor();
                    break;
                case SolveResult.Infeasible:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nProcessing complete. The model was identified as INFEASIBLE.");
                    Console.ResetColor();
                    break;
                case SolveResult.Unbounded:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nProcessing complete. The model was identified as UNBOUNDED.");
                    Console.ResetColor();
                    break;
            }
        }

        private static void Pause()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}