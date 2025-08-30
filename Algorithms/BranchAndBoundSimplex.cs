using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Algorithms
{
    public sealed class BranchAndBoundSimplex
    {
        private const double EPS = 1e-6;

        private sealed class Node
        {
            public List<(int varIdx, double? lb, double? ub)> Bounds = new();
            public int Depth;
        }

        public sealed class Result
        {
            public bool Feasible;
            public double BestValue;
            public double[] BestX;
        }

        public Result Solve(LinearProgrammingModel baseModel, IterationLog log)
        {
            log.Title("Branch & Bound (Simplex)");

            var stack = new Stack<Node>();
            stack.Push(new Node { Depth = 0 });
            double best = double.NegativeInfinity;
            double[] bestX = null;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var model = CloneWithBounds(baseModel, node.Bounds);

                log.Note($"Node depth={node.Depth}, bounds: " +
                         (node.Bounds.Count == 0 ? "(none)" :
                          string.Join(", ", node.Bounds.Select(b => $"x{b.varIdx + 1}{(b.lb.HasValue ? $">={b.lb}" : "")}{(b.ub.HasValue ? $"<={b.ub}" : "")}"))));

                var simplex = new PrimalSimplex();
                var res = simplex.Solve(model, log);
                if (res.IsInfeasible)
                {
                    log.Note("Fathom: infeasible");
                    continue;
                }
                if (res.IsUnbounded)
                {
                    log.Note("Fathom: LP relaxation unbounded → ignore");
                    continue;
                }

                var z = res.ObjectiveValue;
                if (z <= best + EPS)
                {
                    log.Note($"Fathom: bound {IterationLog.R3(z)} ≤ best {IterationLog.R3(best)}");
                    continue;
                }

                // Check integrality
                var intIdx = baseModel.SignRestrictions
                    .Select((s, i) => (s, i))
                    .Where(t => t.s == SignRestriction.Integer || t.s == SignRestriction.Binary)
                    .Select(t => t.i)
                    .ToArray();

                int branchVar = -1; double frac = 0;
                foreach (var k in intIdx)
                {
                    double v = res.X[k];
                    double f = Math.Abs(v - Math.Round(v));
                    if (f > 1e-6 && f > frac) { frac = f; branchVar = k; }
                }

                if (branchVar == -1)
                {
                    // integral
                    if (z > best) { best = z; bestX = (double[])res.X.Clone(); }
                    log.Note($"Integral solution found. Best = {IterationLog.R3(best)}");
                    continue;
                }

                // Branch
                double val = res.X[branchVar];
                double floor = Math.Floor(val);
                double ceil = Math.Ceiling(val);

                var left = new Node { Depth = node.Depth + 1 };
                left.Bounds.AddRange(node.Bounds);
                left.Bounds.Add((branchVar, null, floor)); // x_k <= floor

                var right = new Node { Depth = node.Depth + 1 };
                right.Bounds.AddRange(node.Bounds);
                right.Bounds.Add((branchVar, ceil, null));  // x_k >= ceil

                // DFS push order: try upper bound branch second
                stack.Push(right);
                stack.Push(left);
            }

            return new Result { Feasible = bestX != null, BestValue = best, BestX = bestX };
        }

        private LinearProgrammingModel CloneWithBounds(LinearProgrammingModel m, List<(int varIdx, double? lb, double? ub)> bounds)
        {
            // Shallow copy of base model plus new constraints for bounds.
            var cp = new LinearProgrammingModel
            {
                ObjectiveType = m.ObjectiveType,
                ObjectiveCoefficients = (double[])m.ObjectiveCoefficients.Clone(),
                Variables = m.Variables.ToList(),
                SignRestrictions = m.SignRestrictions.ToList(),
                Constraints = m.Constraints.Select(c => new Constraint((double[])c.Coefficients.Clone(), c.Relation, c.RightHandSide)).ToList(),
                ParsingErrors = new List<string>(),
                SourceFile = m.SourceFile
            };

            foreach (var b in bounds)
            {
                var coeff = new double[m.Variables.Count];
                coeff[b.varIdx] = 1.0;
                if (b.lb.HasValue)
                    cp.Constraints.Add(new Constraint(coeff, ">=", b.lb.Value));
                if (b.ub.HasValue)
                    cp.Constraints.Add(new Constraint(coeff, "<=", b.ub.Value));
            }
            return cp;
        }
    }
}
