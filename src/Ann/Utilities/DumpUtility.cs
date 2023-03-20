using System.IO;
using System.Linq;
using Ann.Enums;
using Ann.GlobalState;
using Ann.Nodes;
using Ann.Primitives;

namespace Ann.Utilities;

public static class DumpUtility
{
    public static (IKdNode? Root, int Dimension, int NumberOfPoints,
        AnnPointCollection Points, AnnIndexCollection PointIndices,
        int BucketSize, AnnPoint LowerBound, AnnPoint UpperBound)?
        ReadDump(StreamReader streamReader, TreeType treeType)
    {
        if (streamReader.ReadLine() is not { } versionLine
            || !versionLine.Trim().StartsWith("#ANN"))
        {
            ExceptionHandler.Instance.LogError("Incorrect header for dump file", ErrorLevel.Abort);
            return null;
        }
        if (streamReader.ReadLine() is not { } pointSectionHeaderLine 
            || !pointSectionHeaderLine.Trim().ToLower().StartsWith("points"))
        {
            ExceptionHandler.Instance.LogError("Points must be supplied in the dump file", ErrorLevel.Abort);
            return null;
        }
        var pointHeaderSegments = pointSectionHeaderLine.Trim().Split(' ').ToArray();
        if (pointHeaderSegments.Length != 3
            || !int.TryParse(pointHeaderSegments[1], out var dimension)
            || !int.TryParse(pointHeaderSegments[2], out var numberOfPoints))
        {
            ExceptionHandler.Instance.LogError($"Invalid section header/: \n{pointSectionHeaderLine}.", ErrorLevel.Abort);
            return null;
        }

        AnnPointCollection points = AllocatorUtility.CreatePoints(numberOfPoints, dimension);
        for (var i = 0; i < numberOfPoints; i++)
        {
            if (streamReader.ReadLine() is not { } pointLine
                || pointLine.Trim().Split(' ') is not { } pointLineSegments
                || pointLineSegments.Length != 1 + dimension)
            {
                ExceptionHandler.Instance.LogError("An error occurred while reading point info.", ErrorLevel.Abort);
                return null;
            }
            if (!int.TryParse(pointLineSegments[0], out var lineIndex)
                || lineIndex < 0 || lineIndex >= numberOfPoints)
            {
                ExceptionHandler.Instance.LogError("Point index is out of range", ErrorLevel.Abort);
                return null;
            }
            for (var j = 0; j < dimension; j++)
            {
                if (!double.TryParse(pointLineSegments[j + 1], out var coordinate))
                {
                    ExceptionHandler.Instance.LogError("An error occurred while reading point info.", ErrorLevel.Abort);
                    return null;
                }
                points[lineIndex][j] = coordinate;
            }
        }

        if (streamReader.ReadLine() is not { } treeSectionHeaderLine
            || !treeSectionHeaderLine.Trim().StartsWith("tree"))
        {
            ExceptionHandler.Instance.LogError("Illegal dump format.	Expecting section heading", ErrorLevel.Abort);
            return null;
        }
        var treeHeaderSegments = treeSectionHeaderLine.Trim().Split(' ').ToArray();
        if (treeHeaderSegments.Length != 4
            || !int.TryParse(treeHeaderSegments[1], out dimension)
            || !int.TryParse(treeHeaderSegments[2], out numberOfPoints)
            || !int.TryParse(treeHeaderSegments[3], out var bucketSize))
        {
            ExceptionHandler.Instance.LogError($"Invalid section header: \n{pointSectionHeaderLine}.", ErrorLevel.Abort);
            return null;
        }
        var lowerBounds = AllocatorUtility.CreatePoint(dimension);
        var upperBounds = AllocatorUtility.CreatePoint(dimension);
        if (streamReader.ReadLine() is not { } lowerBoundLine
            || lowerBoundLine.Trim().Split(' ') is not { } lowerBoundLineSegments
            || lowerBoundLineSegments.Length != dimension
            || streamReader.ReadLine() is not { } upperBoundLine
            || upperBoundLine.Trim().Split(' ') is not { } upperBoundLineSegments
            || upperBoundLineSegments.Length != dimension)
        {
            ExceptionHandler.Instance.LogError("Unmatched tree dimension and bounds.", ErrorLevel.Abort);
            return null;
        }

        for (var j = 0; j < dimension; j++)
        {
            if (!double.TryParse(lowerBoundLineSegments[j + 1], out var lowerBound)
                || !double.TryParse(upperBoundLineSegments[j + 1], out var upperBound))
            {
                ExceptionHandler.Instance.LogError("An error occurred while reading point info.", ErrorLevel.Abort);
                return null;
            }
            lowerBounds[j] = lowerBound;
            upperBounds[j] = upperBound;
        }

        var pointIndices = Enumerable.Repeat(0, numberOfPoints).ToList();
        var (root, nextIndex) = ReadTree(streamReader, treeType, pointIndices, 0)!.Value;
        if (nextIndex != numberOfPoints)
        {
            ExceptionHandler.Instance.LogError("Didn't see as many points as expected", ErrorLevel.Warn);
            return null;
        }

        return (root, dimension, numberOfPoints, points, pointIndices, bucketSize, lowerBounds, upperBounds);
    }

    public static (IKdNode? Root, int NextIndex)?
        ReadTree(StreamReader streamReader, TreeType treeType, AnnIndexCollection pointIndices, int nextIndex)
    {
        if (streamReader.ReadLine() is not { } line)
        {
            ExceptionHandler.Instance.LogError("Cannot read from stream.", ErrorLevel.Abort);
            return null;
        }
        var segments = line.Trim().ToLower().Split(' ');
        if (segments[0] == "null")
        {
            return (null, nextIndex);
        }
        if (segments[0] == "leaf")
        {
            if (!int.TryParse(segments[1], out var numberOfPoints))
            {
                ExceptionHandler.Instance.LogError("An error occurred while reading point info.", ErrorLevel.Abort);
                return null;
            }
            int oldIndex = nextIndex;
            if (numberOfPoints == 0) return (Constants.KdTrivial, nextIndex);
            for (var i = 0; i < numberOfPoints; i++)
            {
                if (!int.TryParse(segments[i + 2], out var index))
                {
                    ExceptionHandler.Instance.LogError("An error occurred while reading tree info.", ErrorLevel.Abort);
                    return null;
                }
                pointIndices[nextIndex++] = index;
            }
            return (new KdLeafNode(numberOfPoints, pointIndices.Skip(oldIndex).ToList()), nextIndex);
        }
        if (segments[0] == "split")
        {
            if (!int.TryParse(segments[1], out var cuttingDimension)
                || !double.TryParse(segments[2], out var cuttingValue)
                || !double.TryParse(segments[3], out var lowerBound)
                || !double.TryParse(segments[4], out var upperbound))
            {
                ExceptionHandler.Instance.LogError("An error occurred while reading tree info.", ErrorLevel.Abort);
                return null;
            }
            (var leftChild, nextIndex) = ReadTree(streamReader, treeType, pointIndices, nextIndex)!.Value;
            (var rightChild, nextIndex) = ReadTree(streamReader, treeType, pointIndices, nextIndex)!.Value;
            return (
                new KdSplitNode(cuttingDimension, cuttingValue, lowerBound, upperbound, (leftChild, rightChild)),
                nextIndex);
        }
        if (segments[0] == "shrink")
        {
            if (treeType is not TreeType.BoxDecompositionTree)
            {
                ExceptionHandler.Instance.LogError("Shrinking node not allowed in kd-tree.", ErrorLevel.Abort);
                return null;
            }
            if (!int.TryParse(segments[1], out var numberOfBounds))
            {
                ExceptionHandler.Instance.LogError("An error occurred while reading tree info.", ErrorLevel.Abort);
                return null;
            }

            AnnOrthogonalHalfSpaceCollection bounds = new OrthogonalHalfSpace[numberOfBounds].ToList();
            for (var i = 0; i < numberOfBounds; i++)
            {
                if (streamReader.ReadLine() is not { } shrinkNodeLine
                    || shrinkNodeLine.Trim().Split(' ') is not {Length: 3} shrinkSegments
                    || !int.TryParse(shrinkSegments[0], out var cuttingDimension)
                    || !double.TryParse(shrinkSegments[1], out var cuttingValue)
                    || !int.TryParse(shrinkSegments[2], out var side))
                {
                    ExceptionHandler.Instance.LogError("An error occurred while reading tree info.", ErrorLevel.Abort);
                    return null;
                }
                bounds[i] = new(cuttingDimension, cuttingValue, side);
            }

            (var inNode, nextIndex) = ReadTree(streamReader, treeType, pointIndices, nextIndex)!.Value;
            (var outNode, nextIndex) = ReadTree(streamReader, treeType, pointIndices, nextIndex)!.Value;

            return new(new BdShrinkNode(numberOfBounds, bounds, (inNode, outNode)), nextIndex);
        }
        
        ExceptionHandler.Instance.LogError("Illegal node type in dump file", ErrorLevel.Abort);
        return null;
    }
    
    public static void Dump(
        this AnnPoint point,
        IOutputStream outputStream,
        int dimension)
    {
        for (var j = 0; j < dimension; j++)
        {
            outputStream.Output(point[j]);
            if (j < dimension - 1) outputStream.Output(" ");
        }
    }
}