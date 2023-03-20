using System;

namespace Ann.Performance;

public class KdStatistics
{
    public int Dimension { get; set; }
    public int NumberOfPoints { get; set; }
    public int BucketSize { get; set; }
    public int NumberOfLeaves { get; set; }
    public int NumberOfTrivialLeaves { get; set; }
    public int NumberOfSplittingNodes { get; set; }
    public int NumberOfShrinkingNodes { get; set; }
    public int Depth { get; set; }
    public float AspectRatioSum { get; set; }
    public float AspectRatioAverage { get; set; }

    public void Reset(
        int dimension = 0,
        int numberOfPoints = 0,
        int bucketSize = 0
    )
    {
        Dimension = dimension;
        NumberOfPoints = numberOfPoints;
        BucketSize = bucketSize;

        NumberOfLeaves =
            NumberOfTrivialLeaves =
                NumberOfSplittingNodes =
                    NumberOfShrinkingNodes =
                        Depth =
                            0;
        AspectRatioSum =
            AspectRatioAverage =
                0f;
    }

    public KdStatistics()
    {
        Dimension =
            NumberOfPoints =
                BucketSize =
                    NumberOfLeaves =
                        NumberOfTrivialLeaves =
                            NumberOfSplittingNodes =
                                NumberOfShrinkingNodes =
                                    Depth =
                                        0;
        AspectRatioSum =
            AspectRatioAverage =
                0f;
    }

    public void Merge(KdStatistics another)
    {
        NumberOfLeaves += another.NumberOfLeaves;
        NumberOfTrivialLeaves += another.NumberOfTrivialLeaves;
        NumberOfSplittingNodes += another.NumberOfSplittingNodes;
        NumberOfShrinkingNodes += another.NumberOfShrinkingNodes;
        Depth = Math.Max(Depth, another.Depth);
        AspectRatioSum += another.AspectRatioSum;
    }
}