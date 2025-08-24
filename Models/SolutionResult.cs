using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Models
{
    /// Represents the solution result from optimization algorithms
    /// This class is prepared for future solver integration
    public class SolutionResult
    {
        public SolutionStatus Status { get; set; }
        public double OptimalValue { get; set; }
        public double[] VariableValues { get; set; }
        public double[] DualValues { get; set; }
        public string[] BasicVariables { get; set; }
        public int Iterations { get; set; }
        public TimeSpan SolutionTime { get; set; }
        public string AlgorithmUsed { get; set; }

        public SolutionResult()
        {
            Status = SolutionStatus.Unknown;
            OptimalValue = double.NaN;
            VariableValues = new double[0];
            DualValues = new double[0];
            BasicVariables = new string[0];
            Iterations = 0;
            SolutionTime = TimeSpan.Zero;
            AlgorithmUsed = string.Empty;
        }

        /// Check if solution is feasible and optimal
        public bool IsOptimal => Status == SolutionStatus.Optimal;

        /// Check if solution is feasible
        public bool IsFeasible => Status == SolutionStatus.Optimal;

        /// Get formatted string representation of the solution
        public override string ToString()
        {
            if (Status == SolutionStatus.Optimal)
            {
                var vars = string.Join(", ", VariableValues.Select((v, i) => $"x{i + 1}={v:F3}"));
                return $"Status: {Status}, Optimal Value: {OptimalValue:F3}, Variables: [{vars}], Iterations: {Iterations}";
            }
            return $"Status: {Status}";
        }

        /// Get detailed solution report
        public string GetDetailedReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Solution Status: {Status}");
            report.AppendLine($"Algorithm Used: {AlgorithmUsed}");
            report.AppendLine($"Solution Time: {SolutionTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Iterations: {Iterations}");

            if (IsOptimal)
            {
                report.AppendLine($"Optimal Value: {OptimalValue:F6}");
                report.AppendLine("Variable Values:");
                for (int i = 0; i < VariableValues.Length; i++)
                {
                    report.AppendLine($"  x{i + 1} = {VariableValues[i]:F6}");
                }

                if (DualValues.Length > 0)
                {
                    report.AppendLine("Dual Values:");
                    for (int i = 0; i < DualValues.Length; i++)
                    {
                        report.AppendLine($"  y{i + 1} = {DualValues[i]:F6}");
                    }
                }
            }

            return report.ToString();
        }
    }
}
