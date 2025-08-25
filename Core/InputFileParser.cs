using Project_LPR381.Exceptions;
using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Project_LPR381.Core
{
    /// Comprehensive input file parser for linear programming models
    /// Handles random numbers of variables and constraints with robust error detection
    public class InputFileParser
    {
        private const double EPSILON = 1e-10;
        public LinearProgrammingModel ParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Input file not found: {filePath}");

            try
            {
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

                // Parse objective function
                ParseObjectiveFunction(lines[0].Line, lines[0].Index, model);

                // Parse constraints
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    ParseConstraint(lines[i].Line, lines[i].Index, model, i);
                }

                // Parse sign restrictions
                ParseSignRestrictions(lines[lines.Length - 1].Line, lines[lines.Length - 1].Index, model);

                // Create variables and validate
                CreateVariables(model);

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

        /// Parse objective function
        private void ParseObjectiveFunction(string line, int lineNumber, LinearProgrammingModel model)
        {
            try
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    throw new InvalidModelException($"Line {lineNumber}: Invalid objective function format.");

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
                        model.ParsingErrors.Add($"Line {lineNumber}: Invalid objective type '{parts[0]}'");
                        model.ObjectiveType = ObjectiveType.Maximize;
                        break;
                }

                // Parse coefficients
                var coefficients = ParseCoefficients(parts.Skip(1).ToArray(), lineNumber, model);
                model.ObjectiveCoefficients = coefficients;

                int zeroCount = coefficients.Count(c => Math.Abs(c) < EPSILON);
                if (zeroCount > 0)
                    model.ParsingErrors.Add($"Line {lineNumber}: Warning - {zeroCount} zero coefficient(s) found in objective function");
            }
            catch (Exception ex) when (!(ex is InvalidModelException))
            {
                throw new InvalidModelException($"Line {lineNumber}: Error parsing objective function - {ex.Message}", ex);
            }
        }

        /// Parse constraint
        private void ParseConstraint(string line, int lineNumber, LinearProgrammingModel model, int constraintIndex)
        {
            try
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Invalid constraint format.");
                    return;
                }

                // Find relation
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
                    model.ParsingErrors.Add($"Line {lineNumber}: No valid relation found.");
                    return;
                }

                // Parse coefficients
                var coeffTokens = parts.Take(relationIndex).ToArray();
                var coefficients = ParseCoefficients(coeffTokens, lineNumber, model);

                // RHS
                if (!double.TryParse(parts[relationIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double rhs))
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Invalid RHS value '{parts[relationIndex + 1]}'");
                    rhs = 0.0;
                }

                var constraint = new Constraint(coefficients, relation, rhs);
                model.Constraints.Add(constraint);

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

        /// General coefficient parser supporting both formats
        private double[] ParseCoefficients(string[] tokens, int lineNumber, LinearProgrammingModel model)
        {
            var coefficients = new List<double>();

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                double coeff = 0.0;

                // Case A: token like "2x1"
                if (token.Contains("x"))
                {
                    var split = token.Split('x');
                    if (split.Length == 2 &&
                        double.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out coeff))
                    {
                        coefficients.Add(coeff);
                        continue;
                    }
                }

                // Case B: token is just a number
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out coeff))
                {
                    coefficients.Add(coeff);
                }
                else
                {
                    model.ParsingErrors.Add($"Line {lineNumber}: Invalid coefficient '{token}' at position {i + 1}");
                    coefficients.Add(0.0);
                }
            }

            return coefficients.ToArray();
        }

        /// Parse sign restrictions
        private void ParseSignRestrictions(string line, int lineNumber, LinearProgrammingModel model)
        {
            try
            {
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                    throw new InvalidModelException($"Line {lineNumber}: No sign restrictions found");

                foreach (var part in parts)
                {
                    SignRestriction restriction;
                    string trimmedPart = new string(part.ToLower().Trim().Where(char.IsLetter).ToArray());

                    switch (trimmedPart)
                    {
                        case "+":
                        case "nonneg":
                        case "nonnegative":
                            restriction = SignRestriction.NonNegative; break;
                        case "-":
                        case "nonpos":
                        case "nonpositive":
                            restriction = SignRestriction.NonPositive; break;
                        case "urs":
                        case "free":
                        case "unrestricted":
                            restriction = SignRestriction.Unrestricted; break;
                        case "int":
                        case "integer":
                            restriction = SignRestriction.Integer; break;
                        case "bin":
                        case "binary":
                            restriction = SignRestriction.Binary; break;
                        default:
                            model.ParsingErrors.Add($"Line {lineNumber}: Invalid sign restriction '{part}'");
                            restriction = SignRestriction.NonNegative;
                            break;
                    }

                    model.SignRestrictions.Add(restriction);
                }
            }
            catch (Exception ex) when (!(ex is InvalidModelException))
            {
                throw new InvalidModelException($"Line {lineNumber}: Error parsing sign restrictions - {ex.Message}", ex);
            }
        }

        /// Create variables
        private void CreateVariables(LinearProgrammingModel model)
        {
            int variableCount = Math.Max(model.ObjectiveCoefficients.Length, model.SignRestrictions.Count);

            for (int i = 0; i < variableCount; i++)
            {
                SignRestriction restriction = i < model.SignRestrictions.Count
                    ? model.SignRestrictions[i]
                    : SignRestriction.NonNegative;

                var variable = new Variable($"x{i + 1}", restriction, i);
                model.Variables.Add(variable);
            }

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
