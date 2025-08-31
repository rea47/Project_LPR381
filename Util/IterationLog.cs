using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Project_LPR381.Util
{
    public class IterationLog
    {
        private readonly StringBuilder _buffer; // Only one buffer for all output
        private string[] _footer;

        /// <summary>
        /// Constructor that accepts a single StringBuilder to capture all output.
        /// </summary>
        public IterationLog(StringBuilder buffer = null)
        {
            _buffer = buffer;
        }

        /// <summary>
        /// Central logging method to write to both the console and the single buffer.
        /// </summary>
        private void Log(string message)
        {
            Console.WriteLine(message);
            _buffer?.AppendLine(message);
        }

        public void Title(string title)
        {
            Log($"\n--- {title} ---");
        }

        public void Note(string note)
        {
            Log(note);
        }

        public void SetFooter(string[] lines)
        {
            _footer = lines;
        }

        public void ClearFooter()
        {
            _footer = null;
        }

        public static string R3(double v) => v.ToString("0.###");

        public void PrintTableau(string[] colNames, string[] rowNames, double[,] T, int step, string note = null)
        {
            string tableauString;
            using (var sw = new StringWriter())
            {
                sw.WriteLine();
                if (!string.IsNullOrEmpty(note))
                    sw.WriteLine($"Step {step}: {note}");
                else
                    sw.WriteLine($"Step {step}");

                int m = T.GetLength(0) - 1;
                int n = T.GetLength(1) - 1;
                string[] allCols = new[] { "Basic" }.Concat(colNames).Concat(new[] { "RHS" }).ToArray();
                int[] widths = allCols.Select(s => s.Length).ToArray();
                for (int i = 0; i < m + 1; i++)
                {
                    string rName = (i < m) ? rowNames[i] : "z";
                    widths[0] = Math.Max(widths[0], rName.Length);
                }
                for (int j = 0; j < n + 1; j++)
                    for (int i = 0; i < m + 1; i++)
                        widths[j + 1] = Math.Max(widths[j + 1], R3(T[i, j]).Length);
                for (int j = 0; j < widths.Length; j++) sw.Write(allCols[j].PadRight(widths[j] + 2));
                sw.WriteLine();
                for (int j = 0; j < widths.Length; j++) sw.Write(new string('-', widths[j]).PadRight(widths[j] + 2));
                sw.WriteLine();
                for (int i = 0; i < m; i++)
                {
                    sw.Write(rowNames[i].PadRight(widths[0] + 2));
                    for (int j = 0; j <= n; j++) sw.Write(R3(T[i, j]).PadRight(widths[j + 1] + 2));
                    sw.WriteLine();
                }
                sw.Write("z".PadRight(widths[0] + 2));
                for (int j = 0; j <= n; j++) sw.Write(R3(T[m, j]).PadRight(widths[j + 1] + 2));
                sw.WriteLine();
                if (_footer != null)
                {
                    foreach (var line in _footer) sw.WriteLine(line);
                }
                sw.WriteLine();
                tableauString = sw.ToString();
            }

            // Log the entire formatted tableau to the single buffer.
            Log(tableauString);
        }

        public void PrintVector(string title, string[] names, double[] values)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            for (int i = 0; i < values.Length; ++i)
            {
                sb.AppendLine($"  {(i < names.Length ? names[i] : "var" + i)}: {R3(values[i])}");
            }
            Log(sb.ToString());
        }
    }
}