using Project_LPR31.Exceptions;
using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Project_LPR31.Core
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
                // Check for empty model
                if (model.Variables.Count == 0)
                {
                    throw new InvalidModelException("Model contains no variables");
                }

                if (model.Constraints.Count == 0)
                {
                    model.ParsingErrors.Add("Warning: Model contains no constraints - problem may be unbounded");
                }

                // Validate constraint coefficient dimensions
                ValidateConstraintDimensions(model);

                // Check for potential infeasibility indicators
                DetectPotentialInfeasibility(model);

                // Check for potential unboundedness indicators  
                DetectPotentialUnboundedness(model);

                // Analyze constraint matrix properties
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

                    // Resize coefficient array
                    var newCoefficients = new double[expectedVarCount];
                    Array.Copy(constraint.Coefficients, newCoefficients, Math.Min(constraint.Coefficients.Length, expectedVarCount));
                    constraint.Coefficients = newCoefficients;

                    // Write back to the model
                    model.Constraints[i] = constraint;
                }
            }
        }

        /// Detect potential infeasibility in the model
        private void DetectPotentialInfeasibility(LinearProgrammingModel model)
        {
            // Check for obviously contradictory constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];

                // Check for contradictory simple bounds
                if (constraint.Coefficients.Count(c => Math.Abs(c) > EPSILON) == 1)
                {
                    int varIndex = Array.FindIndex(constraint.Coefficients, c => Math.Abs(c) > EPSILON);
                    if (varIndex >= 0)
                    {
                        double coeff = constraint.Coefficients[varIndex];
                        double bound = constraint.RightHandSide / coeff;

                        var varRestriction = model.SignRestrictions[varIndex];

                        // Check for obvious conflicts
                        if ((constraint.Relation == "<=" && coeff > 0 && bound < 0 && varRestriction == SignRestriction.NonNegative) ||
                            (constraint.Relation == ">=" && coeff > 0 && bound > 0 && varRestriction == SignRestriction.NonPositive))
                        {
                            model.ParsingErrors.Add($"Warning: Constraint {i + 1} may be infeasible with variable x{varIndex + 1} sign restriction");
                        }
                    }
                }
            }

            // Check for constraints with negative RHS values
            var negativeRHSCount = model.Constraints.Count(c => c.Relation == "<=" && c.RightHandSide < -EPSILON);
            if (negativeRHSCount > 0)
            {
                model.ParsingErrors.Add($"Warning: {negativeRHSCount} constraint(s) with negative RHS values may indicate infeasibility");
            }
        }

        /// Detect potential unboundedness in the model
        private void DetectPotentialUnboundedness(LinearProgrammingModel model)
        {
            if (model.Constraints.Count == 0)
            {
                model.ParsingErrors.Add("Warning: No constraints present - optimization problem may be unbounded");
                return;
            }

            // Check for variables that may be unbounded
            for (int varIndex = 0; varIndex < model.Variables.Count; varIndex++)
            {
                double objCoeff = model.ObjectiveCoefficients[varIndex];

                if (Math.Abs(objCoeff) < EPSILON) continue;

                bool hasLimitingConstraint = false;

                foreach (var constraint in model.Constraints)
                {
                    double constraintCoeff = constraint.Coefficients[varIndex];

                    if (Math.Abs(constraintCoeff) < EPSILON) continue;

                    // Check if constraint limits the variable in the direction of improvement
                    bool limitsGrowth = false;

                    if (model.ObjectiveType == ObjectiveType.Maximize && objCoeff > 0)
                    {
                        limitsGrowth = (constraintCoeff > 0 && constraint.Relation == "<=") ||
                                     (constraintCoeff < 0 && constraint.Relation == ">=");
                    }
                    else if (model.ObjectiveType == ObjectiveType.Minimize && objCoeff < 0)
                    {
                        limitsGrowth = (constraintCoeff > 0 && constraint.Relation == "<=") ||
                                     (constraintCoeff < 0 && constraint.Relation == ">=");
                    }

                    if (limitsGrowth)
                    {
                        hasLimitingConstraint = true;
                        break;
                    }
                }

                if (!hasLimitingConstraint && model.SignRestrictions[varIndex] != SignRestriction.NonPositive)
                {
                    model.ParsingErrors.Add($"Warning: Variable x{varIndex + 1} may be unbounded - no limiting constraints found");
                }
            }
        }

        /// Analyze constraint matrix properties
        private void AnalyzeConstraintMatrix(LinearProgrammingModel model)
        {
            if (model.Constraints.Count == 0) return;

            int m = model.Constraints.Count; // Number of constraints
            int n = model.Variables.Count;   // Number of variables

            // Check matrix dimensions
            model.ParsingErrors.Add($"Info: Constraint matrix dimensions: {m} × {n}");

            // Count constraint types
            var relationCounts = model.GetConstraintTypeDistribution();
            foreach (var kvp in relationCounts)
            {
                model.ParsingErrors.Add($"Info: {kvp.Value} constraint(s) with relation '{kvp.Key}'");
            }

            // Check for zero rows and columns
            CheckZeroRowsAndColumns(model, m, n);

            // Basic rank analysis warning
            if (m > n)
            {
                model.ParsingErrors.Add($"Info: More constraints ({m}) than variables ({n}) - system may be over-constrained");
            }
            else if (m < n)
            {
                model.ParsingErrors.Add($"Info: More variables ({n}) than constraints ({m}) - system likely under-constrained");
            }
        }

        /// Check for zero rows and columns in constraint matrix
        private void CheckZeroRowsAndColumns(LinearProgrammingModel model, int m, int n)
        {
            // Check for zero rows (all-zero constraints)
            int zeroRowCount = 0;
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                if (model.Constraints[i].Coefficients.All(c => Math.Abs(c) < EPSILON))
                {
                    zeroRowCount++;
                    if (Math.Abs(model.Constraints[i].RightHandSide) > EPSILON)
                    {
                        model.ParsingErrors.Add($"Warning: Constraint {i + 1} has all-zero coefficients but non-zero RHS - may be infeasible");
                    }
                }
            }

            if (zeroRowCount > 0)
            {
                model.ParsingErrors.Add($"Info: {zeroRowCount} constraint(s) with all-zero coefficients detected");
            }

            // Check for zero columns (variables that don't appear in constraints)
            int zeroColCount = 0;
            for (int j = 0; j < n; j++)
            {
                bool appearsInConstraints = model.Constraints.Any(c => Math.Abs(c.Coefficients[j]) > EPSILON);
                if (!appearsInConstraints)
                {
                    zeroColCount++;
                    if (Math.Abs(model.ObjectiveCoefficients[j]) > EPSILON)
                    {
                        model.ParsingErrors.Add($"Warning: Variable x{j + 1} appears in objective but not in any constraints - may be unbounded");
                    }
                }
            }

            if (zeroColCount > 0)
            {
                model.ParsingErrors.Add($"Info: {zeroColCount} variable(s) do not appear in any constraints");
            }
        }
    }
}
