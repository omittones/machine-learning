using ConvNetSharp.Core;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using NeuralMotion.Evolution.GeneticSharp;

namespace NeuralMotion.Test
{
    public static class Converters
    {
        public static IChromosome ToChromosome(this INet<double> net)
        {
            var parameters = net.GetParametersAndGradients();
            var geneValues = parameters.SelectMany(p =>
            {
                var reshaped = p.Volume.ReShape(1, 1, -1, 1);
                return reshaped.ToArray();
            }).ToArray();
            var chromosome = new ArrayChromosome(geneValues.Length);
            chromosome.ReplaceGenes(0, geneValues);
            return chromosome;
        }

        public static void FromChromosome(this INet<double> net, IChromosome chromosome)
        {
            var chromo = chromosome as ArrayChromosome;
            var parameters = net.GetParametersAndGradients();
            int gene = 0;
            foreach (var param in parameters)
            {
                var reshaped = param.Volume.ReShape(1, 1, -1, 1);
                for (var i = 0; i < reshaped.Shape.TotalLength; i++)
                {
                    reshaped.Set(0, 0, i, 0, chromo.Values[gene]);
                    gene++;
                }
            }
        }
    }
}
