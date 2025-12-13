namespace Services.Utils
{
    public interface IRandomValue
    {
        int Next(int maxValue);
        int Next(int minValue, int maxValue);
    }
}
