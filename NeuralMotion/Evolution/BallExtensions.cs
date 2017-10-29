using System.Diagnostics;
using GeneticSharp.Domain.Chromosomes;
using NeuralMotion.Intelligence;
using NeuralMotion.Simulator;
using System;
using System.Linq;

namespace NeuralMotion.Evolution
{
    public static class BallExtensions
    {
        public static string Hash(this IBrain brain)
        {
            var genome = brain
                      .Select(g =>
                      {
                          g = (50 + g * 100);
                          if (g < 0) g = 0;
                          if (g > byte.MaxValue) g = byte.MaxValue;
                          return (byte)g;
                      })
                      .Where(g => g != 0)
                      .ToArray();

            var hash = Convert.ToBase64String(genome);
            return hash;
        }

        public static void LoadBrain(this IBrain brain, double[] values)
        {
            Debug.Assert(brain.NumberOfGenes == values.Length);
            for (var gene = 0; gene < values.Length; gene++)
                brain[gene] = (float) (values[gene]);
        }

        public static void LoadBrain(this IBrain brain, IChromosome values)
        {
            Debug.Assert(brain.NumberOfGenes == values.Length);
            for (var gene = 0; gene < values.Length; gene++)
                brain[gene] = (float) ((double)values.GetGene(gene).Value);
        }
    }
}