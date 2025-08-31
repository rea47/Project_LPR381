using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Util
{
    /// Console logger for algorithm iterations/tableaux with a persistent footer.
    public sealed class IterationLog
    {
        public static double R3(double x) => Math.Round(x, 3, MidpointRounding.AwayFromZero);

        // Persistent notes printed under each tableau until cleared
        private string[] _footerNotes = null;

        public void SetFooter(params string[] notes)
        {
            _footerNotes = (notes == null || notes.Length == 0) ? null : notes;
        }
        public void ClearFooter() => _footerNotes = null;

        public void Title(string t)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + t.ToUpper() + " ===");
        }

        public void Note(string t) => Console.WriteLine(t);

        /// Prints an (m+1)×(n+1) tableau T (last row = z, last column = RHS).
        public void PrintTableau(string[] colNames, string[] rowNames, double[,] T, int step, string header = "")
        {
            int mTot = T.GetLength(0); // includes z row
            int nTot = T.GetLength(1); // includes RHS
            int m = mTot - 1;          // constraint rows
            int n = nTot - 1;          // decision columns

            Console.WriteLine($"\n--- Tableau (step {step}) {header} ---");

            var hdr = string.Join("  ", colNames.Select(s => s.PadLeft(8)));
            Console.WriteLine("         " + hdr + "  |    RHS");

            for (int i = 0; i < mTot; i++)
            {
                string rowName = (i < rowNames.Length ? rowNames[i] : (i == m ? "z" : $"r{i + 1}"));
                Console.Write((rowName + "   ").PadRight(9));

                for (int j = 0; j < n; j++)
                    Console.Write(R3(T[i, j]).ToString("0.###").PadLeft(8) + "  ");

                Console.Write("| " + R3(T[i, n]).ToString("0.###").PadLeft(6));
                Console.WriteLine();
            }

            if (_footerNotes != null && _footerNotes.Length > 0)
            {
                Console.WriteLine();
                foreach (var line in _footerNotes)
                    Console.WriteLine(line);
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