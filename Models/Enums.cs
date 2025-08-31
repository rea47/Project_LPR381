using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR381.Models
{
    /// Defines the objective of the linear programming model (Maximize or Minimize).
    public enum ObjectiveType
    {
        Maximize,
        Minimize
    }

    /// Defines the sign restrictions for a variable.
    public enum SignRestriction
    {
        NonNegative,    // >= 0
        NonPositive,    // <= 0
        Unrestricted,   // Free variable
        Integer,        // Must be an integer
        Binary          // Must be 0 or 1
    }

    /// Defines the relationship in a constraint (e.g., <=, >=, =).
    public enum ConstraintRelation
    {
        LessOrEqual,
        GreaterOrEqual,
        Equal
    }
}

