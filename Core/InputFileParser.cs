using Project_LPR381.Exceptions;
using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Project_LPR381.Core
{
    /// Comprehensive input file parser for linear programming models.
    public class InputFileParser
    {
        private const double EPSILON = 1e-10;

        public LinearProgrammingModel ParseFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Input file not found: {filePath}");

            var allLines = File.ReadAllLines(filePath);
            var lines = allLines
                .Select((line, index) => new { Text = line.Trim(), Number = index + 1 })
                .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                .ToArray();

            if (lines.Length < 3)
            {
                throw new InvalidModelException(
                    $"Invalid file format: Expected at least 3 non-empty lines, but found {lines.Length}.");
            }

            var model = new LinearProgrammingModel { SourceFile = filePath };

            try
            {
                // Parse objective function (Line 1)
                ParseObjectiveFunction(lines[0].Text, lines[0].Number, model);

                // Parse constraints (Middle lines)
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    ParseConstraint(lines[i].Text, lines[i].Number, model);
                }

                // Parse sign restrictions (Last line)
                ParseSignRestrictions(lines[lines.Length - 1].Text, lines[lines.Length - 1].Number, model);

                CreateVariables(model);

                // (Validation is disabled as per previous request, but can be re-enabled here)
                // var validator = new ModelValidator();
                // validator.ValidateModel(model);

                return model;
            }
            catch (InvalidModelException)
            {
                // Re-throw our custom exceptions so the main program can display them
                throw;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during parsing
                throw new InvalidModelException($"An unexpected error occurred during parsing: {ex.Message}", ex);
            }
        }

        private void ParseObjectiveFunction(string line, int lineNumber, LinearProgrammingModel model)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new InvalidModelException($"Invalid objective function format.", lineNumber, "ParseError");

            string objectiveTypeStr = parts[0].ToUpper().Trim();

            // --- THIS IS THE CORRECTED CODE BLOCK ---
            // Using a classic switch statement for maximum compatibility.
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
                    // If the objective type is not recognized, throw a specific error.
                    throw new InvalidModelException($"Invalid objective type '{parts[0]}'. Expected MAX or MIN.", lineNumber, "ParseError");
            }
            model.ObjectiveCoefficients = ParseCoefficients(parts.Skip(1).ToArray(), lineNumber);
        }
        private void ParseConstraint(string line, int lineNumber, LinearProgrammingModel model)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                throw new InvalidModelException("Invalid constraint format. Expected coefficients, a relation (<=, >=, =), and a RHS value.", lineNumber);

            var validRelations = new[] { "<=", ">=", "=" };
            int relationIndex = Array.FindIndex(parts, validRelations.Contains);

            if (relationIndex == -1)
                throw new InvalidModelException("No valid relation (<=, >=, =) found in constraint.", lineNumber);

            if (relationIndex == 0 || relationIndex == parts.Length - 1)
                throw new InvalidModelException("Constraint relation must be between coefficients and the RHS value.", lineNumber);

            var coeffTokens = parts.Take(relationIndex).ToArray();
            var coefficients = ParseCoefficients(coeffTokens, lineNumber);

            string rhsToken = parts[relationIndex + 1];
            if (!double.TryParse(rhsToken, NumberStyles.Any, CultureInfo.InvariantCulture, out double rhs))
            {
                throw new InvalidModelException($"Could not parse the Right-Hand Side value '{rhsToken}' as a number.", lineNumber);
            }

            var constraint = new Constraint(coefficients, parts[relationIndex], rhs);
            model.Constraints.Add(constraint);
        }

        private static readonly HashSet<string> RestrictionKeywords = new HashSet<string>
        {
            "int", "integer", "nonneg", "nonnegative",
            "nonpos", "nonpositive", "urs", "free", "unrestricted",
            "bin", "binary"
        };

        private double[] ParseCoefficients(string[] tokens, int lineNumber)
        {
            var coefficients = new List<double>();
            foreach (var token in tokens)
            {
                Console.WriteLine($"DEBUG: Parsing token '{token}' on line {lineNumber}"); // Add this line

                string numericPart = token;

                if (token.Contains('x'))
                {
                    numericPart = token.Split('x')[0];
                }

                string lowered = token.ToLower().Trim();
                if (InputFileParser.RestrictionKeywords.Contains(lowered))
                {
                    continue;
                }

                if (double.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double coeff))
                {
                    coefficients.Add(coeff);
                }
                else
                {
                    Console.WriteLine($"DEBUG: Failed to parse '{numericPart}' from original token '{token}'"); // Add this line
                    throw new InvalidModelException($"Could not parse coefficient '{token}' on line {lineNumber}.", lineNumber);
                }
            }
            return coefficients.ToArray();
        }


        private void ParseSignRestrictions(string line, int lineNumber, LinearProgrammingModel model)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new InvalidModelException("No sign restrictions found on the last line.", lineNumber);

            foreach (var part in parts)
            {
                // Clean the input token to handle variations
                string trimmedPart = new string(part.ToLower().Trim().Where(char.IsLetterOrDigit).ToArray());
                SignRestriction restriction;

                Console.WriteLine($"DEBUG: Parsing sign restriction '{part}' -> '{trimmedPart}' on line {lineNumber}"); // Debug line

                // Using a classic switch statement for maximum compatibility.
                switch (trimmedPart)
                {
                    case "nonneg":
                    case "nonnegative":
                        restriction = SignRestriction.NonNegative;
                        break;
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
                        // If no match is found, throw a detailed error.
                        Console.WriteLine($"DEBUG: Failed to match sign restriction '{trimmedPart}'"); // Debug line
                        throw new InvalidModelException($"Invalid sign restriction '{part}' (processed as '{trimmedPart}').", lineNumber);
                }
                model.SignRestrictions.Add(restriction);
                Console.WriteLine($"DEBUG: Successfully added restriction: {restriction}"); // Debug line
            }
        }
        private void CreateVariables(LinearProgrammingModel model)
        {
            int variableCount = model.ObjectiveCoefficients.Length;
            foreach (var c in model.Constraints)
            {
                if (c.Coefficients.Length > variableCount)
                    variableCount = c.Coefficients.Length;
            }

            // Pad objective coefficients if necessary
            if (model.ObjectiveCoefficients.Length < variableCount)
            {
                var newObjCoeffs = new double[variableCount];
                Array.Copy(model.ObjectiveCoefficients, newObjCoeffs, model.ObjectiveCoefficients.Length);
                model.ObjectiveCoefficients = newObjCoeffs;
            }

            // Pad constraint coefficients if necessary
            foreach (var constraint in model.Constraints)
            {
                if (constraint.Coefficients.Length < variableCount)
                {
                    var newCoeffs = new double[variableCount];
                    Array.Copy(constraint.Coefficients, newCoeffs, constraint.Coefficients.Length);
                    constraint.Coefficients = newCoeffs;
                }
            }

            // Add default sign restrictions if not enough are provided
            while (model.SignRestrictions.Count < variableCount)
            {
                model.SignRestrictions.Add(SignRestriction.NonNegative);
            }

            // Create the Variable objects
            for (int i = 0; i < variableCount; i++)
            {
                model.Variables.Add(new Variable($"x{i + 1}", model.SignRestrictions[i], i));
            }
        }
    }
}
