using Ann.Utilities;

namespace Ann.Primitives;

public class OrthogonalRectangle
{
    public AnnPoint LowerBounds { get; set; }
    public AnnPoint UpperBounds { get; set; }

    public OrthogonalRectangle(
        int dimension,
        AnnCoordinate lowerBound = 0d,
        AnnCoordinate upperBound = 0d
    )
    {
        LowerBounds = AllocatorUtility.CreatePoint(dimension, lowerBound);
        UpperBounds = AllocatorUtility.CreatePoint(dimension, upperBound);
    }

    public OrthogonalRectangle(
        int dimension,
        OrthogonalRectangle another
    )
    {
        LowerBounds = AllocatorUtility.CopyPoint(dimension, another.LowerBounds);
        UpperBounds = AllocatorUtility.CopyPoint(dimension, another.UpperBounds);
    }

    public OrthogonalRectangle(
        int dimension,
        AnnPoint lowPoint,
        AnnPoint highPoint
    )
    {
        LowerBounds = AllocatorUtility.CopyPoint(dimension, lowPoint);
        UpperBounds = AllocatorUtility.CopyPoint(dimension, highPoint);
    }

    public bool Inside(
        int dimension,
        AnnPoint point
    )
    {
        for (var i = 0; i < dimension; i++)
        {
            if (point[i] < LowerBounds[i] || point[i] > UpperBounds[i]) return false;
        }
        return true;
    }
}