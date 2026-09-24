using System.Collections.Generic;
using Game2048.Core;

namespace Game2048.Tests
{
    /// <summary>Deterministic random: always picks the first empty cell and a fixed spawn roll.</summary>
    internal sealed class TestRandom : IRandom
    {
        private readonly Queue<double> _rolls;
        private readonly double _defaultRoll;

        public int NextCalls { get; private set; }

        /// <param name="defaultRoll">0.0 spawns a 2, 0.95 spawns a 4.</param>
        public TestRandom(double defaultRoll = 0.0, params double[] rolls)
        {
            _defaultRoll = defaultRoll;
            _rolls = new Queue<double>(rolls);
        }

        public int Next(int maxExclusive)
        {
            NextCalls++;
            return 0;
        }

        public double NextDouble() => _rolls.Count > 0 ? _rolls.Dequeue() : _defaultRoll;
    }
}
