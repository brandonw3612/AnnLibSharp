using System.IO;
using System.Linq;
using Ann.Enums;
using Ann.GlobalState;
using Ann.Nodes;
using Ann.Primitives;
using Ann.Utilities;

namespace Ann.Trees;

public class BdTree : KdTree
{
    public static IKdNode? RecursiveCreate(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension,
        int bucketSpace,
        OrthogonalRectangle boundingBox,
        SplittingRoutine splittingRoutine,
        ShrinkRule shrinkRule
    )
    {
        if (numberOfPoints <= bucketSpace)
        {
            if (numberOfPoints == 0) return Constants.KdTrivial;
            return new KdLeafNode(numberOfPoints, indices);
        }

        var (decompositionMethod, innerBox) = SelectDecomposition(
            points, indices, numberOfPoints, dimension,
            boundingBox, splittingRoutine, shrinkRule);

        if (decompositionMethod is DecompositionMethod.Split)
        {
            var (cuttingDimension, cuttingValue, lowSidePointCount) = splittingRoutine(
                points, indices, innerBox!, numberOfPoints, dimension);

            AnnCoordinate lowValue = boundingBox.LowerBounds[cuttingDimension];
            AnnCoordinate highValue = boundingBox.UpperBounds[cuttingDimension];
            
            boundingBox.UpperBounds[cuttingDimension] = cuttingValue;
            var leftSubTree = RecursiveCreate(
                points, indices, lowSidePointCount,
                dimension, bucketSpace, boundingBox, splittingRoutine, shrinkRule);
            boundingBox.UpperBounds[cuttingDimension] = highValue;

            boundingBox.LowerBounds[cuttingDimension] = cuttingValue;
            var rightSubTree = RecursiveCreate(
                points, indices.Skip(lowSidePointCount).ToList(), numberOfPoints - lowSidePointCount,
                dimension, bucketSpace, boundingBox, splittingRoutine, shrinkRule);
            boundingBox.LowerBounds[cuttingDimension] = lowValue;

            return new KdSplitNode(cuttingDimension, cuttingValue, lowValue, highValue,
                (leftSubTree!, rightSubTree!));
        }
        else
        {
            int numberOfPointInBox = Tools.BoxSplit(points, indices, numberOfPoints, dimension, innerBox!);
            var leftSubTree = RecursiveCreate(
                points, indices, numberOfPointInBox, dimension, bucketSpace,
                innerBox!, splittingRoutine, shrinkRule);
            var rightSubTree = RecursiveCreate(
                points, indices.Skip(numberOfPointInBox).ToList(), numberOfPoints - numberOfPointInBox,
                dimension, bucketSpace, boundingBox, splittingRoutine, shrinkRule);
            var (numberOfBounds, bounds) = Tools.BoxToBounds(
                innerBox!, boundingBox, dimension);
            return new BdShrinkNode(numberOfBounds, bounds, (leftSubTree!, rightSubTree!));
        }
    }
    
    public BdTree(
        int numberOfPoints,
        int dimension,
        int bucketSize = 1
    ) : base(numberOfPoints, dimension, bucketSize)
    {
        // Nothing further.
    }

    public BdTree(
        AnnPointCollection points,
        int numberOfPoints,
        int dimension,
        int bucketSize = 1,
        SplitRule splitRule = SplitRule.Suggested,
        ShrinkRule shrinkRule = ShrinkRule.Suggested
    ) : this(numberOfPoints, dimension, bucketSize)
    {
        Points = points;
        if (numberOfPoints is 0) return;

        OrthogonalRectangle boundingBox = Tools.EnclosingRectangle(points, PointIndices, numberOfPoints, dimension);

        BoundingBoxLowPoint = AllocatorUtility.CopyPoint(dimension, boundingBox.LowerBounds);
        BoundingBoxHighPoint = AllocatorUtility.CopyPoint(dimension, boundingBox.UpperBounds);

        if (splitRule switch
            {
                SplitRule.Standard => SplittingRoutines.KdSplit,
                SplitRule.Midpoint => SplittingRoutines.MidPointSplit,
                SplitRule.Suggested or SplitRule.SlidingMidpoint => SplittingRoutines.SlidingMidPointSplit,
                SplitRule.Fair => SplittingRoutines.FairSplit,
                SplitRule.SlidingFair => SplittingRoutines.SlidingFairSplit,
                _ => null
            } is { } routine)
        {
            Root = RecursiveCreate(
                points, PointIndices,
                numberOfPoints, dimension, bucketSize,
                boundingBox, routine, shrinkRule);
        }
        else
        {
            ExceptionHandler.Instance.LogError("Illegal splitting method", ErrorLevel.Abort);
        }
    }

    public BdTree(StreamReader streamReader)
    {
        var (root, dimension, numberOfPoints, points, pointIndices,
            bucketSize, lowerBound, upperBound) =
            DumpUtility.ReadDump(streamReader, TreeType.BoxDecompositionTree)!.Value;
        SkeletonTree(numberOfPoints, dimension, bucketSize, points, pointIndices);
        BoundingBoxLowPoint = lowerBound;
        BoundingBoxHighPoint = upperBound;
        Root = root;
    }

    public static (DecompositionMethod Method, OrthogonalRectangle? InnerBox) TrySimpleShrink(
        AnnPointCollection points,
        AnnIndexCollection pointIndices,
        int numberOfPoints,
        int dimension,
        OrthogonalRectangle boundingBox)
    {
        var innerBox = Tools.EnclosingRectangle(points, pointIndices, numberOfPoints, dimension);

        AnnCoordinate maxLength = 0;
        for (var i = 0; i < dimension; i++)
        {
            AnnCoordinate length = innerBox.UpperBounds[i] - innerBox.LowerBounds[i];
            if (length > maxLength)
            {
                maxLength = length;
            }
        }

        int shrinkSideCount = 0;
        for (var i = 0; i < dimension; i++)
        {
            AnnCoordinate gapHigh = boundingBox.UpperBounds[i] - innerBox.UpperBounds[i];
            if (gapHigh < maxLength * Constants.BoxDecompositionGapThreshold)
            {
                innerBox.UpperBounds[i] = boundingBox.UpperBounds[i];
            }
            else
            {
                shrinkSideCount++;
            }

            AnnCoordinate gapLow = innerBox.LowerBounds[i] - boundingBox.LowerBounds[i];
            if (gapLow < maxLength * Constants.BoxDecompositionGapThreshold)
            {
                innerBox.LowerBounds[i] = boundingBox.LowerBounds[i];
            }
            else
            {
                shrinkSideCount++;
            }
        }

        if (shrinkSideCount >= Constants.BoxDecompositionShrinkSideMinimum)
        {
            return (DecompositionMethod.Shrink, innerBox);
        }
        return (DecompositionMethod.Split, null);
    }

    public static (DecompositionMethod Method, OrthogonalRectangle? InnerBox) TryCentroidShrink(
        AnnPointCollection points,
        AnnIndexCollection pointIndices,
        int numberOfPoints,
        int dimension,
        OrthogonalRectangle boundingBox,
        SplittingRoutine splittingRoutine)
    {
        int numberOfSubsetPoints = numberOfPoints;
        int numberOfGoalPoints = (int) (numberOfPoints * Constants.BoxDecompositionFraction);
        int numberOfSplitsNeeded = 0;

        OrthogonalRectangle innerBox = new(dimension);
        AllocatorUtility.AssignRectangle(dimension, boundingBox, innerBox);

        while (numberOfSubsetPoints > numberOfGoalPoints)
        {
            var (cuttingDimension, cuttingValue, lowSidePointCount) = splittingRoutine(
                points, pointIndices, innerBox,
                numberOfSubsetPoints, dimension);
            numberOfSplitsNeeded++;

            if (lowSidePointCount > numberOfSubsetPoints / 2)
            {
                innerBox.UpperBounds[cuttingDimension] = cuttingValue;
                numberOfSubsetPoints = lowSidePointCount;
            }
            else
            {
                innerBox.LowerBounds[cuttingDimension] = cuttingValue;
                pointIndices.RemoveRange(0, lowSidePointCount);
                numberOfSubsetPoints -= lowSidePointCount;
            }
        }

        if (numberOfSplitsNeeded > dimension * Constants.BoxDecompositionAllowedSplitFractionMaximum)
        {
            return (DecompositionMethod.Shrink, innerBox);
        }
        return (DecompositionMethod.Split, null);
    }

    public static (DecompositionMethod Method, OrthogonalRectangle? InnerBox) SelectDecomposition(
        AnnPointCollection points,
        AnnIndexCollection pointIndices,
        int numberOfPoints,
        int dimension,
        OrthogonalRectangle boundingBox,
        SplittingRoutine splittingRoutine,
        ShrinkRule shrinkRule)
    {
        switch (shrinkRule)
        {
            case ShrinkRule.None: return (DecompositionMethod.Split, null);
            case ShrinkRule.Suggested:
            case ShrinkRule.Simple:
                return TrySimpleShrink(points, pointIndices, numberOfPoints, dimension, boundingBox);
            case ShrinkRule.Centroid:
                return TryCentroidShrink(points, pointIndices, numberOfPoints, dimension, boundingBox,
                    splittingRoutine);
            default:
                ExceptionHandler.Instance.LogError("Illegal shrinking rule", ErrorLevel.Abort);
                return (DecompositionMethod.Split, null);
        }
    }
}