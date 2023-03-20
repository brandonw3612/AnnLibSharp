using System.Linq;
using Ann.Primitives;

namespace Ann.Utilities;

public static class AllocatorUtility
{
    public static AnnPoint CreatePoint(
        int dimension,
        AnnCoordinate coordinate = 0d
    ) =>
        Enumerable.Repeat(coordinate, dimension).ToList();

    public static AnnPointCollection CreatePoints(
        int numberOfPoints,
        int dimension
    )
    {
        AnnPointCollection points = new();
        for (var i = 0; i < numberOfPoints; i++)
        {
            points.Add(CreatePoint(dimension, 0d));
        }
        return points;
    }
    
    public static AnnPoint CopyPoint(
        int dimension,
        AnnPoint source
    )
    {
        AnnPoint destination = new();
        for (var i = 0; i < dimension; i++)
        {
            destination.Add(source[i]);
        }
        return destination;
    }

    public static void AssignRectangle(
        int dimension,
        OrthogonalRectangle source,
        OrthogonalRectangle destination)
    {
        for (var i = 0; i < dimension; i++)
        {
            destination.LowerBounds[i] = source.LowerBounds[i];
            destination.UpperBounds[i] = source.UpperBounds[i];
        }
    }
}