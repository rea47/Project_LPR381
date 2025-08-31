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
            Console.WriteLine("Press any key to continue....");
            Console.ReadKey();
        }

        /// Placeholder: Solve the dual model -- Done

        public LinearProgrammingModel BuildDualModel(LinearProgrammingModel lpm)
        {
            var dual = new LinearProgrammingModel();

            dual.ObjectiveType = lpm.ObjectiveType == ObjectiveType.Maximize ? ObjectiveType.Minimize : ObjectiveType.Maximize; //max -> min OR min -> max

            dual.ObjectiveCoefficients = lpm.Constraints.Select(c => c.RightHandSide).ToArray(); //checking if the coefficients of the constraints are equal to the right hand side

            dual.Variables = new List<Variable>(); //converting the constraints to variables
            for (int j = 0; j < lpm.Constraints.Count; j++)
            {
                var pconst= lpm.Constraints[j];
                SignRestriction sr;
                switch(pconst.ConstraintType)
                {
                    case ConstraintRelation.LessOrEqual:
                        sr = lpm.ObjectiveType == ObjectiveType.Maximize ? SignRestriction.NonNegative : SignRestriction.NonPositive;
                        string name = $"y{j + 1}";
                        dual.Variables.Add(new Variable(name, sr, j));
                        break;
                    case ConstraintRelation.GreaterOrEqual:
                        sr = lpm.ObjectiveType == ObjectiveType.Maximize ? SignRestriction.NonPositive : SignRestriction.NonNegative;
                        string name1 = $"y{j + 1}";
                        dual.Variables.Add(new Variable(name1, sr, j));
                        break;
                    case ConstraintRelation.Equal:
                        sr = SignRestriction.Unrestricted;
                        string name2 = $"y{j + 1}";
                        dual.Variables.Add(new Variable(name2, sr, j));
                        break;
                    default:
                        sr = SignRestriction.Unrestricted;
                        string name3 = $"y{j + 1}";
                        dual.Variables.Add(new Variable(name3, sr, j));
                        break;
                }
            }
            dual.Constraints = new List<Constraint>(); //converting the variables to constraints
            for (int i = 0; i < lpm.Variables.Count; i++)
            {
                var coeffs = new double[lpm.Constraints.Count];

                for (int j = 0; j < lpm.Constraints.Count; j++)
                    coeffs[j] = lpm.Constraints[j].Coefficients[i];

                if (lpm.ObjectiveType == ObjectiveType.Maximize)
                    dual.Constraints.Add(new Constraint(coeffs, ">=", lpm.ObjectiveCoefficients[i]));
                else if(lpm.ObjectiveType == ObjectiveType.Minimize)
                    dual.Constraints.Add(new Constraint(coeffs, "<=", lpm.ObjectiveCoefficients[i]));
                else
                    dual.Constraints.Add(new Constraint(coeffs, "=" , lpm.ObjectiveCoefficients[i]));
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
            Console.ReadLine();
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
            Console.ReadKey();
        }
    }
}