using Ann.GlobalState;
using Ann.Utilities;

namespace Ann;

public static class SplittingRoutines
{
    public static readonly SplittingRoutine KdSplit = (points, indices, _, numberOfPoints, dimension) =>
    {
        int cuttingDimension = Tools.MaxSpread(points, indices, numberOfPoints, dimension);
        int lowSidePointCount = numberOfPoints / 2;
        AnnCoordinate cuttingValue =
            Tools.MedianSplit(points, indices, numberOfPoints, cuttingDimension, lowSidePointCount);
        return (cuttingDimension, cuttingValue, lowSidePointCount);
    };

    public static readonly SplittingRoutine MidPointSplit = (points, indices, boundingRectangle, numberOfPoints, dimension) =>
    {
        AnnCoordinate maxLength =
            boundingRectangle.UpperBounds[0] - boundingRectangle.LowerBounds[0];
        for (var d = 1; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length > maxLength)
            {
                maxLength = length;
            }
        }

        int cuttingDimension = -1;
        AnnCoordinate maxSpread = -1;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length >= (1 - Constants.DoubleComparisonEpsilon) * maxLength)
            {
                AnnCoordinate spread = Tools.Spread(points, indices, numberOfPoints, dimension);
                if (spread > maxSpread)
                {
                    maxSpread = spread;
                    cuttingDimension = d;
                }
            }
        }

        AnnCoordinate cuttingValue = 0.5d * (boundingRectangle.LowerBounds[cuttingDimension] +
                                             boundingRectangle.UpperBounds[cuttingDimension]);
        var (break1, break2) =
            Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
        int lowSidePointCount;
        if (break1 > numberOfPoints / 2) lowSidePointCount = break1;
        else if (break2 < numberOfPoints / 2) lowSidePointCount = break2;
        else lowSidePointCount = numberOfPoints / 2;

        return (cuttingDimension, cuttingValue, lowSidePointCount);
    };

    public static readonly SplittingRoutine SlidingMidPointSplit = (points, indices, boundingRectangle, numberOfPoints, dimension) =>
    {
        AnnCoordinate maxLength =
            boundingRectangle.UpperBounds[0] - boundingRectangle.LowerBounds[0];
        for (var d = 1; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length > maxLength)
            {
                maxLength = length;
            }
        }

        int cuttingDimension = 0;
        AnnCoordinate maxSpread = -1;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length >= (1 - Constants.DoubleComparisonEpsilon) * maxLength)
            {
                AnnCoordinate spread = Tools.Spread(points, indices, numberOfPoints, d);
                if (spread > maxSpread)
                {
                    maxSpread = spread;
                    cuttingDimension = d;
                }
            }
        }
            
        AnnCoordinate idealCuttingValue = 0.5d * (boundingRectangle.LowerBounds[cuttingDimension] +
                                                  boundingRectangle.UpperBounds[cuttingDimension]);
        var (minimum, maximum) = Tools.MinMax(points, indices, numberOfPoints, cuttingDimension);

        AnnCoordinate cuttingValue;
        if (idealCuttingValue < minimum) cuttingValue = minimum;
        else if (idealCuttingValue > maximum) cuttingValue = maximum;
        else cuttingValue = idealCuttingValue;
            
        var (break1, break2) =
            Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
        int lowSidePointCount;
        if (idealCuttingValue < minimum) lowSidePointCount = 1;
        else if (idealCuttingValue > maximum) lowSidePointCount = numberOfPoints - 1;
        else if (break1 > numberOfPoints / 2) lowSidePointCount = break1;
        else if (break2 < numberOfPoints / 2) lowSidePointCount = break2;
        else lowSidePointCount = numberOfPoints / 2;

        return (cuttingDimension, cuttingValue, lowSidePointCount);
    };

    public static readonly SplittingRoutine FairSplit = (points, indices, boundingRectangle, numberOfPoints, dimension) =>
    {
        AnnCoordinate maxLength =
            boundingRectangle.UpperBounds[0] - boundingRectangle.LowerBounds[0];
        for (var d = 1; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length > maxLength)
            {
                maxLength = length;
            }
        }

        int cuttingDimension = -1;
        AnnCoordinate maxSpread = -1;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length >= (1 - Constants.DoubleComparisonEpsilon) * maxLength)
            {
                AnnCoordinate spread = Tools.Spread(points, indices, numberOfPoints, dimension);
                if (spread > maxSpread)
                {
                    maxSpread = spread;
                    cuttingDimension = d;
                }
            }
        }

        maxLength = 0;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (d != cuttingDimension && length > maxLength)
            {
                maxLength = length;
            }
        }

        AnnCoordinate smallPiece = maxLength / Constants.FairSplitMaxAllowedAspectRatio;
        AnnCoordinate lowestLegalCut = boundingRectangle.LowerBounds[cuttingDimension] + smallPiece;
        AnnCoordinate highestLegalCut = boundingRectangle.UpperBounds[cuttingDimension] - smallPiece;

        AnnCoordinate cuttingValue;
        int lowSidePointCount;
        if (Tools.SplitBalance(points, indices, numberOfPoints, cuttingDimension, lowestLegalCut) >= 0)
        {
            cuttingValue = lowestLegalCut;
            (lowSidePointCount, _) =
                Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
        }
        else if (Tools.SplitBalance(points, indices, numberOfPoints, cuttingDimension, highestLegalCut) <= 0)
        {
            cuttingValue = lowestLegalCut;
            (_, lowSidePointCount) =
                Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
        }
        else
        {
            lowSidePointCount = numberOfPoints / 2;
            cuttingValue = Tools.MedianSplit(points, indices, numberOfPoints, cuttingDimension, lowSidePointCount);
        }

        return (cuttingDimension, cuttingValue, lowSidePointCount);
    };

    public static readonly SplittingRoutine SlidingFairSplit = (points, indices, boundingRectangle, numberOfPoints, dimension) =>
    {
        AnnCoordinate maxLength =
            boundingRectangle.UpperBounds[0] - boundingRectangle.LowerBounds[0];
        for (var d = 1; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length > maxLength)
            {
                maxLength = length;
            }
        }

        int cuttingDimension = -1;
        AnnCoordinate maxSpread = -1;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (length >= (1 - Constants.DoubleComparisonEpsilon) * maxLength)
            {
                AnnCoordinate spread = Tools.Spread(points, indices, numberOfPoints, dimension);
                if (spread > maxSpread)
                {
                    maxSpread = spread;
                    cuttingDimension = d;
                }
            }
        }

        maxLength = 0;
        for (var d = 0; d < dimension; d++)
        {
            AnnCoordinate length = boundingRectangle.UpperBounds[d] - boundingRectangle.LowerBounds[d];
            if (d != cuttingDimension && length > maxLength)
            {
                maxLength = length;
            }
        }

        AnnCoordinate smallPiece = maxLength / Constants.FairSplitMaxAllowedAspectRatio;
        AnnCoordinate lowestLegalCut = boundingRectangle.LowerBounds[cuttingDimension] + smallPiece;
        AnnCoordinate highestLegalCut = boundingRectangle.UpperBounds[cuttingDimension] - smallPiece;
        var (minimum, maximum) = Tools.MinMax(points, indices, numberOfPoints, dimension);

        AnnCoordinate cuttingValue;
        int lowSidePointCount;
        if (Tools.SplitBalance(points, indices, numberOfPoints, cuttingDimension, lowestLegalCut) >= 0)
        {
            if (maximum > lowestLegalCut)
            {
                cuttingValue = lowestLegalCut;
                (lowSidePointCount, _) =
                    Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
            }
            else
            {
                cuttingValue = maximum;
                _ = Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
                lowSidePointCount = numberOfPoints - 1;
            }
        }
        else if (Tools.SplitBalance(points, indices, numberOfPoints, cuttingDimension, highestLegalCut) <= 0)
        {
            if (minimum < highestLegalCut)
            {
                cuttingValue = highestLegalCut;
                (_, lowSidePointCount) =
                    Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
            }
            else
            {
                cuttingValue = minimum;
                _ = Tools.PlaneSplit(points, indices, numberOfPoints, cuttingDimension, cuttingValue);
                lowSidePointCount = 1;
            }
        }
        else
        {
            lowSidePointCount = numberOfPoints / 2;
            cuttingValue = Tools.MedianSplit(points, indices, numberOfPoints, cuttingDimension, lowSidePointCount);
        }

        return (cuttingDimension, cuttingValue, lowSidePointCount);
    };
}