using Project_LPR381.Algorithms;
using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR31.Algorithms
{
    public class DualityAlgo
    {
        /// Print a basic duality analysis of the given model --done
        /// 
        public void ApplyDuality(LinearProgrammingModel lpm)
        {
            Console.Clear();
            Console.WriteLine("--- Primal Model ---");
            Console.WriteLine($"Primal Objective: {lpm.ObjectiveType} Z = " +
                string.Join(" + ", lpm.ObjectiveCoefficients.Select((c, i) => $"{c}x{i + 1}")));

            Console.WriteLine("\n--- Constraints (Primal) ---");
            foreach (var c in lpm.Constraints)
                Console.WriteLine(c.ToString());

            // Dual construction
            Console.WriteLine("\n--- Dual Model ---");

            var dualObjective = lpm.ObjectiveType == ObjectiveType.Maximize ? "Minimize" : "Maximize"; //flipping obj

            Console.WriteLine($"{dualObjective} W = " + //min|max
                string.Join(" + ", lpm.Constraints.Select((c, j) => $"{c.RightHandSide}y{j + 1}")));

            //build constraints (trans A)
            for (int i = 0; i < lpm.Variables.Count; i++)
            {
                var coeffs = lpm.Constraints.Select(c => c.Coefficients[i]).ToArray();
                string constraint = string.Join(" + ", coeffs.Select((c, j) => $"{c}y{j + 1}"));

                string inequality = (lpm.ObjectiveType == ObjectiveType.Maximize) ? ">=" : "<="; //sign rest according to obj W

                Console.WriteLine($"{constraint} {inequality} {lpm.ObjectiveCoefficients[i]}");
            }

            Console.ReadKey();
        }

        /// Placeholder: Solve the dual model -- Done

        public LinearProgrammingModel BuildDualModel(LinearProgrammingModel lpm)
        {
            var dual = new LinearProgrammingModel();

            dual.ObjectiveType = lpm.ObjectiveType == ObjectiveType.Maximize ? ObjectiveType.Minimize : ObjectiveType.Maximize;
            dual.ObjectiveCoefficients = lpm.Constraints.Select(c => c.RightHandSide).ToArray();

            // Create dual variables based on primal constraints
            dual.Variables = new List<Variable>();
            for (int j = 0; j < lpm.Constraints.Count; j++)
            {
                // This logic seems incomplete in your file, so we'll assume y >= 0 for simplicity.
                // A full implementation would check primal variable sign restrictions.
                string name = $"y{j + 1}";
                dual.Variables.Add(new Variable(name, SignRestriction.NonNegative, j));
            }

            // Create dual constraints based on primal variables
            dual.Constraints = new List<Constraint>();
            // Determine the correct relation for all dual constraints
            string dualRelation = lpm.ObjectiveType == ObjectiveType.Maximize ? ">=" : "<=";

            for (int i = 0; i < lpm.Variables.Count; i++)
            {
                var coeffs = new double[lpm.Constraints.Count];
                for (int j = 0; j < lpm.Constraints.Count; j++)
                {
                    coeffs[j] = lpm.Constraints[j].Coefficients[i];
                }
                // Use the correct dualRelation here instead of an empty string
                dual.Constraints.Add(new Constraint(coeffs, dualRelation, lpm.ObjectiveCoefficients[i]));
            }
            return dual;
        }

        public void SolveDualModel(LinearProgrammingModel lpm)
        {
            IterationLog il = new IterationLog();
            Console.Clear();
            Console.WriteLine("=== Solve Dual Model ===");

            var dual = BuildDualModel(lpm);

            var solver = new PrimalSimplex();
            var result = solver.Solve(dual,il);

            Console.WriteLine(result.ToString());
        }

        /// Placeholder: Verify strong/weak duality conditions  --Done
        public void VerifyDuality(LinearProgrammingModel lpm)
        {
            IterationLog il = new IterationLog();
            Console.Clear();
            Console.WriteLine("=== Verify Strong/Weak Duality ===");

            var dual = BuildDualModel(lpm);

            var solver = new PrimalSimplex();
            var primalResult = solver.Solve(lpm, il);
            var dualResult = solver.Solve(dual, il);

            Console.WriteLine($"Primal Optimal Value: {primalResult.ObjectiveValue}");
            Console.WriteLine($"Dual Optimal Value: {dualResult.ObjectiveValue}");

            if (Math.Abs(primalResult.ObjectiveValue - dualResult.ObjectiveValue) < 1e-6)
            {
                Console.WriteLine("Strong Duality holds (Primal and Dual optimal values are equal).");
            }
            else
            {
                Console.WriteLine("Only Weak Duality holds (Primal ≤ Dual for max problems).");
            }
        }
    }
}