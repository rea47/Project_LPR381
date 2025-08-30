using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks

namespace Project_LPR381.Algorithms
{
    public sealed class CuttingPlane
    {
        private const double EPS = 1e-6;

        public sealed class Result
        {
            public bool Integral;
            public double ObjectiveValue;
            public double[] X;
            public int CutsAdded;
        }

        public Result Solve(LinearProgrammingModel model, IterationLog log, int maxCuts = 20)
        {
            var simplex = new PrimalSimplex();
            int cuts = 0;

            while (true)
            {
                var res = simplex.Solve(model, log);
                if (res.IsInfeasible) return new Result { Integral = false, CutsAdded = cuts };
                if (res.IsUnbounded) return new Result { Integral = false, CutsAdded = cuts };

                int n = model.Variables.Count;
                int fracIdx = -1; double frac = 0;
                for (int j = 0; j < n; j++)
                {
                    double v = res.X[j];
                    double f = Math.Abs(v - Math.Round(v));
                    if (f > EPS && f > frac) { frac = f; fracIdx = j; }
                }
                if (fracIdx == -1)
                    return new Result { Integral = true, ObjectiveValue = res.ObjectiveValue, X = res.X, CutsAdded = cuts };

                if (cuts >= maxCuts)
                    return new Result { Integral = false, ObjectiveValue = res.ObjectiveValue, X = res.X, CutsAdded = cuts };

                // Build a simple Gomory fractional cut: sum(frac(a_j)) x_j >= frac(b)
                // We use the final tableau row for the most fractional basic variable if available.
                var T = res.LastTableau;
                var rowNames = res.RowNames;
                var colNames = res.ColumnNames;

                // pick the row whose RHS is most fractional
                int m = T.GetLength(0) - 1, ncols = T.GetLength(1) - 1;
                int row = -1; double rf = 0;
                for (int i = 0; i < m; i++)
                {
                    double f = FracPart(T[i, ncols]);
                    if (f > rf + 1e-9) { rf = f; row = i; }
                }
                if (row == -1)
                    return new Result { Integral = false, ObjectiveValue = res.ObjectiveValue, X = res.X, CutsAdded = cuts };

                var coeff = new double[n];
                for (int j = 0; j < n; j++)
                {
                    int col = Array.IndexOf(colNames, $"x{j + 1}");
                    double a = (col >= 0 && col < ncols) ? T[row, col] : 0.0;
                    coeff[j] = FracPart(a);
                }
                double rhs = FracPart(T[row, ncols]);

                // Add cut as >= constraint
                model.Constraints.Add(new Constraint(coeff, ">=", rhs));
                cuts++;
                log.Note($"Added Gomory cut #{cuts}: sum(frac(a_j)) x_j >= {IterationLog.R3(rhs)}");
            }

            static double FracPart(double x) { double f = x - Math.Floor(x); if (f < 1e-9) f = 0; return f; }
        }
    }
}
