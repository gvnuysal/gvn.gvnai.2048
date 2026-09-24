namespace Game2048.Core
{
    /// <summary>Random source abstraction so spawns can be made deterministic in tests.</summary>
    public interface IRandom
    {
        /// <summary>Returns an integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);

        /// <summary>Returns a double in [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random _random;

        public SystemRandom() : this(new System.Random()) { }

        public SystemRandom(int seed) : this(new System.Random(seed)) { }

        private SystemRandom(System.Random random)
        {
            _random = random;
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);

        public double NextDouble() => _random.NextDouble();
    }
}
