using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using NeuralMotion.Evolution.GeneticSharp;
using NeuralMotion.Evolution.GeneticSharp.Validations;

namespace Configuration
{
    public class ConfigEfficiencyTester : IFitness
    {
        public const int TestPopulationSize = 100;
        public const int TestChromosomeSize = 50;
        public static readonly IFitness TestFitnessFunction = new AllZerosFitness();

        public double Evaluate(IChromosome chromosome)
        {
            var config = (ConfigChromosome) chromosome;

            var adam = new ArrayChromosome(TestChromosomeSize);

            var evolver = new Evolver(adam, TestPopulationSize, TestFitnessFunction,
                config.Selection, config.Crossover,
                config.Mutation, config.Reinsertion);
            evolver.CrossoverProbability = (float) config.CrossoverFactor;
            evolver.MutationProbability = (float) config.MutationFactor;

            try
            {
                for (var i = 0; i < 100; i++)
                    evolver.PerformSingleStep();
                return evolver.BestFitness;
            }
            catch (ValidationException ex)
            {
                Console.WriteLine(ex.Message);
                return double.MinValue;
            }
        }
    }
}
