using System;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Chromosomes;

namespace NeuralMotion.Test
{
    public class SoftmaxLossFitness : IFitness
    {
        private readonly Net<double> net;
        private readonly Volume<double> inputs;
        private readonly Volume<double> outputs;

        public IChromosome ProtoChromosome { get; }

        public SoftmaxLossFitness(
            Net<double> net,
            Volume<double> inputs,
            Volume<double> outputs)
        {
            this.net = net;
            this.inputs = inputs;
            this.outputs = outputs;
            ProtoChromosome = net.ToChromosome();
        }

        public double Evaluate(IChromosome chromosome)
        {
            net.FromChromosome(chromosome);

            var size = inputs.Shape.Dimensions[3];
            var predicted = net.Forward(inputs, false);

            //loss is the class negative log likelihood
            var fitness = 0.0;
            for (var n = 0; n < size; n++)
                for (var d = 0; d < outputs.Depth; d++)
                    for (var h = 0; h < outputs.Height; h++)
                        for (var w = 0; w < outputs.Width; w++)
                        {
                            var expected = outputs.Get(w, h, d, n);
                            var actual = predicted.Get(w, h, d, n);
                            if (actual == 0.0)
                                actual = double.Epsilon;
                            var current = expected * Math.Log(actual);
                            fitness += current;
                        }

            if (Ops<double>.IsInvalid(fitness))
            {
                throw new ArgumentException("Error during calculation!");
            }

            return fitness;
        }
    }
}
