using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_LPR381.Algorithms
{
    /// Branch & Bound using primal simplex at each node.
    /// Variable selection: integer/bin variable whose current value is fractional and CLOSEST TO 0.5 (tie → lower index).
    /// Logs active bounds under every tableau.
    public sealed class BranchAndBoundSimplex
    {
        private const double EPS = 1e-6;

        private sealed class Node
        {
            public List<(int varIdx, double? lb, double? ub)> Bounds = new List<(int varIdx, double? lb, double? ub)>();
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

                // Show active bounds under each simplex tableau
                var footer = node.Bounds.Count == 0
                    ? new[] { $"Node depth: {node.Depth}  |  Active bounds: (none)" }
                    : new[] { $"Node depth: {node.Depth}",
                              "Active bounds: " + string.Join(", ", node.Bounds.Select(b =>
                                  $"x{b.varIdx + 1}" +
                                  (b.lb.HasValue ? $" ≥ {IterationLog.R3(b.lb.Value)}" : "") +
                                  (b.ub.HasValue ? $" ≤ {IterationLog.R3(b.ub.Value)}" : "")
                              )) };
                log.SetFooter(footer);

                var simplex = new PrimalSimplex();
                var res = simplex.Solve(model, log);
                log.ClearFooter();

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

                // integer/bin indices
                var intIdx = baseModel.SignRestrictions
                    .Select((s, i) => (s, i))
                    .Where(t => t.s == SignRestriction.Integer || t.s == SignRestriction.Binary)
                    .Select(t => t.i)
                    .ToArray();

                // choose var closest to 0.5 among fractional ones
                int branchVar = -1; double bestDist = double.PositiveInfinity;
                foreach (var k in intIdx)
                {
                    double v = res.X[k];
                    double frac = Math.Abs(v - Math.Round(v));
                    if (frac < 1e-9) continue;
                    double d = Math.Abs(v - 0.5);
                    if (d < bestDist - 1e-12 || (Math.Abs(d - bestDist) <= 1e-12 && k < branchVar))
                    {
                        bestDist = d; branchVar = k;
                    }
                }

                if (branchVar == -1)
                {
                    // integral
                    if (z > best) { best = z; bestX = (double[])res.X.Clone(); }
                    log.Note($"Integral solution found. Best = {IterationLog.R3(best)}");
                    continue;
                }

                double val = res.X[branchVar];
                double floor = Math.Floor(val);
                double ceil = Math.Ceiling(val);
                log.Note($"Branch on x{branchVar + 1}: x{branchVar + 1} ≤ {floor}  OR  x{branchVar + 1} ≥ {ceil}  (value = {IterationLog.R3(val)})");

                var left = new Node { Depth = node.Depth + 1 };
                left.Bounds.AddRange(node.Bounds);
                left.Bounds.Add((branchVar, null, floor));

                var right = new Node { Depth = node.Depth + 1 };
                right.Bounds.AddRange(node.Bounds);
                right.Bounds.Add((branchVar, ceil, null));

                // DFS
                stack.Push(right);
                stack.Push(left);
            }

            return new Result { Feasible = bestX != null, BestValue = best, BestX = bestX };
        }

        private static LinearProgrammingModel CloneWithBounds(LinearProgrammingModel m, List<(int varIdx, double? lb, double? ub)> bounds)
        {
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
                if (b.lb.HasValue) cp.Constraints.Add(new Constraint(coeff, ">=", b.lb.Value));
                if (b.ub.HasValue) cp.Constraints.Add(new Constraint(coeff, "<=", b.ub.Value));
            }
            return cp;
        }
    }
}
