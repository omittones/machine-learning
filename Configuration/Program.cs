using System;
using NeuralMotion.Evolution.Custom;
using NeuralMotion.Evolution.GeneticSharp;
using NeuralMotion.Evolution.GeneticSharp.Config;
using NeuralMotion.Evolution.GeneticSharp.Validations;

namespace Configuration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var annealer = new ShrinkingRadiusSearch(
                ConfigEfficiencyTester.TestPopulationSize,
                new ArrayChromosome(ConfigEfficiencyTester.TestChromosomeSize),
                new AllZerosFitness());
            while (annealer.CurrentGeneration < 100)
            {
                annealer.PerformSingleStep();
                Console.WriteLine($"{annealer.BestFitness}");
            }

            return;

            var config = new RandomConfig();
            var validatingConfig = new ValidatedConfig(config);

            var adam = new ConfigChromosome(
                validatingConfig,
                true,
                ConfigEfficiencyTester.TestPopulationSize,
                ConfigEfficiencyTester.TestChromosomeSize,
                ConfigEfficiencyTester.TestFitnessFunction);

            var configurator = new Evolver(adam, 100, new ConfigEfficiencyTester());

            while (true)
            {
                configurator.PerformSingleStep();
                Console.WriteLine($"{configurator.BestFitness}");

                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                    break;
            }

            Console.WriteLine(configurator.CurrentChampGenome);
        }
    }
}
