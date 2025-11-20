namespace API.Services
{
    public class RandomValue : IRandomValue
    {
        private readonly Random _random = Random.Shared; 

        public int Next(int maxValue) => _random.Next(maxValue);
        public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    }
}