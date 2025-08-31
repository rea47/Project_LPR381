using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Util
{
    /// Console logger for algorithm iterations (tableaux, vectors, notes).
    /// Numbers are rounded to 3 d.p. via R3().
    public sealed class IterationLog
    {
        public static double R3(double x)
            => Math.Round(x, 3, MidpointRounding.AwayFromZero);

        public void Title(string t)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + (t ?? string.Empty).ToUpper() + " ===");
        }

        public void Note(string t) => Console.WriteLine(t ?? string.Empty);

        /// <summary>
        /// Prints a simplex tableau where T has size (m+1 x n+1):
        ///  - last row is the objective (z)
        ///  - last column is the RHS
        /// rowNames should have length m (constraint rows only).
        /// colNames should have length n (all columns except RHS).
        /// Bounds-safe even if the name arrays are missing/short.
        /// </summary>
        public void PrintTableau(string[] colNames, string[] rowNames, double[,] T, int step, string extra = "")
        {
            Console.WriteLine($"\n--- Tableau (step {step}) {extra} ---");

            if (T == null) { Console.WriteLine("(no tableau)"); return; }

            int rows = T.GetLength(0); // includes objective
            int cols = T.GetLength(1); // includes RHS
            if (rows <= 0 || cols <= 0) { Console.WriteLine("(empty tableau)"); return; }

            int cons = rows - 1;       // constraint rows
            int vars = cols - 1;       // variable columns (excl RHS)

            // Build safe column names of exact length 'vars'
            string[] safeCols = new string[vars];
            for (int j = 0; j < vars; j++)
                safeCols[j] = (colNames != null && j < colNames.Length && colNames[j] != null)
                    ? colNames[j]
                    : "x" + (j + 1);

            string header = string.Join("  ", safeCols.Select(s => s.PadLeft(8)));
            Console.WriteLine("         " + header + "  |    RHS");

            // Constraint rows
            for (int i = 0; i < cons; i++)
            {
                string rn = (rowNames != null && i < rowNames.Length && rowNames[i] != null)
                    ? rowNames[i]
                    : "r" + (i + 1);

                Console.Write((rn + " ").PadRight(9));

                for (int j = 0; j < vars; j++)
                    Console.Write(R3(T[i, j]).ToString("0.###").PadLeft(8) + "  ");

                Console.Write("| " + R3(T[i, cols - 1]).ToString("0.###").PadLeft(6));
                Console.WriteLine();
            }

            // Objective row (z)
            Console.Write("z".PadRight(9));
            for (int j = 0; j < vars; j++)
                Console.Write(R3(T[rows - 1, j]).ToString("0.###").PadLeft(8) + "  ");
            Console.Write("| " + R3(T[rows - 1, cols - 1]).ToString("0.###").PadLeft(6));
            Console.WriteLine();
        }

        public void PrintVector(string title, string[] names, double[] v)
        {
            Console.WriteLine($"\n{title}");
            if (v == null || v.Length == 0) { Console.WriteLine("(empty)"); return; }

            for (int i = 0; i < v.Length; i++)
            {
                string n = (names != null && i < names.Length && names[i] != null)
                    ? names[i]
                    : "v" + (i + 1);
                Console.WriteLine($"  {n,-10} = {R3(v[i])}");
            }
        }
    }
}
