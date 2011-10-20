namespace RandomAccessPerlinNoise
{
    public interface IInterpolator
    {
        double Interpolate(double a, double b, double t);
    }
}
