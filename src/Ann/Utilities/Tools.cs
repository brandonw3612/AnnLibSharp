using System;
using System.Linq;
using Ann.Primitives;

namespace Ann.Utilities;

public static class Tools
{
    public static AnnDistance Distance(
        int dimension,
        AnnPoint p,
        AnnPoint q
    )
    {
        AnnDistance distance = 0;
        for (var i = 0; i < dimension; i++)
        {
            distance += Math.Pow(p[i] - q[i], 2);
        }
        Performance.Global.LogFloatOp(3 * dimension);
        Performance.Global.LogPointVisit(1);
        Performance.Global.LogCoordinateHit(dimension);
        return distance;
    }

    public static AnnDistance SquaredDistance(
        AnnPoint p,
        AnnPoint q
    )
    {
        return p.Select((t, i) => Math.Pow(t - q[i], 2d)).Sum();
    }

    public static double AspectRatio(
        int dimension,
        OrthogonalRectangle boundingCube
    )
    {
        var length = boundingCube.UpperBounds[0] - boundingCube.LowerBounds[0];
        var minLength = length;
        var maxLength = length;
        for (var i = 0; i < dimension; i++)
        {
            length = boundingCube.UpperBounds[i] - boundingCube.LowerBounds[i];
            minLength = Math.Min(minLength, length);
            maxLength = Math.Max(maxLength, length);
        }
        return maxLength / minLength;
    }

    public static OrthogonalRectangle EnclosingRectangle(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension
    )
    {
        OrthogonalRectangle result = new(dimension);
        for (var i = 0; i < dimension; i++)
        {
            var lowerBound = points[indices[0]][i];
            var higherBound = points[indices[0]][i];
            for (var j = 0; j < numberOfPoints; j++)
            {
                lowerBound = Math.Min(lowerBound, points[indices[j]][i]);
                higherBound = Math.Max(higherBound, points[indices[j]][i]);
            }
            result.LowerBounds[i] = lowerBound;
            result.UpperBounds[i] = higherBound;
        }
        return result;
    }

    public static OrthogonalRectangle EnclosingCube(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension
    )
    {
        var rectangle = EnclosingRectangle(points, indices, numberOfPoints, dimension);

        AnnCoordinate maxLength = 0;
        for (var i = 0; i < dimension; i++)
        {
            var length = rectangle.UpperBounds[i] - rectangle.LowerBounds[i];
            maxLength = Math.Max(length, maxLength);
        }

        for (var i = 0; i < dimension; i++)
        {
            var length = rectangle.UpperBounds[i] - rectangle.LowerBounds[i];
            var halfDifference = (maxLength - length) * 0.5d;
            rectangle.LowerBounds[i] -= halfDifference;
            rectangle.UpperBounds[i] += halfDifference;
        }

        return rectangle;
    }

    public static AnnDistance BoxDistance(
        AnnPoint point,
        AnnPoint lowPoint,
        AnnPoint highPoint,
        int dimension
    )
    {
        var distance = 0d;
        for (var i = 0; i < dimension; i++)
        {
            if (point[i] < lowPoint[i])
            {
                distance += Math.Pow(lowPoint[i] - point[i], 2d);
            }
            else if (point[i] > highPoint[i])
            {
                distance += Math.Pow(point[i] - highPoint[i], 2d);
            }
        }
        return distance;
    }

    public static AnnCoordinate Spread(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension
    )
    {
        var minCoordinate = points[indices[0]][dimension];
        var maxCoordinate = points[indices[0]][dimension];
        for (var i = 1; i < numberOfPoints; i++)
        {
            var c = points[indices[i]][dimension];
            minCoordinate = Math.Min(minCoordinate, c);
            maxCoordinate = Math.Max(maxCoordinate, c);
        }
        return maxCoordinate - minCoordinate;
    }

    public static (AnnCoordinate Min, AnnCoordinate Max) MinMax(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension
    )
    {
        var minCoordinate = points[indices[0]][dimension];
        var maxCoordinate = points[indices[0]][dimension];
        for (var i = 1; i < numberOfPoints; i++)
        {
            var c = points[indices[i]][dimension];
            minCoordinate = Math.Min(minCoordinate, c);
            maxCoordinate = Math.Max(maxCoordinate, c);
        }
        return (maxCoordinate, minCoordinate);
    }
    
    public static int MaxSpread(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension
    )
    {
        var maxDimension = 0;
        AnnCoordinate maxSpread = 0;
        if (numberOfPoints is 0) return maxDimension;
        for (var i = 0; i < dimension; i++)
        {
            var spread = Spread(points, indices, numberOfPoints, i);
            if (spread > maxSpread)
            {
                maxSpread = spread;
                maxDimension = i;
            }
        }
        return maxDimension;
    }

    public static AnnCoordinate MedianSplit(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension,
        int lowerPartCount
    )
    {
        int left = 0, right = numberOfPoints - 1;
        while (left < right)
        {
            var i = (left + right) / 2;

            if (points[indices[i]][dimension] > points[indices[right]][dimension])
            {
                (indices[i], indices[right]) = (indices[right], indices[i]);
            }
            (indices[left], indices[i]) = (indices[i], indices[left]);

            var coordinate = points[indices[left]][dimension];
            i = left;
            var k = right;
            while (true)
            {
                while (points[indices[++i]][dimension] < coordinate)
                {
                }

                while (points[indices[--k]][dimension] > coordinate)
                {
                }

                if (i < k)
                {
                    (indices[i], indices[k]) = (indices[k], indices[i]);
                }
                else break;
            }
            (indices[left], indices[k]) = (indices[k], indices[left]);

            if (k > lowerPartCount) right = k - 1;
            else if (k < lowerPartCount) left = k + 1;
            else break;
        }

        if (lowerPartCount > 0)
        {
            var coordinate = points[indices[0]][dimension];
            var k = 0;
            for (var i = 1; i < lowerPartCount; i++)
            {
                if (points[indices[i]][dimension] > coordinate)
                {
                    coordinate = points[indices[i]][dimension];
                    k = i;
                }
            }
            (indices[lowerPartCount - 1], indices[k]) = (indices[k], indices[lowerPartCount - 1]);
        }

        return (points[indices[lowerPartCount - 1]][dimension] + points[indices[lowerPartCount]][dimension]) * 0.5d;
    }

    public static (int FirstBreak, int SecondBreak) PlaneSplit(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension,
        AnnCoordinate cuttingValue)
    {
        int left = 0, right = numberOfPoints - 1;
        while (true)
        {
            while (left < indices.Count && points[indices[left]][dimension] < cuttingValue) left++;
            while (right >= 0 && points[indices[right]][dimension] >= cuttingValue) right--;
            if (left > right) break;
            (indices[left], indices[right]) = (indices[right], indices[left]);
            left++;
            right--;
        }
        var firstBreak = left;
        right = numberOfPoints - 1;
        while (true)
        {
            while (left < indices.Count && points[indices[left]][dimension] <= cuttingValue) left++;
            while (right >= 0 && points[indices[right]][dimension] > cuttingValue) right--;
            if (left > right) break;
            (indices[left], indices[right]) = (indices[right], indices[left]);
            left++;
            right--;
        } 
        var secondBreak = left;
        return (firstBreak, secondBreak);
    }

    public static int BoxSplit(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension,
        OrthogonalRectangle box
    )
    {
        int left = 0, right = numberOfPoints - 1;
        while (true)
        {
            while (left < indices.Count && box.Inside(dimension, points[indices[left]])) left++;
            while (right >= 0 && !box.Inside(dimension, points[indices[right]])) right--;
            if (left > right) break;
            (indices[left], indices[right]) = (indices[right], indices[left]);
            left++;
            right--;
        }
        return left;
    }

    public static int SplitBalance(
        AnnPointCollection points,
        AnnIndexCollection indices,
        int numberOfPoints,
        int dimension,
        AnnCoordinate cuttingValue
    )
    {
        
        var lowerPartCount = 0;
        for (var i = 0; i < numberOfPoints; i++)
        {
            if (points[indices[i]][dimension] < cuttingValue)
            {
                lowerPartCount++;
            }
        }
        return lowerPartCount - numberOfPoints / 2;
    }

    public static (int NumberOfBounds, AnnOrthogonalHalfSpaceCollection Bounds) BoxToBounds(
        OrthogonalRectangle innerBox,
        OrthogonalRectangle enclosingBox,
        int dimension
    )
    {
        AnnOrthogonalHalfSpaceCollection result = new();
        for (var i = 0; i < dimension; i++)
        {
            if (innerBox.LowerBounds[i] > enclosingBox.LowerBounds[i])
            {
                result.Add(new(i, innerBox.LowerBounds[i], 1));
            }
            if (innerBox.UpperBounds[i] < enclosingBox.UpperBounds[i])
            {
                result.Add(new(i, innerBox.UpperBounds[i], -1));
            }
        }
        return (result.Count, result);
    }

    public static OrthogonalRectangle BoundsToBox(
        OrthogonalRectangle enclosingBox,
        int dimension,
        int numberOfBounds,
        AnnOrthogonalHalfSpaceCollection bounds
    )
    {
        OrthogonalRectangle innerBox = new(dimension); 
        AllocatorUtility.AssignRectangle(dimension, enclosingBox, innerBox);
        for (var i = 0; i < numberOfBounds; i++)
        {
            bounds[i].Project(innerBox.LowerBounds);
            bounds[i].Project(innerBox.UpperBounds);
        }
        return innerBox;
    }
}