using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Models
{
    /// <summary>
    /// Temporary placeholder for LP solving results.
    /// Replace with actual solver output when implemented.
    /// </summary>
    public class SolutionResult
    {
        public bool IsOptimal { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; }

        public SolutionResult()
        {
            IsOptimal = false;
            ObjectiveValue = 0.0;
            VariableValues = new double[0];
        }
    }
}
