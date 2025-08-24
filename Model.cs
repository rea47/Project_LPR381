using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Project_LPR31;

namespace OperationsResearchSolver
{
    /// Enumeration for the objective function type (Maximization or Minimization).
    public enum ObjectiveType
    {
        Max,
        Min
    }

    /// Enumeration for the type of relation in a constraint (<=, >=, =).
    public enum RelationType
    {
        LessThanOrEqual,
        GreaterThanOrEqual,
        EqualTo
    }

    /// Enumeration for the sign restriction on a decision variable.
    /// urs = Unrestricted in Sign
    public enum VariableType
    {
        Positive,    // >= 0
        Negative,    // <= 0
        URS,         // Unrestricted
        Integer,     // Integer
        Binary       // 0 or 1
    }

    /// Represents a single constraint in the model.
    /// Example: +11x1 +8x2 <= 40
    public class Constraint
    {
        public List<double> Coefficients { get; set; } = new List<double>();
        public RelationType Relation { get; set; }
        public double RightHandSide { get; set; }
    }

    /// Represents the entire mathematical model parsed from the input file.
    /// This object is passed to the solver algorithms.
    public class LpModel
    {
        public ObjectiveType Type { get; set; }
        public List<double> ObjectiveCoefficients { get; set; } = new List<double>();
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public List<VariableType> VariableSignRestrictions { get; set; } = new List<VariableType>();

        public int VariableCount => ObjectiveCoefficients.Count;
        public int ConstraintCount => Constraints.Count;
    }
}
