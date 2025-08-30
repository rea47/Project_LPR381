using Project_LPR31.Models;
using Project_LPR381.Models;
using Project_LPR381.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Algorithms
{
    public sealed class BranchAndBoundKnapsack
    {
        private sealed class Node
        {
            public int k;            // next item index
            public double value;
            public double weight;
            public int[] take;       // 0/1 decisions up to k
        }

        public sealed class Result
        {
            public double BestValue;
            public int[] BestTake;
        }

        public Result Solve(KnapsackModel m, IterationLog log)
        {
            log.Title("Branch & Bound (Knapsack)");
            int n = m.N;
            var order = Enumerable.Range(0, n)
                .OrderByDescending(i => m.Values[i] / m.Weights[i]).ToArray();

            double Best = 0;
            int[] BestSel = new int[n];

            var stack = new Stack<Node>();
            stack.Push(new Node { k = 0, value = 0, weight = 0, take = new int[n] });

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                double UB = node.value + FractionalUpperBound(m, order, node.k, node.weight);

                log.Note($"Node k={node.k}, val={IterationLog.R3(node.value)}, wt={IterationLog.R3(node.weight)}, UB={IterationLog.R3(UB)}");

                if (UB <= Best) continue;

                if (node.k == n)
                {
                    if (node.value > Best) { Best = node.value; BestSel = (int[])node.take.Clone(); }
                    continue;
                }

                int i = order[node.k];

                // include
                if (node.weight + m.Weights[i] <= m.Capacity)
                {
                    var a = new Node
                    {
                        k = node.k + 1,
                        value = node.value + m.Values[i],
                        weight = node.weight + m.Weights[i],
                        take = (int[])node.take.Clone()
                    };
                    a.take[i] = 1;
                    stack.Push(a);
                }
                // exclude
                {
                    var b = new Node
                    {
                        k = node.k + 1,
                        value = node.value,
                        weight = node.weight,
                        take = (int[])node.take.Clone()
                    };
                    b.take[i] = 0;
                    stack.Push(b);
                }
            }

            log.Note($"Best value = {IterationLog.R3(Best)}");
            return new Result { BestValue = Best, BestTake = BestSel };
        }

        private double FractionalUpperBound(KnapsackModel m, int[] order, int k, double w)
        {
            double cap = m.Capacity - w;
            double ub = 0;
            for (int t = k; t < order.Length; t++)
            {
                int i = order[t];
                if (m.Weights[i] <= cap) { ub += m.Values[i]; cap -= m.Weights[i]; }
                else { ub += (cap / m.Weights[i]) * m.Values[i]; break; }
            }
            return ub;
        }
    }
}
