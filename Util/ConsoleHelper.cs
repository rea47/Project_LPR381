using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Util
{
    /// Helper class for console formatting and display operations
    public static class ConsoleHelper
    {
        /// Display welcome screen with ASCII art
        public static void ShowWelcomeScreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    ╔════════════════════════════════════════════════════════════════════════════╗
    ║                                                                            ║
    ║    ██████╗ ██████╗     ███████╗ ██████╗ ██╗    ██╗   ██╗███████╗██████╗    ║
    ║   ██╔═══██╗██╔══██╗    ██╔════╝██╔═══██╗██║    ██║   ██║██╔════╝██╔══██╗   ║
    ║   ██║   ██║██████╔╝    ███████╗██║   ██║██║    ██║   ██║█████╗  ██████╔╝   ║
    ║   ██║   ██║██╔══██╗    ╚════██║██║   ██║██║    ╚██╗ ██╔╝██╔══╝  ██╔══██╗   ║
    ║   ╚██████╔╝██║  ██║    ███████║╚██████╔╝███████╗╚████╔╝ ███████╗██║  ██║   ║
    ║    ╚═════╝ ╚═╝  ╚═╝    ╚══════╝ ╚═════╝ ╚══════╝ ╚═══╝  ╚══════╝╚═╝  ╚═╝   ║
    ║                                                                            ║
    ║                  Linear Programming Parser & Analyzer                      ║
    ║                               Version 1.0                                  ║
    ╚════════════════════════════════════════════════════════════════════════════╝
            ");
            Console.ResetColor();
            Console.WriteLine("\n         Welcome to the Operations Research Solver!");
            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }

        /// Display main menu with visual styling
        public static void ShowMainMenu(LinearProgrammingModel currentModel)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        MAIN MENU                              ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════╣");
            Console.ResetColor();

            Console.WriteLine("║  [1] Load Input File                                          ║");
            Console.WriteLine("║  [2] View Current Model                                       ║");
            Console.WriteLine("║  [3] Export Results to File                                   ║");
            Console.WriteLine("║  [4] About                                                    ║");
            Console.WriteLine("║  [5] Quit                                                     ║");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            if (currentModel != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Current Model: {currentModel.ObjectiveType} with {currentModel.Variables.Count} variables, {currentModel.Constraints.Count} constraints");
                if (currentModel.HasErrors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Warning: Model contains errors - check output for details");
                }
                Console.ResetColor();
            }

            Console.Write("\nPlease select an option: ");
        }

        /// <summary>
        /// Show about information
        /// </summary>
        public static void ShowAbout()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         ABOUT                                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Operations Research Solver v1.0");
            Console.WriteLine("Linear Programming Model Parser");
            Console.WriteLine("Comprehensive input file processing tool\n");

            Console.WriteLine("Core Features:");
            Console.WriteLine("   • Flexible input file parsing");
            Console.WriteLine("   • Handles variable numbers of variables and constraints");
            Console.WriteLine("   • Comprehensive error detection and reporting");
            Console.WriteLine("   • Canonical form generation");
            Console.WriteLine("   • Detailed output file creation\n");

            Console.WriteLine("Supported Input Format:");
            Console.WriteLine("   Line 1: Objective function (MAX/MIN followed by coefficients)");
            Console.WriteLine("   Lines 2-n: Constraints (coefficients relation RHS)");
            Console.WriteLine("   Last line: Sign restrictions (+, -, urs, int, bin)\n");

            Console.WriteLine("Error Detection:");
            Console.WriteLine("   • Invalid file format detection");
            Console.WriteLine("   • Missing or malformed data identification");
            Console.WriteLine("   • Inconsistent variable counts");
            Console.WriteLine("   • Invalid sign restriction formats");
        }

        /// Display colored status message
        public static void DisplayStatusMessage(string message, bool isError = false)
        {
            Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(isError ? $"{message}" : $"{message}");
            Console.ResetColor();
        }

        /// Display warning message
        public static void DisplayWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }
    }
}