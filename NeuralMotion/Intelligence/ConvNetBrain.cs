using System.Collections;
using System.Collections.Generic;
using ConvNetSharp.Volume.Single;
using ConvNetSharp.Core.Layers.Single;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using ConvNetSharp.Core.Layers;

namespace NeuralMotion.Intelligence
{
    public class ConvNetBrain : IBrain
    {
        private struct VolumePointer
        {
            internal int X;
            public int H;
            public int W;
            public int D;
            public int N;
            public Net<float> Net;

            public float Get()
            {
                var volume = Net.GetParametersAndGradients()[X].Volume;
                return volume.Get(W, H, D, N);
            }

            public void Set(float value)
            {
                var volume = Net.GetParametersAndGradients()[X].Volume;
                volume.Set(W, H, D, N, value);
            }
        }

        private Volume<float> inputVolume;
        private Net<float> net;
        private List<VolumePointer> map;

        public ConvNetBrain(int inputs, int outputs)
        {
            BuilderInstance.Volume = BuilderInstance.Create();
            this.inputVolume = BuilderInstance.Volume.SameAs(Shape.From(inputs, 1, 1));

            this.net = new Net<float>();
            net.AddLayer(new InputLayer(inputs, 1, 1));
            net.AddLayer(new FullyConnLayer(inputs));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(outputs));
            net.AddLayer(new SoftmaxLayer(outputs));

            this.map = new List<VolumePointer>();
            var volumes = this.net.GetParametersAndGradients();
            for (var x = 0; x < volumes.Count; x++)
            {
                var volume = volumes[x].Volume;
                for (var width = 0; width < volume.Shape.GetDimension(0); width++)
                    for (var height = 0; height < volume.Shape.GetDimension(1); height++)
                        for (var depth = 0; depth < volume.Shape.GetDimension(2); depth++)
                            for (var number = 0; number < volume.Shape.GetDimension(3); number++)
                                map.Add(new VolumePointer
                                {
                                    Net = net,
                                    X = x,
                                    W = height,
                                    H = width,
                                    D = depth,
                                    N = number
                                });
            }
        }

        public float this[int geneIndex]
        {
            get { return this.map[geneIndex].Get(); }
            set { this.map[geneIndex].Set(value); }
        }

        public int NumberOfGenes => this.map.Count;

        public IBrain ExpandGenome(int numberProcessingUnitsToAdd)
        {
            throw new System.NotImplementedException();
        }

        public float[] GetOutput(float[] inputs)
        {
            this.inputVolume.Storage.Set(inputs);

            var output = this.net.Forward(this.inputVolume, false);

            return output.ToArray();
        }

        public IEnumerator<float> GetEnumerator()
        {
            foreach (var m in map)
                yield return m.Get();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
