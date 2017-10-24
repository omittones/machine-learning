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

            var size = inputs.Shape.GetDimension(3);
            var predicted = net.Forward(inputs, false);

            //loss is the class negative log likelihood
            var loss = 0.0;
            for (var n = 0; n < size; n++)
                for (var d = 0; d < outputs.Shape.GetDimension(2); d++)
                    for (var h = 0; h < outputs.Shape.GetDimension(1); h++)
                        for (var w = 0; w < outputs.Shape.GetDimension(0); w++)
                        {
                            var expected = outputs.Get(w, h, d, n);
                            var actual = predicted.Get(w, h, d, n);
                            if (actual == 0.0)
                                actual = double.Epsilon;
                            var current = expected * Math.Log(actual);
                            loss += current;
                        }

            if (Ops<double>.IsInvalid(loss))
            {
                throw new ArgumentException("Error during calculation!");
            }

            return -loss;
        }
    }
}
