using Project_LPR31.Exceptions;
using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR31.Core
{
    /// Comprehensive input file parser for linear programming models
    /// Handles random numbers of variables and constraints with robust error detection
    public class InputFileParser
    {
        private const double EPSILON = 1e-10;

        /// Parse input file and create linear programming model with comprehensive error handling
        /// <param name="filePath">Path to the input file</param>
        /// <returns>Parsed linear programming model</returns>
        public LinearProgrammingModel ParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Input file not found: {filePath}");

            try
            {
                // Read all lines and filter out empty ones
                var allLines = File.ReadAllLines(filePath);
                var lines = allLines
                    .Select((line, index) => new { Line = line.Trim(), Index = index + 1 })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Line))
                    .ToArray();

                if (lines.Length < 3)
                {
                    throw new InvalidModelException(
                        $"Invalid file format: Expected at least 3 non-empty lines (objective, constraints, sign restrictions), found {lines.Length}");
                }

                var model = new LinearProgrammingModel { SourceFile = filePath };

                // Parse objective function (first line)
                ParseObjectiveFunction(lines[0].Line, lines[0].Index, model);

                // Parse constraints (middle lines)
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    ParseConstraint(lines[i].Line, lines[i].Index, model, i);
                }

                // Parse sign restrictions (last line)
                ParseSignRestrictions(lines[lines.Length - 1].Line, lines[lines.Length - 1].Index, model);

                // Create variables and validate model consistency
                CreateVariables(model);

                // Validate with ModelValidator
                var validator = new ModelValidator();
                validator.ValidateModel(model);

                return model;
            }
            catch (IOException ex)
            {
                throw new InvalidModelException($"Error reading file: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidModelException($"Access denied to file: {ex.Message}", ex);
            }
        }

        /// Parse objective function from first line with comprehensive validation
        private void ParseObjectiveFunction(string line, int lineNumber, LinearProgrammingModel model)
        {
            try
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    throw new InvalidModelException($"Line {lineNumber}: Invalid objective function format. Expected 'MAX/MIN coefficient1 coefficient2 ...'");
                }

                // Parse objective type
                string objectiveTypeStr = parts[0].ToUpper().Trim();
                switch (objectiveTypeStr)
                {
                    case "MAX":
                    case "MAXIMIZE":
                        model.ObjectiveType = ObjectiveType.Maximize;
                        break;
                    case "MIN":
                    case "MINIMIZE":
                        model.ObjectiveType = ObjectiveType.Minimize;
                        break;
                    default:
                        model.ParsingErrors.Add($"Line {lineNumber}: Invalid objective type '{parts[0]}'. Expected MAX, MIN, MAXIMIZE, or MINIMIZE");
                        model.ObjectiveType = ObjectiveType.Maximize; // Default fallback
                        break;
                }

                // Parse coefficients
                var coefficients = new List<double>();
                for (int i = 1; i < parts.Length; i++)
                {
                    if (double.TryParse(parts[i], out double coeff))
                    {
                        coefficients.Add(coeff);
                    }
                    else
                    {
                        model.ParsingErrors.Add($"Line {lineNumber}: Invalid coefficient '{parts[i]}' at position {i}");
                        coefficients.Add(0.0); // Add zero as fallback
                    }
                }

                if (coefficients.Count == 0)
                {
                    throw new InvalidModelException($"Line {lineNumber}: No valid coefficients found in objective function");
                }

                model.ObjectiveCoefficients = coefficients.ToArray();

                // Warn about zero coefficients
                int zeroCount = coefficients.Count(c => Math.Abs(c) < EPSILON);
                if (zeroCount > 0)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Warning - {zeroCount} zero coefficient(s) found in objective function");
                }
            }
            catch (Exception ex) when (!(ex is InvalidModelException))
            {
                throw new InvalidModelException($"Line {lineNumber}: Error parsing objective function - {ex.Message}", ex);
            }
        }

        /// Parse constraint from line with comprehensive validation
        private void ParseConstraint(string line, int lineNumber, LinearProgrammingModel model, int constraintIndex)
        {
            try
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Invalid constraint format. Expected 'coeff1 coeff2 ... relation rhs'");
                    return;
                }

                // Find relation position (<=, >=, =)
                int relationIndex = -1;
                string relation = null;
                var validRelations = new[] { "<=", ">=", "=" };

                for (int i = 0; i < parts.Length; i++)
                {
                    if (validRelations.Contains(parts[i]))
                    {
                        relationIndex = i;
                        relation = parts[i];
                        break;
                    }
                }

                if (relationIndex == -1)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: No valid relation (<=, >=, =) found in constraint");
                    return;
                }

                if (relationIndex == 0)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: No coefficients found before relation operator");
                    return;
                }

                if (relationIndex == parts.Length - 1)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: No RHS value found after relation operator");
                    return;
                }

                // Parse coefficients
                var coefficients = new List<double>();
                for (int i = 0; i < relationIndex; i++)
                {
                    if (parts[i] == "+") continue; // NEW: skip lone plus

                    if (double.TryParse(parts[i], out double coeff))
                    {
                        coefficients.Add(coeff);
                    }
                    else
                    {
                        model.ParsingErrors.Add($"Line {lineNumber}: Invalid coefficient '{parts[i]}' at position {i + 1}");
                        coefficients.Add(0.0); // Add zero as fallback
                    }
                }

                // Validate coefficient count consistency
                if (model.ObjectiveCoefficients.Length > 0 && coefficients.Count != model.ObjectiveCoefficients.Length)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Constraint has {coefficients.Count} coefficients, but objective function has {model.ObjectiveCoefficients.Length}. Model may be inconsistent.");
                }

                // Parse RHS value
                if (!double.TryParse(parts[relationIndex + 1], out double rhs))
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Invalid RHS value '{parts[relationIndex + 1]}'");
                    rhs = 0.0; // Fallback value
                }

                // Check for additional unexpected data after RHS
                if (parts.Length > relationIndex + 2)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Warning - Extra data found after RHS value: {string.Join(" ", parts.Skip(relationIndex + 2))}");
                }

                // Warn about negative RHS for <= constraints
                if (relation == "<=" && rhs < 0)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Warning - Negative RHS ({rhs}) with <= constraint may indicate infeasible region");
                }

                // Create constraint
                try
                {
                    var constraint = new Constraint(coefficients.ToArray(), relation, rhs);
                    model.Constraints.Add(constraint);
                }
                catch (ArgumentException ex)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Error creating constraint - {ex.Message}");
                }

                // Warn about all-zero constraint coefficients
                if (coefficients.All(c => Math.Abs(c) < EPSILON))
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Warning - All coefficients are zero in constraint {constraintIndex}");
                }
            }
            catch (Exception ex)
            {
                model.ParsingErrors.Add($"Line {lineNumber}: Unexpected error parsing constraint - {ex.Message}");
            }
        }

        /// Parse sign restrictions from last line with comprehensive validation
        private void ParseSignRestrictions(string line, int lineNumber, LinearProgrammingModel model)
        {
            try
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                {
                    throw new InvalidModelException($"Line {lineNumber}: No sign restrictions found");
                }

                // Validate count matches objective function variables
                if (model.ObjectiveCoefficients.Length > 0 && parts.Length != model.ObjectiveCoefficients.Length)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Found {parts.Length} sign restrictions, but objective function has {model.ObjectiveCoefficients.Length} variables");
                }

                foreach (var part in parts)
                {
                    SignRestriction restriction;
                    string trimmedPart = part.ToLower().Trim();

                    switch (trimmedPart)
                    {
                        case "+":
                        case "nonneg":
                        case "nonnegative":
                            restriction = SignRestriction.NonNegative;
                            break;
                        case "-":
                        case "nonpos":
                        case "nonpositive":
                            restriction = SignRestriction.NonPositive;
                            break;
                        case "urs":
                        case "free":
                        case "unrestricted":
                            restriction = SignRestriction.Unrestricted;
                            break;
                        case "int":
                        case "integer":
                            restriction = SignRestriction.Integer;
                            break;
                        case "bin":
                        case "binary":
                            restriction = SignRestriction.Binary;
                            break;
                        default:
                            model.ParsingErrors.Add($"Line {lineNumber}: Invalid sign restriction '{part}'. Valid options: +, -, urs, int, bin");
                            restriction = SignRestriction.NonNegative; // Default fallback
                            break;
                    }

                    model.SignRestrictions.Add(restriction);
                }

                // Analyze variable type distribution
                var restrictionCounts = model.SignRestrictions
                    .GroupBy(r => r)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Add informational messages about variable types
                if (restrictionCounts.ContainsKey(SignRestriction.Integer))
                {
                    model.ParsingErrors.Add($"Info: Model contains {restrictionCounts[SignRestriction.Integer]} integer variable(s) - requires integer programming methods");
                }

                if (restrictionCounts.ContainsKey(SignRestriction.Binary))
                {
                    model.ParsingErrors.Add($"Info: Model contains {restrictionCounts[SignRestriction.Binary]} binary variable(s) - may benefit from specialized algorithms");
                }

                if (restrictionCounts.ContainsKey(SignRestriction.Unrestricted))
                {
                    model.ParsingErrors.Add($"Info: Model contains {restrictionCounts[SignRestriction.Unrestricted]} unrestricted variable(s) - will require variable substitution");
                }
            }
            catch (Exception ex) when (!(ex is InvalidModelException))
            {
                throw new InvalidModelException($"Line {lineNumber}: Error parsing sign restrictions - {ex.Message}", ex);
            }
        }

        /// Create variables for the model based on parsed data
        private void CreateVariables(LinearProgrammingModel model)
        {
            int variableCount = Math.Max(model.ObjectiveCoefficients.Length, model.SignRestrictions.Count);

            for (int i = 0; i < variableCount; i++)
            {
                SignRestriction restriction = i < model.SignRestrictions.Count
                    ? model.SignRestrictions[i]
                    : SignRestriction.NonNegative; // Default for missing restrictions

                var variable = new Variable($"x{i + 1}", restriction, i);
                model.Variables.Add(variable);
            }

            // Ensure all arrays have consistent lengths
            if (model.ObjectiveCoefficients.Length < variableCount)
            {
                var newCoeffs = new double[variableCount];
                Array.Copy(model.ObjectiveCoefficients, newCoeffs, model.ObjectiveCoefficients.Length);
                model.ObjectiveCoefficients = newCoeffs;
                model.ParsingErrors.Add($"Warning: Padded objective coefficients with zeros to match variable count ({variableCount})");
            }

            while (model.SignRestrictions.Count < variableCount)
            {
                model.SignRestrictions.Add(SignRestriction.NonNegative);
                model.ParsingErrors.Add($"Warning: Added default non-negative restriction for variable x{model.SignRestrictions.Count}");
            }
        }
    }
}
