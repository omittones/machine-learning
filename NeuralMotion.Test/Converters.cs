using ConvNetSharp.Core;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using NeuralMotion.Evolution.GeneticSharp;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.Double;

namespace NeuralMotion.Test
{
    public static class Converters
    {
        public static Volume<double> ToSoftmaxVolume(this int[] classIndexes)
        {
            var nmClasses = classIndexes.Max() + 1;
            var outputs = BuilderInstance.Volume.SameAs(Shape.From(1, 1, nmClasses, classIndexes.Length));
            outputs.Clear();
            for (var i = 0; i < classIndexes.Length; i++)
                outputs.Set(0, 0, classIndexes[i], i, 1);
            return outputs;
        }

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
