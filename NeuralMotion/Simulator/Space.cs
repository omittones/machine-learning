//import logging;
//logger = logging.getLogger(__name__);
//import numpy as np;
//from gym import error;
//from gym.utils import closer;
//env_closer = closer.Closer();
////# Env-related abstractions

using System;
using System.Linq;

namespace gym
{
    public class Space<T> : Space
    {
        public virtual T sample()
        {
            throw new NotImplementedException();
        }

        public virtual bool contains(T item)
        {
            throw new NotImplementedException();
        }
    }

    public class Space
    {
        protected Random rnd = new Random();

        public static Space<int> Discrete(int states)
        {
            return new DiscreteSpace(Enumerable.Range(0, states).ToArray());
        }

        public static Space<(double x, double y)> Box((double x, double y) high, (double x, double y) low)
        {
            return new BoxSpace(high, low);
        }
    }
}