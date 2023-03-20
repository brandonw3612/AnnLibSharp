using System;

namespace Ann.Primitives;

public class OrthogonalHalfSpace
{
    public int CuttingDimension { get; set; }
    public AnnCoordinate CuttingValue { get; set; }
    public int Side { get; set; }

    public OrthogonalHalfSpace()
        : this(0, 0d, 0)
    {
        // Nothing further.
    }

    public OrthogonalHalfSpace(
        int cuttingDimension,
        AnnCoordinate cuttingValue,
        int side
    )
    {
        CuttingDimension = cuttingDimension;
        CuttingValue = cuttingValue;
        Side = side;
    }

    public bool Inside(AnnPoint point) => (point[CuttingDimension] - CuttingValue) * Side >= 0;
    public bool Outside(AnnPoint point) => (point[CuttingDimension] - CuttingValue) * Side > 0;
    public AnnDistance Distance(AnnPoint point) => Math.Pow(point[CuttingDimension] - CuttingValue, 2);

    public void SetLowerBound(
        int dimension,
        AnnPoint point
    )
    {
        CuttingDimension = dimension;
        CuttingValue = point[dimension];
        Side = 1;
    }

    public void SetUpperBound(
        int dimension,
        AnnPoint point
    )
    {
        CuttingDimension = dimension;
        CuttingValue = point[dimension];
        Side = -1;
    }

    public void Project(AnnPoint point)
    {
        if (Outside(point))
        {
            point[CuttingDimension] = CuttingValue;
        }
    }

    public void Dump(IOutputStream outputStream)
    {
        outputStream.Output($"{CuttingDimension} {CuttingValue} {Side}\n");
    }
}