using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Models
{
    /// Temporary placeholder for LP solving results.
    /// Replace with actual solver output when implemented.
    public class SolutionResult
    {
        public bool IsOptimal { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; }
        // The complete final tableau, including the objective function row (z-row).
        public double[,] LastTableau { get; set; }

        // The names of the columns in the order they appear in the tableau (e.g., "x1", "x2", "s1", ...).
        public string[] ColumnNames { get; set; }

        // The names of the basic variables for each row of the tableau (e.g., "s1", "x2", ...).
        public string[] RowNames { get; set; }

        public SolutionResult()
        {
            IsOptimal = false;
            ObjectiveValue = 0.0;
            VariableValues = new double[0];
        }
    }
}
