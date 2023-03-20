using System.Text;

namespace Ann.Performance;

public static class Global
{
    public static int NumberOfDataPoints;
    public static int NumberOfVisitedLeafNodes;
    public static int NumberOfVisitedSplittingNodes;
    public static int NumberOfVisitedShrinkingNodes;
    public static int NumberOfVisitedPointsInQuery;
    public static int CoordinateHitsInQuery;
    public static int FloatingOpsInQuery;

    public static SampleStatistics LeafNodeVisitStatistics = new();
    public static SampleStatistics SplittingNodeVisitStatistics = new();
    public static SampleStatistics ShrinkingNodeVisitStatistics = new();
    public static SampleStatistics TotalNodeVisitStatistics = new();
    public static SampleStatistics PointVisitStatistics = new();
    public static SampleStatistics CoordinateHitStatistics = new();
    public static SampleStatistics FloatingOpStatistics = new();

    public static SampleStatistics AverageErrorStatistics = new();
    public static SampleStatistics RankErrorStatistics = new();

    public static void ResetStatistics(int dataSize)
    {
        NumberOfDataPoints = dataSize;
        LeafNodeVisitStatistics.Reset();
        SplittingNodeVisitStatistics.Reset();
        ShrinkingNodeVisitStatistics.Reset();
        TotalNodeVisitStatistics.Reset();
        PointVisitStatistics.Reset();
        CoordinateHitStatistics.Reset();
        FloatingOpStatistics.Reset();
        AverageErrorStatistics.Reset();
        RankErrorStatistics.Reset();
    }

    public static void ResetCounts()
    {
        NumberOfVisitedLeafNodes = 0;
        NumberOfVisitedSplittingNodes = 0;
        NumberOfVisitedShrinkingNodes = 0;
        NumberOfVisitedPointsInQuery = 0;
        CoordinateHitsInQuery = 0;
        FloatingOpsInQuery = 0;
    }

    public static void UpdateStatistics()
    {
        LeafNodeVisitStatistics += NumberOfVisitedLeafNodes;
        TotalNodeVisitStatistics += NumberOfVisitedSplittingNodes + NumberOfVisitedLeafNodes;
        SplittingNodeVisitStatistics += NumberOfVisitedSplittingNodes;
        ShrinkingNodeVisitStatistics += NumberOfVisitedShrinkingNodes;
        PointVisitStatistics += NumberOfVisitedPointsInQuery;
        CoordinateHitStatistics += CoordinateHitsInQuery;
        FloatingOpStatistics += FloatingOpsInQuery;
    }

    public static string PrintStatistics(bool validate)
    {
        var builder = new StringBuilder()
            .Append("Performance statistics:\n")
            .Append("[ Mean : StandardDeviation ]< Minimum , Maximum >\n")
            .Append(LeafNodeVisitStatistics.ToString("Leaf nodes", 1))
            .Append(SplittingNodeVisitStatistics.ToString("Splitting nodes", 1))
            .Append(ShrinkingNodeVisitStatistics.ToString("Shrinking nodes", 1))
            .Append(TotalNodeVisitStatistics.ToString("Total nodes", 1))
            .Append(PointVisitStatistics.ToString("Points visited", 1))
            .Append(CoordinateHitStatistics.ToString("Coordinate hits / point", NumberOfDataPoints))
            .Append(FloatingOpStatistics.ToString("Floating ops(K)", 1000));
        if (validate)
        {
            builder
                .Append(AverageErrorStatistics.ToString("Average error", 1))
                .Append(RankErrorStatistics.ToString("Rank error", 1));
        }
        builder.Append('\n');
        return builder.ToString();
    }

    public static void LogFloatOp(int n)
    {
#if ANN_PERF
        FloatingOpsInQuery += n;
#endif
    }

    public static void LogLeafNodeVisit(int n)
    {
#if ANN_PERF
        NumberOfVisitedLeafNodes += n;
#endif
    }

    public static void LogSplitNodeVisit(int n)
    {
#if ANN_PERF
        NumberOfVisitedSplittingNodes += n;
#endif
    }

    public static void LogShrinkingNodeVisit(int n)
    {
#if ANN_PERF
        NumberOfVisitedShrinkingNodes += n;
#endif
    }

    public static void LogPointVisit(int n)
    {
#if ANN_PERF
        NumberOfVisitedPointsInQuery += n;
#endif
    }

    public static void LogCoordinateHit(int n)
    {
#if ANN_PERF
        CoordinateHitsInQuery += n;
#endif
    }
}