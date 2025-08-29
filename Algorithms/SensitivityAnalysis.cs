using Project_LPR381.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_LPR31.Algorithms
{
    public class SensitivityAnalysis
    {
        private readonly SolutionResult solution;
        private readonly LinearProgrammingModel model;

        public SensitivityAnalysis(SolutionResult solution, LinearProgrammingModel model)
        {
            this.solution = solution;
            this.model = model;
        }

        public void ComputeRangesForVariables() {
            bool exit = false;
            while(!exit)
            {
                Console.Clear();
                Console.WriteLine("Please select an option for computing ranges for variables:");
                Console.WriteLine("1. Compute ranges for all basic variables.");
                Console.WriteLine("2. Compute ranges for all non-basic variables.");
                Console.WriteLine("3. Compute ranges for RHS values.");
                Console.WriteLine("4. Adding new activities and/or constraints.");
                Console.WriteLine("5. Calculate shadow prices.");
                Console.WriteLine("0. Return to the previous ");
                string options = Console.ReadLine();
                
                if(!int.TryParse(options, out int opt) || opt < 0 || opt > 5)
                {
                    Console.WriteLine("Invalid option. Please try again.");
                    continue;
                }
                switch (options)
                {
                    case ("1"):
                        //how the fuck do you do matrices???

                        break;
                    case ("2"):
                        //i think i can do this
                        break;
                    case ("3"):
                        break;
                    case ("4"):
                        break;
                    case ("5"):
                        break;
                    case ("0"):
                        exit = true; //exits loop
                        break;
                    default:
                        Console.WriteLine("Invalid option, please select a whole number from 1 to 5, or 0 to exit. :)");
                        break;

                }
            }

        }

        public void ComputeRangesForConstraints() {
            /* TODO */ 
        }

        public void ComputeShadowPrices() {
            /* TODO */ 
        }

        public void ApplyChangeToCoefficient(int varIndex, double newCoeff) { /* TODO */ }

        public void ApplyChangeToRHS(int constraintIndex, double newRHS) { /* TODO */ }
    }

}
