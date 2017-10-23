using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using NeuralMotion.Evolution.GeneticSharp;
using NeuralMotion.Simulator;
using Util;

namespace NeuralMotion.Evolution
{
    public class DetermineFitnessBySimulation : IFitness
    {
        public BallFitness BestBall { get; set; }

        private readonly BoxArena simulator;
        private readonly List<PointF> positions;
        private readonly RandomEx random;

        public DetermineFitnessBySimulation(BoxArena simulator)
        {
            this.simulator = simulator;
            this.positions = new List<PointF>();
            this.random = new RandomEx(1000);
            this.BestBall = new BallFitness(fitness: double.MinValue);
        }

        private PointF Position(int index)
        {
            if (positions.Count <= index)
                positions.Add(new PointF(simulator.RandomPosition(random), simulator.RandomPosition(random)));
            return positions[index];
        }

        public double Evaluate(IChromosome chromo)
        { 
            var chromosome = (ArrayChromosome)chromo;

            return Evaluate(chromosome.Values);
        }

        public double Evaluate(double[] genes)
        {
            this.simulator.RunAsync((index, ball) =>
            {
                ball.LoadBrain(genes);
                ball.Position = Position(index);
            }).Wait();

            this.BestBall = new BallFitness(fitness: double.MinValue);
            var calculated = this
                .simulator
                .EngineBalls
                .Select(ball => new BallFitness { Ball = ball, Fitness = Evaluate(ball) })
                .ToArray();
            foreach (var ball in calculated)
                if (ball.Fitness > BestBall.Fitness)
                    BestBall = ball;

            return calculated.Min(f => f.Fitness);
        }

        public double Evaluate(Ball ball)
        {
            var totalKicks = ball.KicksToBorder + ball.KicksToBall + ball.KicksFromBall + 1;
            return ball.DistanceTravelled / totalKicks / (ball.Energy + 1);
        }
        
        //public double Evaluate(Ball ball)
        //{
        //    return ball.KicksToBall/(ball.KicksToBorder + 0.1);
        //}

        //public double Evaluate(Ball ball)
        //{
        //    var totalKicks = (ball.KicksToBorder + ball.KicksToBall + 0.1);
        //    var distanceMoved = ball.Position.Distance(ball.StartingPosition) + 0.1;
        //    return 1/distanceMoved/(totalKicks*totalKicks);
        //}

        //public double Evaluate(Ball ball)
        //{
        //    return (1/(ball.Position.Length() + 0.00001));
        //}

        //public double Evaluate(Ball ball)
        //{
        //    return 1.0 / (ball.KicksToBorder + ball.KicksToBall + 0.1);
        //}

        //public double Evaluate(Ball ball)
        //{
        //    return 1.0/(Math.Abs(ball.Brain.Sum()) + 0.01);
        //}
    }
}