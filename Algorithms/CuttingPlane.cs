using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_LPR381.Algorithms
{
    /// Gomory fractional cutting-plane with fully explained steps:
    /// - choose a basic, integer-constrained variable with fractional RHS
    /// - rewrite row as x_B = b - Σ a_ij x_j
    /// - decompose with floor/frac: a_ij = floor(a_ij) + frac(a_ij),  b = floor(b) + frac(b)
    /// - move all fractional parts to RHS, show the "≤ 0" form, and final cut Σ frac(a_ij) x_j ≥ frac(b)
    /// - add cut using ONLY original variables (x1..xn)
    public sealed class CuttingPlane
    {
        private const double EPS = 1e-9;

        public sealed class Result
        {
            public bool Integral;
            public bool Infeasible;
            public double ObjectiveValue;
            public double[] X;
            public int CutsAdded;
        }

        public Result Solve(LinearProgrammingModel model, IterationLog log, int maxCuts = 50)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            int cuts = 0;

            while (true)
            {
                // 1) Solve LP relaxation
                var simplex = new PrimalSimplex();
                var res = simplex.Solve(model, log);

                if (res.IsInfeasible)
                {
                    log.Note("Cutting Plane: current relaxation infeasible.");
                    return new Result { Infeasible = true, Integral = false, CutsAdded = cuts };
                }
                if (res.IsUnbounded)
                {
                    log.Note("Cutting Plane: current relaxation unbounded.");
                    return new Result { Infeasible = false, Integral = false, CutsAdded = cuts };
                }

                // 2) Choose a basic row whose RHS is fractional (prefer integer-restricted original vars)
                var T = res.LastTableau;                 // (m+1) x (n+1)
                var rowNames = res.RowNames;             // length m
                var colNames = res.ColumnNames;          // length n (no RHS)
                int m = T.GetLength(0) - 1;
                int n = T.GetLength(1) - 1;
                int origN = model.Variables.Count;

                int chosenRow = -1;
                double chosenFrac = 0.0;
                string chosenBasicName = null;

                for (int i = 0; i < m; i++)
                {
                    double rhs = T[i, n];
                    double frac = Fractional(rhs);
                    if (frac < 1e-6 || frac > 1 - 1e-6) continue;

                    string bname = rowNames[i];
                    int k = ParseXIndex(bname); // xK -> K, otherwise -1
                    bool isOrig = (k >= 1 && k <= origN);
                    bool isIntRestricted = isOrig &&
                        (model.SignRestrictions[k - 1] == SignRestriction.Integer ||
                         model.SignRestrictions[k - 1] == SignRestriction.Binary);

                    bool better =
                        (isIntRestricted && chosenRow < 0) ||
                        (isIntRestricted && frac > chosenFrac + 1e-9) ||
                        (!isIntRestricted && chosenRow < 0 && chosenFrac <= 0);

                    if (better)
                    {
                        chosenRow = i;
                        chosenFrac = frac;
                        chosenBasicName = bname;
                    }
                }

                if (chosenRow < 0)
                {
                    log.Note($"Cutting Plane: all integer constraints satisfied after {cuts} cut(s).");
                    return new Result
                    {
                        Integral = true,
                        Infeasible = false,
                        CutsAdded = cuts,
                        ObjectiveValue = res.ObjectiveValue,
                        X = res.X
                    };
                }

                if (cuts >= maxCuts)
                {
                    log.Note($"Cutting Plane: reached cut limit ({maxCuts}).");
                    return new Result { Integral = false, Infeasible = false, CutsAdded = cuts, ObjectiveValue = res.ObjectiveValue, X = res.X };
                }

                // 3) Derive Gomory cut with the exact presentation you asked for
                // Your row is printed as: x_B + Σ row[i,j] x_j = RHS
                // Put in canonical "x_B = b - Σ a_ij x_j" where a_ij = -row[i,j]
                var a = new double[n];
                var aInt = new double[n];
                var aFrac = new double[n];

                for (int j = 0; j < n; j++)
                {
                    a[j] = -T[chosenRow, j];                 // a_ij
                    aInt[j] = Math.Floor(a[j]);              // integer part (works for negatives)
                    aFrac[j] = Fractional(a[j]);             // fractional part in [0,1)
                }

                double b = T[chosenRow, n];
                double bInt = Math.Floor(b);
                double bFrac = Fractional(b);

                // 4) Log the step-by-step derivation
                LogRowOriginal(log, chosenRow, chosenBasicName, T, colNames);
                LogRowRearranged(log, chosenBasicName, b, a, colNames);
                LogDecomposition(log, a, aInt, aFrac, b, bInt, bFrac, colNames);
                LogMoveFractions(log, chosenBasicName, aInt, aFrac, bInt, bFrac, colNames);
                LogInequalities(log, aFrac, bFrac, colNames);

                // 5) Add the cut using only original variables x1..xn:
                //    Σ frac(a_ij) x_j ≥ frac(b)
                var coeff = new double[origN];
                for (int j = 0; j < origN; j++)
                {
                    string name = "x" + (j + 1);
                    int cj = Array.IndexOf(colNames, name);
                    coeff[j] = (cj >= 0 && cj < n) ? aFrac[cj] : 0.0;
                }
                model.Constraints.Add(new Constraint(coeff, ">=", bFrac));
                cuts++;

                log.Note($"Added Cut #{cuts}:  " +
                         string.Join(" + ", coeff.Select((c, j) => $"{IterationLog.R3(c)}*x{j + 1}")) +
                         $"  ≥  {IterationLog.R3(bFrac)}");
            }
        }

        // ---------- helpers (C# 7.3-compatible) ----------

        private static double Fractional(double x)
        {
            // for negatives: -2.4 -> floor=-3, frac=0.6
            double f = x - Math.Floor(x);
            if (f < EPS) f = 0.0;
            if (1.0 - f < EPS) f = 0.0;
            return f;
        }

        private static int ParseXIndex(string name)
        {
            if (string.IsNullOrEmpty(name) || name[0] != 'x') return -1;
            int k; return int.TryParse(name.Substring(1), out k) ? k : -1;
        }

        private static void LogRowOriginal(IterationLog log, int row, string bname, double[,] T, string[] cols)
        {
            int n = T.GetLength(1) - 1;
            var L = new List<string> { bname };
            for (int j = 0; j < n; j++)
            {
                double c = T[row, j];
                if (Math.Abs(c) < 1e-12) continue;
                L.Add((c >= 0 ? " + " : " - ") + Math.Abs(c).ToString("0.###") + cols[j]);
            }
            log.Note($"\nSource row:   {string.Join("", L)}  =  {T[row, n]:0.###}");
        }

        private static void LogRowRearranged(IterationLog log, string bname, double b, double[] a, string[] cols)
        {
            var R = new List<string> { b.ToString("0.###") };
            for (int j = 0; j < a.Length; j++)
            {
                if (Math.Abs(a[j]) < 1e-12) continue;
                R.Add((a[j] >= 0 ? " - " : " + ") + Math.Abs(a[j]).ToString("0.###") + cols[j]);
            }
            log.Note($"Rearranged:   {bname} = {string.Join("", R)}");
        }

        private static void LogDecomposition(IterationLog log,
            double[] a, double[] aInt, double[] aFrac,
            double b, double bInt, double bFrac, string[] cols)
        {
            // Show e.g. -2.4s1  =>  (-3 + 0.6)s1
            var pieces = new List<string>();
            for (int j = 0; j < a.Length; j++)
            {
                if (Math.Abs(a[j]) < 1e-12) continue;
                pieces.Add($"{a[j]:0.###}{cols[j]} = ({aInt[j]:0.###} + {aFrac[j]:0.###}){cols[j]}");
            }
            var btxt = $"{b:0.###} = {bInt:0.###} + {bFrac:0.###}";
            if (pieces.Count > 0) log.Note("Decompose:    " + string.Join(",  ", pieces.ToArray()) + $",   b: {btxt}");
            else log.Note("Decompose:    " + $"b: {btxt}");
        }

        private static void LogMoveFractions(IterationLog log,
            string bname, double[] aInt, double[] aFrac, double bInt, double bFrac, string[] cols)
        {
            // x_B = (⌊b⌋ - Σ⌊a⌋x)  + (f_b - Σ f x)
            // Move fracs to RHS:
            // x_B - (⌊b⌋ - Σ⌊a⌋x) = f_b - Σ f x
            var left = new List<string> { bInt.ToString("0.###") };
            for (int j = 0; j < aInt.Length; j++)
            {
                if (Math.Abs(aInt[j]) < 1e-12) continue;
                left.Add((aInt[j] >= 0 ? " - " : " + ") + Math.Abs(aInt[j]).ToString("0.###") + cols[j]);
            }
            var right = new List<string> { bFrac.ToString("0.###") };
            for (int j = 0; j < aFrac.Length; j++)
            {
                if (Math.Abs(aFrac[j]) < 1e-12) continue;
                right.Add(" - " + aFrac[j].ToString("0.###") + cols[j]);
            }
            log.Note($"Move fracs:   {bname} - ({string.Join("", left)}) = {string.Join("", right)}");
        }

        private static void LogInequalities(IterationLog log, double[] aFrac, double bFrac, string[] cols)
        {
            // RHS ≤ 0: (f_b - Σ f x) ≤ 0
            var rhs = new List<string> { bFrac.ToString("0.###") };
            for (int j = 0; j < aFrac.Length; j++)
            {
                if (Math.Abs(aFrac[j]) < 1e-12) continue;
                rhs.Add(" - " + aFrac[j].ToString("0.###") + cols[j]);
            }
            log.Note($"Make ≤ 0:     {string.Join("", rhs)} ≤ 0");
            // Final cut: Σ f x ≥ f_b
            var lhs = new List<string>();
            for (int j = 0; j < aFrac.Length; j++)
            {
                if (Math.Abs(aFrac[j]) < 1e-12) continue;
                lhs.Add(aFrac[j].ToString("0.###") + cols[j]);
            }
            string lhsText = lhs.Count > 0 ? string.Join(" + ", lhs) : "0";
            log.Note($"Final Cut:    {lhsText} ≥ {bFrac:0.###}\n");
        }
    }
}
