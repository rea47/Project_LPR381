using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Models
{
    public sealed class KnapsackModel
    {
        public double[] Values;
        public double[] Weights;
        public double Capacity;
        public int N => Values?.Length ?? 0;
    }

}
