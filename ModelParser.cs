using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OperationsResearchSolver;

namespace Project_LPR381
{
    /// Parses an input text file into a structured LpModel object.
    public static class ModelParser
    {
        /// Reads the specified file and parses it into a mathematical model.
        /// <param name="filePath">The path to the input text file.</param>
        /// <returns>An LpModel object representing the problem.</returns>
        public static LpModel ParseFile(string filePath)
        {
            var model = new LpModel();
            var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            if (lines.Length < 3)
            {
                throw new ArgumentException("Input file must contain at least 3 lines (objective, one constraint, signs).");
            }

            // 1. Parse the Objective Function (first line)
            ParseObjectiveFunction(lines[0], model);

            // 2. Parse the Sign Restrictions (last line)
            ParseSignRestrictions(lines[lines.Length - 1], model);

            // 3. Parse Constraints (all lines between first and last)
            for (int i = 1; i < lines.Length - 1; i++)
            {
                ParseConstraint(lines[i], model);
            }

            return model;
        }

        /// Parses the first line of the file for objective type and coefficients.
        /// Example: max +2 +3 +5
        private static void ParseObjectiveFunction(string line, LpModel model)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Set objective type
            model.Type = parts[0].ToLower() == "max" ? ObjectiveType.Max : ObjectiveType.Min;

            // Loop through coefficients in pairs (operator, value)
            for (int i = 1; i < parts.Length; i += 2)
            {
                double sign = parts[i] == "+" ? 1.0 : -1.0;
                double coeff = double.Parse(parts[i + 1]);
                model.ObjectiveCoefficients.Add(sign * coeff);
            }
        }

        /// Parses a constraint line.
        /// Example: +11 +8 +6 <= 40
        private static void ParseConstraint(string line, LpModel model)
        {
            var constraint = new Constraint();
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int varCount = model.VariableCount;
            // A constraint line must have 2*VarCount + 2 parts (signs, coeffs, relation, RHS)
            if (parts.Length != 2 * varCount + 2)
            {
                throw new FormatException($"Constraint format error. Expected {2 * varCount + 2} parts, but found {parts.Length}. Line: '{line}'");
            }

            // Parse technological coefficients
            for (int i = 0; i < 2 * varCount; i += 2)
            {
                double sign = parts[i] == "+" ? 1.0 : -1.0;
                double coeff = double.Parse(parts[i + 1]);
                constraint.Coefficients.Add(sign * coeff);
            }

            // Parse relation
            string relationStr = parts[parts.Length - 2];
            if (relationStr == "<=") constraint.Relation = RelationType.LessThanOrEqual;
            else if (relationStr == ">=") constraint.Relation = RelationType.GreaterThanOrEqual;
            else if (relationStr == "=") constraint.Relation = RelationType.EqualTo;
            else throw new FormatException($"Unknown relation operator: {relationStr}");

            // Parse Right-Hand-Side (RHS)
            constraint.RightHandSide = double.Parse(parts[parts.Length - 1]);

            model.Constraints.Add(constraint);
        }

        /// Parses the last line for variable sign restrictions.
        /// Example: bin bin bin
        private static void ParseSignRestrictions(string line, LpModel model)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != model.VariableCount)
            {
                throw new FormatException("The number of sign restrictions must match the number of decision variables.");
            }

            foreach (string part in parts)
            {
                switch (part.ToLower())
                {
                    case "+": model.VariableSignRestrictions.Add(VariableType.Positive); break;
                    case "-": model.VariableSignRestrictions.Add(VariableType.Negative); break;
                    case "urs": model.VariableSignRestrictions.Add(VariableType.URS); break;
                    case "int": model.VariableSignRestrictions.Add(VariableType.Integer); break;
                    case "bin": model.VariableSignRestrictions.Add(VariableType.Binary); break;
                    default: throw new FormatException($"Unknown variable type: {part}");
                }
            }
        }
    }
}
