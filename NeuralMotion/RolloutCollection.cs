using System.Collections.Generic;
using System.Linq;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using System;

namespace NeuralMotion
{
    public class RolloutCollection
    {
        private readonly int nmPaths;
        private readonly int nmActions;
        private Volume<double> inputs;
        private List<Path> paths;

        public RolloutCollection(int nmPaths, int nmActions)
        {
            this.nmPaths = nmPaths;
            this.nmActions = nmActions;
            this.paths = new List<Path>();
        }

        public bool Full => paths.Count == nmPaths && paths.All(p => p.Actions.Count == nmActions);

        public void AppendToPath(long actorId, double[] inputs, int action, Func<double> pathReturnGetter)
        {
            if (this.inputs == null)
            {
                this.inputs = BuilderInstance<double>.Volume.SameAs(1, 1, inputs.Length, nmPaths * nmActions);
            }

            var lastPath = paths.FindLast(p => p.Actor == actorId);
            if (lastPath == null || lastPath.Actions.Count == nmActions)
            {
                if (paths.Count == nmPaths)
                    return;

                lastPath = new Path { Actor = actorId, Actions = new List<int>(), BatchStart = paths.Count * nmActions };
                paths.Add(lastPath);
            }

            var batch = lastPath.BatchStart + lastPath.Actions.Count;
            lastPath.Actions.Add(action);
                        
            for (var d = 0; d < inputs.Length; d++)
                this.inputs.Set(0, 0, d, batch, inputs[d]);

            if (lastPath.Actions.Count == nmActions)
                lastPath.Return = pathReturnGetter();
        }

        public void Apply(ReinforcementTrainer trainer)
        {
            var pathActions = paths
                .Select(p => p.Actions.ToArray())
                .ToArray();
            var pathReturns = paths
                .Select(p => p.Return)
                .ToArray();

            trainer.Reinforce(inputs, pathActions, pathReturns);

            paths.Clear();
        }
    }
}