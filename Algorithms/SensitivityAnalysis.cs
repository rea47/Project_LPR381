using Project_LPR381.Models;
using Project_LPR381.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project_LPR381.Algorithms
{
    public class SensitivityAnalysis
    {
        // Change the type of the 'solution' field
        private readonly PrimalSimplex.Result solution;
        private readonly LinearProgrammingModel model;

        // Change the type of the 'solution' parameter in the constructor
        public SensitivityAnalysis(PrimalSimplex.Result solution, LinearProgrammingModel model)
        {
            if (solution == null || !solution.IsOptimal || solution.LastTableau == null)
            {
                throw new ArgumentException("A valid optimal solution with its final tableau is required for sensitivity analysis.");
            }
            this.solution = solution;
            this.model = model;
        }
        #region --- Public Methods ---

        /// The main menu for performing different types of sensitivity analysis.
        public void PerformAnalysis()
        {
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== Sensitivity Analysis Menu ===");
                Console.WriteLine("1. Ranges for Objective Coefficients (Basic Variables)");
                Console.WriteLine("2. Ranges for Objective Coefficients (Non-Basic Variables)");
                Console.WriteLine("3. Ranges for Constraint RHS Values (Feasibility)");
                Console.WriteLine("4. Calculate Shadow Prices");
                Console.WriteLine("5. Return to Main Menu");
                Console.Write("Select an option: ");
                string choice = Console.ReadLine();

                Console.Clear();
                Console.WriteLine("--- Analysis Results ---");
                switch (choice)
                {
                    case "1":
                        ComputeRangesForBasicVariables();
                        break;
                    case "2":
                        ComputeRangesForNonBasicVariables();
                        break;
                    case "3":
                        ComputeRangesForConstraints();
                        break;
                    case "4":
                        ComputeShadowPrices();
                        break;
                    case "5":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }

                if (!exit)
                {
                    Console.WriteLine("\nPress any key to return to the menu...");
                    Console.ReadKey();
                }
            }
        }

        #endregion

        #region --- Core Calculation Methods ---

        /// Calculates and displays the allowable range for the objective function
        /// coefficient of each variable that is currently BASIC in the optimal solution.
        public void ComputeRangesForBasicVariables()
        {
            Console.WriteLine("Ranges of Optimality for Objective Coefficients (Basic Variables):");
            int numVars = solution.ColumnNames.Length;
            int numRows = solution.RowNames.Length;
            var zRow = GetRow(solution.LastTableau, numRows);

            for (int i = 0; i < numRows; i++)
            {
                string basicVarName = solution.RowNames[i];
                int basicVarIndex = Array.IndexOf(solution.ColumnNames, basicVarName);

                // Skip slack/surplus variables if they are not original decision variables
                if (!basicVarName.StartsWith("x")) continue;

                double currentCoeff = model.ObjectiveCoefficients[basicVarIndex];
                var tableauRow = GetRow(solution.LastTableau, i);

                double allowableIncrease = double.PositiveInfinity;
                double allowableDecrease = double.PositiveInfinity;

                // The change in a basic variable's coefficient affects the reduced cost of all non-basic variables.
                // We must ensure all reduced costs remain non-negative (for a maximization problem).
                // Δ <= (z_j - c_j) / a_ij  for a_ij > 0
                // Δ >= (z_j - c_j) / a_ij  for a_ij < 0
                for (int j = 0; j < numVars; j++)
                {
                    // Only consider non-basic variables
                    if (solution.RowNames.Contains(solution.ColumnNames[j])) continue;

                    double reducedCost = zRow[j];
                    double a_ij = tableauRow[j];

                    if (Math.Abs(a_ij) < 1e-9) continue;

                    if (a_ij > 0)
                    {
                        allowableIncrease = Math.Min(allowableIncrease, reducedCost / a_ij);
                    }
                    else // a_ij < 0
                    {
                        allowableDecrease = Math.Min(allowableDecrease, -reducedCost / a_ij);
                    }
                }

                Console.WriteLine($"  - Variable {basicVarName}:");
                Console.WriteLine($"    Current Coefficient: {currentCoeff}");
                Console.WriteLine($"    Allowable Increase:  {(allowableIncrease == double.PositiveInfinity ? "Infinity" : allowableIncrease.ToString("F3"))}");
                Console.WriteLine($"    Allowable Decrease:  {(allowableDecrease == double.PositiveInfinity ? "Infinity" : allowableDecrease.ToString("F3"))}");
                Console.WriteLine($"    Range:               [{currentCoeff - allowableDecrease:F3}, {currentCoeff + allowableIncrease:F3}]");
            }
        }

        /// Calculates and displays the allowable range for the objective function
        /// coefficient of each variable that is NON-BASIC in the optimal solution.
        public void ComputeRangesForNonBasicVariables()
        {
            Console.WriteLine("Ranges of Insignificance for Objective Coefficients (Non-Basic Variables):");
            int numVars = solution.ColumnNames.Length;
            int numRows = solution.RowNames.Length;
            var zRow = GetRow(solution.LastTableau, numRows);

            for (int j = 0; j < numVars; j++)
            {
                string varName = solution.ColumnNames[j];
                // Only consider non-basic, original variables
                if (solution.RowNames.Contains(varName) || !varName.StartsWith("x")) continue;

                double currentCoeff = model.ObjectiveCoefficients[j];
                double reducedCost = zRow[j]; // This is (z_j - c_j)

                // For a non-basic variable to remain non-basic, its reduced cost must stay >= 0.
                // A change in its own coefficient, Δ, directly impacts its reduced cost: (z_j - c_j) - Δ >= 0.
                // This means the maximum allowable increase is its reduced cost.
                double allowableIncrease = reducedCost;

                Console.WriteLine($"  - Variable {varName}:");
                Console.WriteLine($"    Current Coefficient: {currentCoeff}");
                Console.WriteLine($"    Allowable Increase:  {allowableIncrease:F3}");
                Console.WriteLine($"    Allowable Decrease:  Infinity");
                Console.WriteLine($"    Range:               (-Infinity, {currentCoeff + allowableIncrease:F3}]");
            }
        }

        /// Calculates and displays the allowable range for the RHS of each constraint.
        /// This determines how much a resource can change while the current basis remains feasible.
        public void ComputeRangesForConstraints()
        {
            Console.WriteLine("Ranges of Feasibility for Constraint RHS Values:");
            int numRows = solution.RowNames.Length;
            int numCols = solution.ColumnNames.Length;
            var rhsColumn = GetColumn(solution.LastTableau, numCols);

            for (int k = 0; k < numRows; k++) // For each original constraint 'k'
            {
                // Find the column in the tableau corresponding to the slack/surplus variable of this constraint.
                // We assume s1 corresponds to constraint 1, s2 to constraint 2, etc.
                string slackVarName = "s" + (k + 1); // This might need adjustment for >= or = constraints
                int slackColIndex = Array.IndexOf(solution.ColumnNames, slackVarName);
                if (slackColIndex == -1)
                {
                    Console.WriteLine($"  - Constraint {k + 1}: Could not find slack variable '{slackVarName}' to analyze.");
                    continue;
                }

                double currentRHS = model.Constraints[k].RightHandSide;
                var b_inv_col = GetColumn(solution.LastTableau, slackColIndex);

                double allowableIncrease = double.PositiveInfinity;
                double allowableDecrease = double.PositiveInfinity;

                // The new RHS must remain non-negative: b' - B_inv_col * Δ >= 0.
                // Δ <= b'_i / b_inv_col_i for b_inv_col_i > 0
                // Δ >= b'_i / b_inv_col_i for b_inv_col_i < 0
                for (int i = 0; i < numRows; i++)
                {
                    if (Math.Abs(b_inv_col[i]) < 1e-9) continue;

                    double ratio = rhsColumn[i] / b_inv_col[i];

                    if (b_inv_col[i] > 0)
                    {
                        allowableDecrease = Math.Min(allowableDecrease, ratio);
                    }
                    else // b_inv_col[i] < 0
                    {
                        allowableIncrease = Math.Min(allowableIncrease, -ratio);
                    }
                }

                Console.WriteLine($"  - Constraint {k + 1}:");
                Console.WriteLine($"    Current RHS:         {currentRHS}");
                Console.WriteLine($"    Allowable Increase:  {(allowableIncrease == double.PositiveInfinity ? "Infinity" : allowableIncrease.ToString("F3"))}");
                Console.WriteLine($"    Allowable Decrease:  {(allowableDecrease == double.PositiveInfinity ? "Infinity" : allowableDecrease.ToString("F3"))}");
                Console.WriteLine($"    Range:               [{currentRHS - allowableDecrease:F3}, {currentRHS + allowableIncrease:F3}]");
            }
        }

        /// Calculates and displays the shadow price for each constraint.
        /// The shadow price is the amount the objective function value will improve
        /// for a one-unit increase in the RHS of that constraint.
        public void ComputeShadowPrices()
        {
            Console.WriteLine("Shadow Prices for Constraints:");
            int numRows = solution.RowNames.Length;
            var zRow = GetRow(solution.LastTableau, numRows);

            for (int k = 0; k < numRows; k++)
            {
                // Find the slack/surplus variable for the k-th constraint.
                // This assumes a simple mapping (s1 for constraint 1, etc.)
                string slackVarName = "s" + (k + 1);
                int slackColIndex = Array.IndexOf(solution.ColumnNames, slackVarName);

                if (slackColIndex != -1)
                {
                    double shadowPrice = zRow[slackColIndex];
                    Console.WriteLine($"  - Constraint {k + 1} (Resource {k + 1}): {shadowPrice:F3}");
                }
                else
                {
                    Console.WriteLine($"  - Constraint {k + 1}: Could not find corresponding slack variable to determine shadow price.");
                }
            }
        }

        #endregion

        #region --- Helper Methods ---

        private double[] GetRow(double[,] matrix, int rowIndex)
        {
            int cols = matrix.GetLength(1);
            double[] row = new double[cols];
            for (int i = 0; i < cols; i++)
            {
                row[i] = matrix[rowIndex, i];
            }
            return row;
        }

        private double[] GetColumn(double[,] matrix, int colIndex)
        {
            int rows = matrix.GetLength(0);
            double[] column = new double[rows];
            for (int i = 0; i < rows; i++)
            {
                column[i] = matrix[i, colIndex];
            }
            return column;
        }

        #endregion
    }
}