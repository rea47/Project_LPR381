using System.Text;
using System.IO;
using OperationsResearchSolver;

namespace Project_LPR381
{
    /// Handles writing all formatted output to the specified text file.
    public static class OutputWriter
    {
        /// Writes the canonical form of the model to the output file.
        /// NOTE: This is a simplified representation. The actual conversion logic
        /// would be part of the algorithm implementation.
        public static void WriteCanonicalForm(LpModel model, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Canonical Form ---");

            // Objective Function
            sb.Append(model.Type == ObjectiveType.Max ? "Maximize Z = " : "Minimize Z = ");
            for (int i = 0; i < model.VariableCount; i++)
            {
                sb.Append($"{model.ObjectiveCoefficients[i]:F3}x{i + 1}");
                if (i < model.VariableCount - 1) sb.Append(" + ");
            }
            sb.AppendLine();
            sb.AppendLine();

            // Constraints
            sb.AppendLine("Subject to:");
            // This is where slack/surplus variables would be added by the solver
            // For now, we just print the original constraints.
            foreach (var c in model.Constraints)
            {
                for (int i = 0; i < c.Coefficients.Count; i++)
                {
                    sb.Append($"{c.Coefficients[i]:F3}x{i + 1}");
                    if (i < c.Coefficients.Count - 1) sb.Append(" + ");
                }
                string relation = c.Relation == RelationType.LessThanOrEqual ? "<=" : (c.Relation == RelationType.GreaterThanOrEqual ? ">=" : "=");
                sb.AppendLine($" {relation} {c.RightHandSide:F3}");
            }
            sb.AppendLine();
            sb.AppendLine("------------------------");
            sb.AppendLine();

            File.WriteAllText(filePath, sb.ToString()); // Overwrites file for a new run
        }

        /// Appends a formatted tableau to the output file.
        public static void AppendTableau(double[,] tableau, string title, string[] headers, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- {title} ---");

            // Headers
            sb.AppendLine(string.Join("\t", headers));

            // Data
            for (int i = 0; i < tableau.GetLength(0); i++)
            {
                for (int j = 0; j < tableau.GetLength(1); j++)
                {
                    sb.Append(tableau[i, j].ToString("F3").PadRight(8));
                    if (j < tableau.GetLength(1) - 1) sb.Append("\t");
                }
                sb.AppendLine();
            }
            sb.AppendLine("------------------------");
            sb.AppendLine();

            File.AppendAllText(filePath, sb.ToString());
        }

        /// Writes a final status message, such as "Infeasible" or "Unbounded".
        public static void AppendStatusMessage(string message, string filePath)
        {
            File.AppendAllText(filePath, $"--- RESULT ---\n{message}\n--------------\n");
        }
    }
}
