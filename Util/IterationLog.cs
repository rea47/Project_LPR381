using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Util
{
    public sealed class IterationLog
    {
        public static double R3(double x) => Math.Round(x, 3, MidpointRounding.AwayFromZero);

        public void Title(string t)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + t.ToUpper() + " ===");
        }

        public void Note(string t) => Console.WriteLine(t);

        public void PrintTableau(string[] colNames, string[] rowNames, double[,] T, int step, string extra = "")
        {
            Console.WriteLine($"\n--- Tableau (step {step}) {extra} ---");
            int m = T.GetLength(0);
            int n = T.GetLength(1);
            var hdr = string.Join("  ", colNames.Select(s => s.PadLeft(8)));
            Console.WriteLine("         " + hdr + "  |    RHS");
            for (int i = 0; i < m; i++)
            {
                Console.Write((rowNames[i] + "   ").PadRight(9));
                for (int j = 0; j < n - 1; j++)
                    Console.Write(R3(T[i, j]).ToString("0.###").PadLeft(8) + "  ");
                Console.Write("| " + R3(T[i, n - 1]).ToString("0.###").PadLeft(6));
                Console.WriteLine();
            }
        }

        public void PrintVector(string title, string[] names, double[] v)
        {
            Console.WriteLine($"\n{title}");
            for (int i = 0; i < v.Length; i++)
                Console.WriteLine($"  {names[i],-10} = {R3(v[i])}");
        }
    }
}
