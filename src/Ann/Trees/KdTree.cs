using System;
using System.IO;
using System.Linq;
using Ann.Enums;
using Ann.GlobalState;
using Ann.Nodes;
using Ann.Performance;
using Ann.Primitives;
using Ann.Utilities;

namespace Ann.Trees;

public class KdTree : IPointSet
{
    public int Dimension { get; protected set; }
    public int NumberOfPoints { get; protected set; }
    protected int BucketSize;
    public AnnPointCollection? Points { get; protected set; }
    protected AnnIndexCollection PointIndices;
    protected IKdNode? Root;
    protected AnnPoint? BoundingBoxLowPoint, BoundingBoxHighPoint;

    public static IKdNode RecursiveCreate(
        AnnPointCollection points,
        AnnIndexCollection pointIndices,
        int numberOfPoints,
        int dimension,
        int bucketSpace,
        OrthogonalRectangle boundingBox,
        SplittingRoutine splittingRoutine
    )
    {
        if (numberOfPoints <= bucketSpace)
        {
            if (numberOfPoints == 0) return Constants.KdTrivial!;
            return new KdLeafNode(numberOfPoints, pointIndices);
        }

        var (cuttingDimension, cuttingValue, lowSidePointCount) = 
            splittingRoutine(points, pointIndices, boundingBox, numberOfPoints, dimension);
        AnnCoordinate lowValue = boundingBox.LowerBounds[cuttingDimension];
        AnnCoordinate highValue = boundingBox.UpperBounds[cuttingDimension];

        boundingBox.UpperBounds[cuttingDimension] = cuttingValue;
        var lowSubTree = RecursiveCreate(points, pointIndices,
            lowSidePointCount, dimension, bucketSpace, boundingBox, splittingRoutine);
        boundingBox.UpperBounds[cuttingDimension] = highValue;

        boundingBox.LowerBounds[cuttingDimension] = cuttingValue;
        var highSubTree = RecursiveCreate(points, pointIndices.Skip(lowSidePointCount).ToList(),
            numberOfPoints - lowSidePointCount, dimension, bucketSpace, boundingBox, splittingRoutine);
        boundingBox.LowerBounds[cuttingDimension] = lowValue;

        return new KdSplitNode(cuttingDimension, cuttingValue, lowValue, highValue, (lowSubTree, highSubTree));
    }
    
    protected void SkeletonTree(
        int numberOfPoints,
        int dimension,
        int bucketSize,
        AnnPointCollection? points = null,
        AnnIndexCollection? pointIndices = null
    )
    {
        Dimension = dimension;
        NumberOfPoints = numberOfPoints;
        BucketSize = bucketSize;
        Points = points;
        Root = null;

        if (pointIndices is not {Count: > 0})
        {
            PointIndices = Enumerable.Range(0, numberOfPoints).ToList();
        }
        else
        {
            PointIndices = pointIndices;
        }

        BoundingBoxLowPoint = BoundingBoxHighPoint = null;

        if (Constants.KdTrivial is null)
        {
            Constants.KdTrivial = new KdLeafNode(0, new() {0});
        }
    }

    public KdTree(
        int numberOfPoints = 0,
        int dimension = 0,
        int bucketSize = 1)
    {
        SkeletonTree(numberOfPoints, dimension, bucketSize);
    }

    public KdTree(
        AnnPointCollection points,
        int numberOfPoints,
        int dimension,
        int bucketSize = 1,
        SplitRule splitRule = SplitRule.Suggested
    )
    {
        SkeletonTree(numberOfPoints, dimension, bucketSize);
        Points = points;
        if (numberOfPoints is 0) return;
        OrthogonalRectangle boundingBox =
            Tools.EnclosingRectangle(points, PointIndices!, numberOfPoints, dimension);
        BoundingBoxLowPoint = AllocatorUtility.CopyPoint(dimension, boundingBox.LowerBounds);
        BoundingBoxHighPoint = AllocatorUtility.CopyPoint(dimension, boundingBox.UpperBounds);

        if (splitRule switch
            {
                SplitRule.Standard => SplittingRoutines.KdSplit,
                SplitRule.Midpoint => SplittingRoutines.MidPointSplit,
                SplitRule.Fair => SplittingRoutines.FairSplit,
                SplitRule.Suggested or SplitRule.SlidingMidpoint => SplittingRoutines.SlidingMidPointSplit,
                SplitRule.SlidingFair => SplittingRoutines.SlidingFairSplit,
                _ => null
            } is { } splittingRoutine)
        {
            Root = RecursiveCreate(points, PointIndices!, numberOfPoints,
                dimension, bucketSize, boundingBox, splittingRoutine);
        }
        else
        {
            ExceptionHandler.Instance.LogError("Illegal splitting method", ErrorLevel.Abort);
        }
    }
    
    public KdTree(StreamReader streamReader)
    {
        var (root, dimension, numberOfPoints, points, pointIndices,
            bucketSize, lowerBound, upperBound) = DumpUtility.ReadDump(
            streamReader, TreeType.KdTree)!.Value;
        SkeletonTree(numberOfPoints, dimension, bucketSize, points, pointIndices);
        BoundingBoxLowPoint = lowerBound;
        BoundingBoxHighPoint = upperBound;
        Root = root;
    }

    public void SearchNeighbors(
        AnnPoint queryPoint,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0d
    )
    {
        Query.Search.Dimension = Dimension;
        Query.Search.QueryPoint = queryPoint;
        Query.Search.Points = Points;
        SearchLimit.Visited = 0;
        
        if (numberOfNeighbors > NumberOfPoints)
        {
            ExceptionHandler.Instance.LogError("Requesting more near neighbors than data points", ErrorLevel.Abort);
            return;
        }
        Query.Search.MaxSquaredToleranceError = Math.Pow(1.0 + errorBound, 2);
        Global.LogFloatOp(2);
        Query.Search.PointMinKSet = new(numberOfNeighbors);
        Root!.Search(Tools.BoxDistance(queryPoint, BoundingBoxLowPoint!, BoundingBoxHighPoint!, Dimension));
        for (var i = 0; i < numberOfNeighbors; i++)
        {
            distanceToNearestNeighbors[i] = Query.Search.PointMinKSet.GetAscendingKey(i);
            nearestNeighbors[i] = Query.Search.PointMinKSet.GetAscendingInfo(i);
        }
    }

    public void PrioritySearchNeighbors(
        AnnPoint queryPoint,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0d
    )
    {
        Query.PrioritySearch.MaxSquaredToleranceError = Math.Pow(1.0 + errorBound, 2);
        Global.LogFloatOp(2);
        Query.PrioritySearch.Dimension = Dimension;
        Query.PrioritySearch.QueryPoint = queryPoint;
        Query.PrioritySearch.Points = Points;
        SearchLimit.Visited = 0;
        Query.PrioritySearch.PointMinKSet = new(numberOfNeighbors);

        AnnDistance boxDistance =
            Tools.BoxDistance(queryPoint, BoundingBoxLowPoint!, BoundingBoxHighPoint!, Dimension);

        Query.PrioritySearch.BoxPriorityQueue = new(NumberOfPoints);
        var boxPriorityQueue = Query.PrioritySearch.BoxPriorityQueue;
        boxPriorityQueue.Insert(boxDistance, Root!);

        while (boxPriorityQueue.IsNotEmpty &&
               !(SearchLimit.MaximumVisit != 0 && SearchLimit.Visited > SearchLimit.MaximumVisit))
        {
            (boxDistance, var nextBox) = boxPriorityQueue.ExtractMin();
            Global.LogFloatOp(2);
            if (boxDistance * Query.PrioritySearch.MaxSquaredToleranceError >=
                Query.PrioritySearch.PointMinKSet.MaximumKey) break;
            nextBox.PrioritySearch(boxDistance);
        }

        for (var i = 0; i < numberOfNeighbors; i++)
        {
            distanceToNearestNeighbors[i] = Query.PrioritySearch.PointMinKSet.GetAscendingKey(i);
            nearestNeighbors[i] = Query.PrioritySearch.PointMinKSet.GetAscendingInfo(i);
        }
    }

    public int FixedRadiusSearchNeighbors(
        AnnPoint queryPoint,
        double squaredRadius,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0
    )
    {
        Query.FixedRadiusSearch.Dimension = Dimension;
        Query.FixedRadiusSearch.QueryPoint = queryPoint;
        Query.FixedRadiusSearch.SquaredRadiusSearchBound = squaredRadius;
        Query.FixedRadiusSearch.Points = Points;
        Query.FixedRadiusSearch.NumberOfPointsVisited = 0;
        Query.FixedRadiusSearch.NumberOfPointsInRange = 0;
        Query.FixedRadiusSearch.MaxSquaredToleranceError = Math.Pow(1.0 + errorBound, 2);
        Global.LogFloatOp(2);
        Query.FixedRadiusSearch.ClosestPointSet = new(numberOfNeighbors);

        Root?.FixedRadiusSearch(
            Tools.BoxDistance(queryPoint, BoundingBoxLowPoint!, BoundingBoxHighPoint!, Dimension));

        var closestSet = Query.FixedRadiusSearch.ClosestPointSet;
        for (var i = 0; i < numberOfNeighbors; i++)
        {
            if (distanceToNearestNeighbors is not null)
            {
                distanceToNearestNeighbors[i] = closestSet.GetAscendingKey(i);
            }
            if (nearestNeighbors is not null)
            {
                nearestNeighbors[i] = closestSet.GetAscendingInfo(i);
            }
        }

        return Query.FixedRadiusSearch.NumberOfPointsInRange;
    }

    public void Print(
        bool withPoints,
        IOutputStream outputStream
    )
    {
        outputStream.Output($"ANN Version {Versioning.Version}\n");
        if (withPoints)
        {
            outputStream.Output("    Points:\n");
            for (var i = 0; i < NumberOfPoints; i++)
            {
                outputStream.Output($"\t{i}: ");
                Points![i].Dump(outputStream, Dimension);
                outputStream.Output('\n');
            }
        }
        if (Root is null)
        {
            outputStream.Output("    Null tree.\n");
        }
        else
        {
            Root.Print(0, outputStream);
        }
    }
    
    public void Dump(
        bool withPoints,
        IOutputStream outputStream
    )
    {
        outputStream.Output($"#ANN {Versioning.Version}\n");

        if (withPoints)
        {
            outputStream.Output($"points {Dimension} {NumberOfPoints}\n");
            for (var i = 0; i < NumberOfPoints; i++)
            {
                outputStream.Output($"{i} ");
                Points![i].Dump(outputStream, Dimension);
                outputStream.Output('\n');
            }
        }

        outputStream.Output($"tree {Dimension} {NumberOfPoints} {BucketSize}\n");
        BoundingBoxLowPoint!.Dump(outputStream, Dimension);
        outputStream.Output('\n');
        BoundingBoxHighPoint!.Dump(outputStream, Dimension);
        outputStream.Output('\n');

        if (Root is null)
        {
            outputStream.Output("null\n");
        }
        else
        {
            Root.Dump(outputStream);
        }
    }

    public void GetStatistics(KdStatistics statistics)
    {
        statistics.Reset(Dimension, NumberOfPoints, BucketSize);
        OrthogonalRectangle boundingBox = new(Dimension, BoundingBoxLowPoint!, BoundingBoxHighPoint!);
        if (Root is not null)
        {
            Root.GetStatistics(statistics, Dimension, boundingBox);
            statistics.AspectRatioAverage = statistics.AspectRatioSum / statistics.NumberOfLeaves;
        }
    }
}