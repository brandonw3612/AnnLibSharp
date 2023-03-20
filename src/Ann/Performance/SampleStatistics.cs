using System;
using System.Text;
using Ann.GlobalState;

namespace Ann.Performance;

public class SampleStatistics
{
    public int NumberOfSamples { get; private set; }
    private double _sum;
    private double _squaredSum;
    public double Minimum { get; private set; }
    public double Maximum { get; private set; }

    public void Reset()
    {
        NumberOfSamples = 0;
        _sum = _squaredSum = 0;
        Minimum = Constants.MaxDouble;
        Maximum = Constants.MaxDouble * -1d;
    }

    private SampleStatistics(
        int numberOfSamples,
        double sum,
        double squaredSum,
        double minimum,
        double maximum
    )
    {
        NumberOfSamples = numberOfSamples;
        _sum = sum;
        _squaredSum = squaredSum;
        Minimum = minimum;
        Maximum = maximum;
    }

    public SampleStatistics()
        : this(
            0,
            0d,
            0d,
            Constants.MaxDouble,
            Constants.MaxDouble * -1d
        )
    {
        // Nothing further.
    }

    public static SampleStatistics operator + (SampleStatistics s, double x)
    {
        return new(
            s.NumberOfSamples + 1,
            s._sum + x,
            s._squaredSum + x * x,
            Math.Min(s.Minimum, x),
            Math.Max(s.Maximum, x)
        );
    }

    public double Mean => _sum / NumberOfSamples;
    public double StandardDeviation => Math.Sqrt((_squaredSum - _sum * _sum / NumberOfSamples) / (NumberOfSamples - 1));

    public string ToString(
        string title,
        double divisor
    ) =>
        new StringBuilder()
            .Append(title).Append(" = [ ")
            .Append($"{Mean / divisor,9}").Append(" : ")
            .Append($"{StandardDeviation / divisor,9}").Append(" ]<")
            .Append($"{Minimum / divisor,9}").Append(" , ")
            .Append($"{Maximum,9}").Append(".\n")
            .ToString();
}