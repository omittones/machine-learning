//import logging;
//logger = logging.getLogger(__name__);
//import numpy as np;
//from gym import error;
//from gym.utils import closer;
//env_closer = closer.Closer();
////# Env-related abstractions


namespace gym
{
    internal class BoxSpace : Space<(double x, double y)>
    {
        private (double x, double y) high;
        private (double x, double y) low;

        public BoxSpace((double x, double y) high, (double x, double y) low)
        {
            this.high = high;
            this.low = low;
        }

        public override bool contains((double x, double y) item)
        {
            return
                high.x > item.x && low.x < item.x &&
                high.y > item.y && low.y < item.y;
        }

        public override (double x, double y) sample()
        {
            return (
                x: high.x - (high.x - low.x) * rnd.NextDouble(),
                y: high.y - (high.y - low.y) * rnd.NextDouble());
        }
    }
}