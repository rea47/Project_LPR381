
using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project_LPR381.Algorithms
{
    public sealed class PrimalSimplex
    {
        private const double EPS = 1e-9;

        public sealed class Result
        {
            public bool IsOptimal;
            public bool IsUnbounded;
            public bool IsInfeasible;
            public double ObjectiveValue;
            public double[] X;                  // in original variable order
            public string[] ColumnNames;        // tableau column names (without RHS)
            public string[] RowNames;           // basic var names
            public double[,] LastTableau;       // final tableau (rows = m+1, cols = n+1 incl RHS)
        }

        // Public entry
        public Result Solve(LinearProgrammingModel model, IterationLog log)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Variables.Count == 0 || model.Constraints.Count == 0)
                throw new InvalidOperationException("Model has no variables or constraints.");

            // Build canonical (Two-Phase)
            Canonical K = BuildCanonical(model);
            log.Title("Canonical form");
            log.Note($"Objective: MAX  (internally we maximize; MIN is converted)");
            log.Note($"Vars: {string.Join(", ", K.ColNames)}");
            for (int i = 0; i < K.m; i++)
                log.Note($"Row {K.RowNames[i]}: " + string.Join(" ",
                    Enumerable.Range(0, K.n).Select(j => $"{(K.A[i, j] >= 0 && j > 0 ? "+" : "")}{K.A[i, j]} {K.ColNames[j]}")) + $" = {K.b[i]}");

            // Phase I if artificials exist
            Result res;
            if (K.ArtificialIdx.Count > 0)
            {
                res = PhaseI(K, log);
                if (res.IsInfeasible) return res;
                // Remove artificial columns, build Phase II tableau with original c
                K = BuildPhaseIIFromPhaseI(K, res);
            }

            // Phase II: optimize original objective
            res = RunSimplex(K, K.c, log, header: "Primal Simplex (Phase II)");
            return MapBackToOriginal(model, K, res);
        }

        // ===== Canonicalization =====
        private sealed class Canonical
        {
            public int m, n;                  // m constraints, n columns
            public double[,] A;               // m x n
            public double[] b;                // m
            public double[] c;                // n (original objective in canonical columns)
            public List<string> ColNames;     // var names for columns
            public string[] RowNames;         // basic names (size m)
            public List<int> Basic;           // basic column index per row
            public List<int> ArtificialIdx;   // artificial column indices
            public int origVarCount;          // number of original variables
            public double objSign = 1.0;      // 1 for max, -1 when original was min
            public int[] origVarCol;          // mapping original xi -> column index in canonical
        }

        private Canonical BuildCanonical(LinearProgrammingModel m)
        {
            // Convert MIN to MAX by flipping c
            var objSign = (m.ObjectiveType == ObjectiveType.Minimize) ? -1.0 : 1.0;
            int n0 = m.Variables.Count;
            int m0 = m.Constraints.Count;

            // Start with original x columns
            var colNames = new List<string>();
            for (int j = 0; j < n0; j++) colNames.Add($"x{j + 1}");

            // Build A,b, add slack/surplus/artificial
            var rows = new List<double[]>();
            var rhs = new List<double>();
            var basic = new List<int>();
            var artificials = new List<int>();

            // We will collect additional columns as we go
            var extraCols = new List<double[]>();
            var extraNames = new List<string>();

            for (int i = 0; i < m0; i++)
            {
                var cons = m.Constraints[i];
                var row = new double[colNames.Count + extraCols.Count];
                // ensure length
                Array.Resize(ref row, colNames.Count);
                for (int j = 0; j < n0; j++)
                    row[j] = cons.Coefficients[j];

                double b = cons.RightHandSide;

                if (cons.Relation == "<=")
                {
                    // add slack +1
                    var slack = new double[rows.Count + 1 == 0 ? 0 : 0]; // dummy to please IDE
                    AddNewColumn(extraCols, rows.Count, +1.0, out int colIdx);
                    extraNames.Add($"s{i + 1}");
                    row = ExtendRow(row, extraCols);
                    basic.Add(colNames.Count + colIdx);
                }
                else if (cons.Relation == ">=")
                {
                    // surplus -1 and artificial +1
                    AddNewColumn(extraCols, rows.Count, -1.0, out int cSur);
                    extraNames.Add($"t{i + 1}");
                    // artificial
                    AddNewColumn(extraCols, rows.Count, +1.0, out int cArt);
                    extraNames.Add($"a{i + 1}");
                    row = ExtendRow(row, extraCols);
                    basic.Add(colNames.Count + cArt);
                    artificials.Add(colNames.Count + cArt);
                }
                else // "="
                {
                    // artificial +1
                    AddNewColumn(extraCols, rows.Count, +1.0, out int cArt);
                    extraNames.Add($"a{i + 1}");
                    row = ExtendRow(row, extraCols);
                    basic.Add(colNames.Count + cArt);
                    artificials.Add(colNames.Count + cArt);
                }

                rows.Add(row);
                rhs.Add(b);
            }

            // finalize matrix
            int m1 = rows.Count;
            int n1 = colNames.Count + extraCols.Count;
            var A = new double[m1, n1];
            for (int i = 0; i < m1; i++)
                for (int j = 0; j < n1; j++)
                    A[i, j] = rows[i][j];

            var bvec = rhs.ToArray();

            // compose names
            colNames.AddRange(extraNames);
            var rowNames = Enumerable.Range(0, m1).Select(i => $"B{i + 1}").ToArray();

            // objective in canonical columns
            var c = new double[n1];
            for (int j = 0; j < n0; j++) c[j] = objSign * m.ObjectiveCoefficients[j];

            return new Canonical
            {
                m = m1,
                n = n1,
                A = A,
                b = bvec,
                c = c,
                ColNames = colNames,
                RowNames = rowNames,
                Basic = basic,
                ArtificialIdx = artificials,
                origVarCount = n0,
                objSign = objSign,
                origVarCol = Enumerable.Range(0, n0).ToArray()
            };

            // local helpers
            static void AddNewColumn(List<double[]> cols, int currentRow, double valAtRow, out int idx)
            {
                idx = cols.Count;
                int r = currentRow + 1; // number of rows after we add this one
                var col = new double[r];
                for (int i = 0; i < r - 1; i++) col[i] = 0.0;
                col[r - 1] = valAtRow;
                cols.Add(col);
            }
            static double[] ExtendRow(double[] row, List<double[]> cols)
            {
                int n = row.Length;
                int add = cols.Count - (n - row.Length);
                var newRow = new double[cols[0].Length + (n)];
                // rebuild by reading each column vector at this row
                int totalCols = n + cols.Count;
                var r = new double[totalCols];
                for (int j = 0; j < n; j++) r[j] = row[j];
                for (int j = 0; j < cols.Count; j++) r[n + j] = cols[j][cols[j].Length - 1];
                return r;
            }
        }

        private Result PhaseI(Canonical K, IterationLog log)
        {
            // Phase I objective: minimize sum of artificials -> maximize -sum(a)
            var c1 = new double[K.n];
            foreach (int j in K.ArtificialIdx) c1[j] = -1.0;
            var r = RunSimplex(K, c1, log, header: "Primal Simplex (Phase I)");
            if (!r.IsOptimal || r.ObjectiveValue < -EPS)
            {
                r.IsInfeasible = true;
                return r;
            }
            return r;
        }

        private Canonical BuildPhaseIIFromPhaseI(Canonical K, Result phaseIRes)
        {
            // remove artificial columns
            var keep = Enumerable.Range(0, K.n).Except(K.ArtificialIdx).ToArray();
            int n2 = keep.Length;
            var A2 = new double[K.m, n2];
            for (int i = 0; i < K.m; i++)
                for (int jj = 0; jj < n2; jj++)
                    A2[i, jj] = K.A[i, keep[jj]];
            var c2 = new double[n2];
            for (int jj = 0; jj < n2 && jj < K.c.Length; jj++)
                c2[jj] = K.c[keep[jj]];
            var colNames2 = keep.Select(j => K.ColNames[j]).ToList();

            // Rebuild basis indices if needed (drop any artificial bases)
            var basic2 = new List<int>();
            for (int i = 0; i < K.m; i++)
            {
                int bjOld = K.Basic[i];
                int pos = Array.IndexOf(keep, bjOld);
                if (pos >= 0) basic2.Add(pos);
                else
                {
                    // try to find an identity column in remaining columns
                    int bj = FindUnitColumn(A2, i);
                    basic2.Add(bj >= 0 ? bj : 0);
                }
            }

            return new Canonical
            {
                m = K.m,
                n = n2,
                A = A2,
                b = (double[])K.b.Clone(),
                c = c2,
                ColNames = colNames2,
                RowNames = (string[])K.RowNames.Clone(),
                Basic = basic2,
                ArtificialIdx = new List<int>(),
                origVarCount = K.origVarCount,
                objSign = K.objSign,
                origVarCol = K.origVarCol
            };

            static int FindUnitColumn(double[,] A, int row)
            {
                int m = A.GetLength(0);
                int n = A.GetLength(1);
                for (int j = 0; j < n; j++)
                {
                    bool isUnit = true;
                    for (int i = 0; i < m; i++)
                    {
                        double v = Math.Abs(A[i, j]);
                        if (i == row) { if (Math.Abs(v - 1.0) > 1e-9) { isUnit = false; break; } }
                        else { if (v > 1e-9) { isUnit = false; break; } }
                    }
                    if (isUnit) return j;
                }
                return -1;
            }
        }

        // ===== Core simplex on a canonical model =====
        private Result RunSimplex(Canonical K, double[] c, IterationLog log, string header)
        {
            log.Title(header);

            // Build tableau: rows m+1, cols n+1 (RHS)
            int m = K.m; int n = K.n;
            var T = new double[m + 1, n + 1];

            // copy A|b
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) T[i, j] = K.A[i, j];
                T[i, n] = K.b[i];
            }
            // objective row: z - c^T x = 0  -> store reduced costs as -(c_N - c_B*B^-1*N) on the fly via tableau ops
            for (int j = 0; j < n; j++) T[m, j] = -c[j];
            T[m, n] = 0.0;

            var rowNames = K.RowNames.ToArray();
            var colNames = K.ColNames.ToArray();

            // If the current basis not reflected in tableau, pivot to make it basic (we built rows accordingly, so usually ok)

            int step = 0;
            log.PrintTableau(colNames, rowNames, T, step);

            while (true)
            {
                int enter = ChooseEntering(T, m, n);
                if (enter < 0) // optimal
                {
                    var res = ExtractResult(K, T, m, n, colNames, rowNames);
                    res.IsOptimal = true;
                    return res;
                }

                int leave = ChooseLeaving(T, m, n, enter);
                if (leave < 0)
                {
                    // unbounded
                    var res = ExtractResult(K, T, m, n, colNames, rowNames);
                    res.IsUnbounded = true;
                    return res;
                }

                string entName = colNames[enter];
                string levName = rowNames[leave];

                Pivot(T, m, n, leave, enter);

                rowNames[leave] = entName;

                step++;
                log.PrintTableau(colNames, rowNames, T, step, $"Entering {entName}, Leaving {levName}");
            }
        }

        private static int ChooseEntering(double[,] T, int m, int n)
        {
            int col = -1; double best = 0.0;
            for (int j = 0; j < n; j++)
            {
                double rc = T[m, j];
                if (rc < -1e-9) // remember objective row stores -reduced-costs
                {
                    if (col < 0 || rc < best) { best = rc; col = j; }
                }
            }
            return col;
        }

        private static int ChooseLeaving(double[,] T, int m, int n, int enter)
        {
            int row = -1; double best = double.PositiveInfinity;
            for (int i = 0; i < m; i++)
            {
                double a = T[i, enter];
                if (a > 1e-9)
                {
                    double ratio = T[i, n] / a;
                    if (ratio < best - 1e-12) { best = ratio; row = i; }
                }
            }
            return row;
        }

        private static void Pivot(double[,] T, int m, int n, int pRow, int pCol)
        {
            double piv = T[pRow, pCol];
            // scale pivot row
            for (int j = 0; j <= n; j++) T[pRow, j] /= piv;
            // eliminate other rows
            for (int i = 0; i <= m; i++)
            {
                if (i == pRow) continue;
                double factor = T[i, pCol];
                if (Math.Abs(factor) < 1e-12) continue;
                for (int j = 0; j <= n; j++)
                    T[i, j] -= factor * T[pRow, j];
            }
        }

        private Result ExtractResult(Canonical K, double[,] T, int m, int n, string[] colNames, string[] rowNames)
        {
            // basic variables are the row names; read their RHS
            var xCanon = new double[n];
            for (int i = 0; i < m; i++)
            {
                int col = Array.IndexOf(colNames, rowNames[i]);
                if (col >= 0) xCanon[col] = T[i, n];
            }
            var xOrig = new double[K.origVarCount];
            for (int j = 0; j < K.origVarCount; j++)
                xOrig[j] = (j < xCanon.Length) ? xCanon[j] : 0.0;

            double z = T[m, n];
            return new Result
            {
                ObjectiveValue = z,
                X = xOrig,
                ColumnNames = colNames,
                RowNames = rowNames,
                LastTableau = T
            };
        }

        private Result MapBackToOriginal(LinearProgrammingModel model, Canonical K, Result r)
        {
            // If original was MIN, undo sign for objective (we maximized objSign*c)
            if (model.ObjectiveType == ObjectiveType.Minimize)
                r.ObjectiveValue *= -1.0;

            return r;
        }
    }
}
