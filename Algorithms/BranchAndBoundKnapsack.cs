using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project_LPR381.Algorithms
{
    /// Branch & Bound (0/1 Knapsack) with “Sub-Problem” 0, 1, 1.1, 1.2… style,
    /// capacity arithmetic (e.g., “15 - 12 = 3” on each line), and Candidate A/B/C labels.
    public sealed class BranchAndBoundKnapsack
    {
        private const int MaxDepth = 32;  // safety
        private readonly List<string> _steps = new List<string>();
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private int _candidateCounter = 0; // for A, B, C...
        private IterationLog _log;

        private struct Item
        {
            public int Id;       // original index
            public double V;     // value (profit)
            public double W;     // weight (cost)
            public double Ratio; // V/W (∞ if W<=0)
        }

        private struct Fix
        {
            public int ItemId;   // original index
            public int Value01;  // 0 or 1
        }

        private struct Candidate
        {
            public string Label;     // A, B, ...
            public string Problem;   // e.g., "1.2.1"
            public double Profit;
            public int[] Take;       // x_i in original order
        }

        /// <summary>
        /// Solve and print steps. Also returns all step strings for exporting if you need.
        /// </summary>
        public List<string> Solve(KnapsackModel model, IterationLog log)
        {
            _steps.Clear();
            _candidates.Clear();
            _candidateCounter = 0;
            _log = log ?? new IterationLog();

            if (model == null || model.Values == null || model.Weights == null ||
                model.Values.Length == 0 || model.Values.Length != model.Weights.Length)
            {
                Note("Invalid knapsack model.");
                return _steps;
            }

            var items = BuildItems(model);
            Note("Start Branch & Bound – Knapsack");
            Note("");

            // Root sub-problem “0”
            var rootFixed = new List<Fix>(); // none
            Recurse(model.Capacity, items, rootFixed, "0", 0, model.Values.Length);

            // Print summary of candidates (if any)
            if (_candidates.Count > 0)
            {
                var best = _candidates.OrderByDescending(c => c.Profit).First();
                Note("");
                Note("Candidates:");
                foreach (var c in _candidates)
                    Note($"  Candidate {c.Label}  from Sub-Problem {c.Problem}:  z = {c.Profit:0.###}");

                Note("");
                Note($"Best solution: Candidate {best.Label}  from Sub-Problem {best.Problem}  with z = {best.Profit:0.###}");
            }
            else
            {
                Note("No feasible integral solution was found.");
            }

            Note("");
            Note("End Branch & Bound – Knapsack");
            return _steps;
        }

        // ---------- recursion ----------

        private void Recurse(double capacity, Item[] items, List<Fix> fixedChoices,
                             string problemName, int depth, int n)
        {
            if (depth > MaxDepth)
            {
                Note($"[{problemName}] Max depth reached — branch abandoned.");
                return;
            }

            // Pretty header
            Note($"Sub-Problem {problemName}");

            // 1) Apply fixed decisions and compute remaining capacity (show the arithmetic lines)
            var take = new double[n];          // fractional plan (original order)
            var fixedSet = fixedChoices.ToDictionary(f => f.ItemId, f => f.Value01);
            double remaining = capacity;
            var sb = new StringBuilder();

            // Show fixed decisions first (stars)
            foreach (var f in fixedChoices)
            {
                var it = items.First(x => x.Id == f.ItemId);
                if (f.Value01 == 1)
                {
                    sb.AppendLine(RowLine(true, it.Id, 1.0, remaining, it.W)); // "* xi = 1   R - w = R'"
                    remaining -= it.W;
                }
                else
                {
                    sb.AppendLine(RowLine(true, it.Id, 0.0, remaining, 0.0));  // "* xi = 0   R - 0 = R"
                }
                if (remaining < -1e-9)
                {
                    Note(sb.ToString().TrimEnd());
                    Note($"[{problemName}] Infeasible (capacity exceeded). Branch abandoned.");
                    return;
                }
                take[it.Id] = f.Value01;
            }

            // 2) Greedy fill in ratio order → pick branch variable if fractional
            int fracItemId = -1;
            double profit = 0.0;
            double startRemaining = remaining;

            foreach (var it in items)
            {
                if (fixedSet.ContainsKey(it.Id)) continue; // already decided

                if (it.W <= 1e-12) // zero or near-zero weight
                {
                    // Take it fully — no capacity change, infinite ratio edge
                    take[it.Id] = 1.0;
                    profit += it.V;
                    sb.AppendLine(RowLine(false, it.Id, 1.0, remaining, 0.0));
                    continue;
                }

                if (it.W <= remaining + 1e-12)
                {
                    // Take fully
                    take[it.Id] = 1.0;
                    profit += it.V;
                    sb.AppendLine(RowLine(false, it.Id, 1.0, remaining, it.W));
                    remaining -= it.W;
                }
                else
                {
                    // Take fraction
                    double frac = Math.Max(0.0, Math.Min(1.0, remaining / it.W));
                    take[it.Id] = frac;
                    profit += frac * it.V;
                    sb.AppendLine(RowLine(false, it.Id, frac, remaining, it.W)); // will show R - W (negative if over-written)
                    fracItemId = it.Id;
                    remaining -= frac * it.W; // should be ~0
                    break; // bounding step ends at first fractional
                }
            }

            // Print the block for this sub-problem
            Note(sb.ToString().TrimEnd());

            bool integral = fracItemId < 0; // no fractional used => integral plan
            if (integral)
            {
                // We have an integral feasible solution → Candidate
                var candLabel = NextCandidateLabel();
                var profitInt = items.Sum(it => (take[it.Id] >= 0.5 ? 1.0 : 0.0) * it.V);
                var finalTake = items.Select(it => take[it.Id] >= 0.5 ? 1 : 0).ToArray();

                Note($"Candidate {candLabel}: z = {profitInt:0.###}  (integral)");
                _candidates.Add(new Candidate
                {
                    Label = candLabel,
                    Problem = problemName,
                    Profit = profitInt,
                    Take = finalTake
                });
                return;
            }

            // 3) Branch on the fractional item: ".1" (fix = 0) and ".2" (fix = 1)
            //    We keep the exact numbering convention you asked for.
            var fracFix0 = new List<Fix>(fixedChoices) { new Fix { ItemId = fracItemId, Value01 = 0 } };
            var fracFix1 = new List<Fix>(fixedChoices) { new Fix { ItemId = fracItemId, Value01 = 1 } };

            // Left child (".1"): fix to 0
            string child1 = problemName == "0" ? "1" : problemName + ".1";
            Recurse(capacity, items, fracFix0, child1, depth + 1, n);

            // Right child (".2"): fix to 1
            string child2 = problemName == "0" ? "2" : problemName + ".2";
            Recurse(capacity, items, fracFix1, child2, depth + 1, n);
        }

        // ---------- helpers ----------

        private static Item[] BuildItems(KnapsackModel model)
        {
            var items = Enumerable.Range(0, model.N)
                .Select(i => new Item
                {
                    Id = i,
                    V = model.Values[i],
                    W = model.Weights[i],
                    Ratio = (model.Weights[i] <= 0) ? double.PositiveInfinity : model.Values[i] / model.Weights[i]
                })
                .OrderByDescending(x => x.Ratio) // ratio order (as in notes)
                .ToArray();
            return items;
        }

        private static string VarName(int id) => "x" + (id + 1);

        private static string RowLine(bool isFixed, int itemId, double chosen, double before, double weight)
        {
            // e.g. "* x1 = 1     15 - 12 = 3"   or   "x4 = 7/12    0 - 12 = -12"
            string star = isFixed ? "* " : "";
            string chosenStr = chosen % 1.0 == 0 ? ((int)chosen).ToString() : chosen.ToString("0.###");
            double after = before - (chosen * weight);
            string arithmetic = $"{Trim(before)} - {Trim(chosen * weight)} = {Trim(after)}";
            return $"{star}{VarName(itemId),-3} = {chosenStr,-5}  {arithmetic}";
        }

        private static string Trim(double x)
        {
            double rx = Math.Abs(x) < 1e-9 ? 0.0 : x;
            return rx.ToString("0.###");
        }

        private void Note(string s)
        {
            _steps.Add(s);
            _log?.Note(s);
        }

        private string NextCandidateLabel()
        {
            int t = _candidateCounter++;
            // A..Z, then AA..AZ, BA.. etc.
            var letters = new StringBuilder();
            do
            {
                letters.Insert(0, (char)('A' + (t % 26)));
                t = t / 26 - 1;
            } while (t >= 0);
            return letters.ToString();
        }
    }
}
