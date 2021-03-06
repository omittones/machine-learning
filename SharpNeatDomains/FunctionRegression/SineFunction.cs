﻿/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2016 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */
// Disable comment warnings for trivial class.
#pragma warning disable 1591

using System;

namespace SharpNeat.Domains.FunctionRegression
{
    public class SineFunction : IFunction
    {
        public double GetValue(double[] args)
        {
            return (Math.Sin(args[0])*0.4)+0.5;
        }

        public int InputCount
        {
            get { return 1; }
        }
    }
}
