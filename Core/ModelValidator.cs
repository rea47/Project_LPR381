using Project_LPR381.Exceptions;
using Project_LPR381.Models;
using System;
using System.Linq;

namespace Project_LPR381.Core
{
    /// Validates linear programming models and detects potential issues
    public class ModelValidator
    {
        private const double EPSILON = 1e-10;

        /// Validate overall model consistency and detect potential issues
        public void ValidateModel(LinearProgrammingModel model)
        {
            try
            {
                if (model.Variables.Count == 0)
                    throw new InvalidModelException("Model contains no variables");

                if (model.Constraints.Count == 0)
                    model.ParsingErrors.Add("Warning: Model contains no constraints - problem may be unbounded");

                ValidateConstraintDimensions(model);
                DetectPotentialInfeasibility(model);
                DetectPotentialUnboundedness(model);
                AnalyzeConstraintMatrix(model);
            }
            catch (Exception ex) when (!(ex is InvalidModelException))
            {
                model.ParsingErrors.Add($"Error during model validation: {ex.Message}");
            }
        }

        /// Validate constraint coefficient dimensions
        private void ValidateConstraintDimensions(LinearProgrammingModel model)
        {
            int expectedVarCount = model.Variables.Count;
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];
                if (constraint.Coefficients.Length != expectedVarCount)
                {
                    model.ParsingErrors.Add($"Constraint {i + 1}: Has {constraint.Coefficients.Length} coefficients, expected {expectedVarCount}");

                    var newCoefficients = new double[expectedVarCount];
                    Array.Copy(constraint.Coefficients, newCoefficients, Math.Min(constraint.Coefficients.Length, expectedVarCount));
                    constraint.Coefficients = newCoefficients;
                    model.Constraints[i] = constraint;
                }
            }
        }

        /// Detect potential infeasibility
        private void DetectPotentialInfeasibility(LinearProgrammingModel model)
        {
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];

                // Single-variable bounds check
                if (constraint.Coefficients.Count(c => Math.Abs(c) > EPSILON) == 1)
                {
                    int varIndex = Array.FindIndex(constraint.Coefficients, c => Math.Abs(c) > EPSILON);
                    if (varIndex >= 0)
                    {
                        double coeff = constraint.Coefficients[varIndex];
                        double bound = constraint.RightHandSide / coeff;
                        var varRestriction = model.SignRestrictions[varIndex];

                        if ((constraint.Relation == "<=" && coeff > 0 && bound < 0 && varRestriction == SignRestriction.NonNegative) ||
                            (constraint.Relation == ">=" && coeff > 0 && bound > 0 && varRestriction == SignRestriction.NonPositive))
                        {
                            model.ParsingErrors.Add($"Warning: Constraint {i + 1} may be infeasible with variable x{varIndex + 1} sign restriction");
                        }
                    }
                }
            }

            // Negative RHS check
            int negativeRHSCount = model.Constraints.Count(c => c.Relation == "<=" && c.RightHandSide < -EPSILON);
            if (negativeRHSCount > 0)
                model.ParsingErrors.Add($"Warning: {negativeRHSCount} constraint(s) with negative RHS values may indicate infeasibility");
        }

        /// Detect potential unboundedness
        private void DetectPotentialUnboundedness(LinearProgrammingModel model)
        {
            if (model.Constraints.Count == 0)
            {
                model.ParsingErrors.Add("Warning: No constraints present - optimization problem may be unbounded");
                return;
            }

            for (int varIndex = 0; varIndex < model.Variables.Count; varIndex++)
            {
                double objCoeff = model.ObjectiveCoefficients[varIndex];
                if (Math.Abs(objCoeff) < EPSILON) continue;

                bool hasLimitingConstraint = model.Constraints.Any(constraint =>
                {
                    double c = constraint.Coefficients[varIndex];
                    if (Math.Abs(c) < EPSILON) return false;

                    if (model.ObjectiveType == ObjectiveType.Maximize && objCoeff > 0)
                        return (c > 0 && constraint.Relation == "<=") || (c < 0 && constraint.Relation == ">=");
                    if (model.ObjectiveType == ObjectiveType.Minimize && objCoeff < 0)
                        return (c > 0 && constraint.Relation == "<=") || (c < 0 && constraint.Relation == ">=");

                    return false;
                });

                if (!hasLimitingConstraint && model.SignRestrictions[varIndex] != SignRestriction.NonPositive)
                    model.ParsingErrors.Add($"Warning: Variable x{varIndex + 1} may be unbounded - no limiting constraints found");
            }
        }

        /// Analyze constraint matrix
        private void AnalyzeConstraintMatrix(LinearProgrammingModel model)
        {
            if (model.Constraints.Count == 0) return;

            int m = model.Constraints.Count;
            int n = model.Variables.Count;

            model.ParsingErrors.Add($"Info: Constraint matrix dimensions: {m} × {n}");

            var relationCounts = model.GetConstraintTypeDistribution();
            foreach (var kvp in relationCounts)
                model.ParsingErrors.Add($"Info: {kvp.Value} constraint(s) with relation '{kvp.Key}'");

            CheckZeroRowsAndColumns(model, m, n);

            if (m > n)
                model.ParsingErrors.Add($"Info: More constraints ({m}) than variables ({n}) - system may be over-constrained");
            else if (m < n)
                model.ParsingErrors.Add($"Info: More variables ({n}) than constraints ({m}) - system likely under-constrained");
        }

        /// Check zero rows & columns
        private void CheckZeroRowsAndColumns(LinearProgrammingModel model, int m, int n)
        {
            // Zero-row constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                if (model.Constraints[i].Coefficients.All(c => Math.Abs(c) < EPSILON))
                {
                    if (Math.Abs(model.Constraints[i].RightHandSide) > EPSILON)
                        model.ParsingErrors.Add($"Warning: Constraint {i + 1} has all-zero coefficients but non-zero RHS - may be infeasible");
                    else
                        model.ParsingErrors.Add($"Info: Constraint {i + 1} has all-zero coefficients (may be redundant)");
                }
            }

            // Zero-column variables
            for (int j = 0; j < n; j++)
            {
                bool appearsInConstraints = model.Constraints.Any(c => Math.Abs(c.Coefficients[j]) > EPSILON);
                if (!appearsInConstraints)
                {
                    if (Math.Abs(model.ObjectiveCoefficients[j]) > EPSILON)
                        model.ParsingErrors.Add($"Warning: Variable x{j + 1} appears in objective but not in any constraints - may be unbounded");
                    else
                        model.ParsingErrors.Add($"Info: Variable x{j + 1} does not appear in any constraints (redundant)");
                }
            }
        }
    }
}
