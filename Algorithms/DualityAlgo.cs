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
                //Console.WriteLine("1. Apply Duality to Programming Model");
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
            Console.Clear();
            Console.WriteLine("=== Apply Duality ==="); //just going to wait to call from LPModels

            // Example: primal (max c^T x, Ax <= b)
            // Dual: (min b^T y, A^T y >= c, y >= 0)

            Console.WriteLine("Enter number of constraints (m): ");
            int m = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter number of variables (n): ");
            int n = int.Parse(Console.ReadLine());

            Console.WriteLine("Primal coefficients (c vector): ");
            double[] c = new double[n];
            for (int i = 0; i < n; i++)
            {
                Console.Write($"c[{i + 1}]: ");
                c[i] = double.Parse(Console.ReadLine());
            }

            Console.WriteLine("\nPrimal constraints matrix (A): ");
            double[,] A = new double[m, n];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    Console.Write($"A[{i + 1},{j + 1}]: ");
                    A[i, j] = double.Parse(Console.ReadLine());
                }
            }

            Console.WriteLine("\nRight-hand side vector (b): ");
            double[] b = new double[m];
            for (int i = 0; i < m; i++)
            {
                Console.Write($"b[{i + 1}]: ");
                b[i] = double.Parse(Console.ReadLine());
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
