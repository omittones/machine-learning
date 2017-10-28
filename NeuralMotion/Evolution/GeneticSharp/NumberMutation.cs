using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Mutations;
using Util;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Univariate;

namespace NeuralMotion.Evolution.GeneticSharp
{
    public class NumberMutation : IMutation
    {
        private readonly double intensity;
        private readonly NormalDistribution gaussian;

        public bool IsOrdered => true;

        public NumberMutation(double intensity = 1.0f)
        {
            this.intensity = intensity;
            this.gaussian = new NormalDistribution(0, 3.0f);
        }

        public void Mutate(IChromosome chromosome, float geneMutationProbability)
        {
            var chromo = (ArrayChromosome)chromosome;
            var newValues = chromo.Values
                .Select(value =>
                {
                    if (RandomEx.Global.NextDouble() < geneMutationProbability)
                    {
                        var deviation = gaussian.Generate() * intensity;
                        value = value + deviation;
                    }
                    return value;
                }).ToArray();
            chromo.ReplaceGenes(0, newValues);
        }
    }
}