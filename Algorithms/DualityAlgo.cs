using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR31.Algorithms
{
    class Program
    {
        static void Main(string[] args)
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("  Linear Programming Duality Solver");
                Console.WriteLine("1. Apply Duality to Programming Model");
                Console.WriteLine("2. Solve Dual Programming Model");
                Console.WriteLine("3. Verify Strong/Weak Duality");
                Console.WriteLine("4. Exit");

                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        ApplyDuality();
                        break;
                    case "2":
                        SolveDualModel();
                        break;
                    case "3":
                        VerifyDuality();
                        break;
                    case "4":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice, press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void ApplyDuality()
        {
            LinearProgrammingModel lpm = new LinearProgrammingModel();
            Console.Clear();
            bool valid = true;
            Console.WriteLine("Is this the correct LP Model? (Y/N)");
            while (!valid)
            {

                string srt = "| \t"; //The LP top row for variable names and stuff
                string cols = String.Join("| ", lpm.Variables);
                string srtcols = srt + cols;
                string rest = "| Sign | RHS";
                string full = srtcols + rest + " |";
                Console.WriteLine(full);

                string obj = "| "+lpm.ObjectiveType + " Z"; //first row of the LP Model which is the objective function
                string coeffs = String.Join("| ", lpm.ObjectiveCoefficients);
                string objcoeffs = obj + coeffs;
                string restr1 = "| \t| \t";
                string fullr1 = objcoeffs + restr1 + " |";
                Console.WriteLine(fullr1);

                for (int i = 1; i < lpm.Constraints.Count+1; i++) //rest of the rows which is constraints as well as Xs being >0 and whatnot
                {
                    string csrt = "|\t" + i.ToString();
                    string ccols = String.Join("| ", lpm.Constraints[i-1].Coefficients);
                    string csrtcols = csrt + ccols;
                    string signandrhs = "|" + lpm.SignRestrictions[i-1]+"\t |\t";
                }

                string res =  Console.ReadLine().ToUpper();
                switch (res)
                {
                    case "Y":
                        //duality results here
                        break;
                    case "N":
                        Console.WriteLine("Returning to main menu...");
                        //go to main menu where they upload or put the correct stuff in.
                        return;
                    default:
                        Console.WriteLine("Please enter a valid response.");
                        Console.ReadKey();
                        break;
                }
            }



            Console.WriteLine("\n--- Dual Model ---");
            Console.WriteLine("Minimize: b^T y");
            Console.WriteLine("Subject to: A^T y >= c, y >= 0");

            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
        }

        static void SolveDualModel()
        {
            Console.Clear();
            Console.WriteLine("=== Solve Dual Model ===");
            // Placeholder: here you’d implement simplex or any solver for dual.
            Console.WriteLine("Solver not yet implemented.");
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
        }

        static void VerifyDuality()
        {
            Console.Clear();
            Console.WriteLine("=== Verify Strong/Weak Duality ===");
            // Placeholder: compare primal and dual optimal values
            Console.WriteLine("Verification not yet implemented.");
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
        }
    }

}
