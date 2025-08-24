using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR31.Models
{
    /// Enumeration for optimization direction
    public enum ObjectiveType
    {
        Maximize,
        Minimize
    }

    /// Enumeration for constraint relations
    public enum ConstraintRelation
    {
        LessOrEqual,
        GreaterOrEqual,
        Equal
    }

    /// Enumeration for variable sign restrictions
    public enum SignRestriction
    {
        NonNegative,    // +
        NonPositive,    // -
        Unrestricted,   // urs
        Integer,        // int
        Binary          // bin
    }

    /// Enumeration for solution status (for future solver integration)
    public enum SolutionStatus
    {
        Optimal,
        Infeasible,
        Unbounded,
        Unknown
    }
}
