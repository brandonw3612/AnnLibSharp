using Ann.Enums;
using Ann.GlobalState;
using Ann.Helpers;
using Ann.Primitives;
using Ann.Utilities;

namespace Ann.Trees;

public class BruteForce : IPointSet
{
    public int Dimension { get; private set; }
    public int NumberOfPoints { get; private set; }
    public AnnPointCollection Points { get; private set; }

    public BruteForce(
        AnnPointCollection points,
        int numberOfPoints,
        int dimension
    )
    {
        Dimension = dimension;
        NumberOfPoints = numberOfPoints;
        Points = points;
    }
    
    public void SearchNeighbors(
        AnnPoint queryPoint,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0
    )
    {
        if (numberOfNeighbors > NumberOfPoints)
        {
            ExceptionHandler.Instance.LogError("Requesting more near neighbors than data points", ErrorLevel.Abort);
            return;
        }
        MinKSet minKSet = new(numberOfNeighbors);
        for (var i = 0; i < NumberOfPoints; i++)
        {
            AnnDistance squaredDistance = Tools.Distance(Dimension, Points[i], queryPoint);
            if (Constants.AllowSelfMatch || squaredDistance != 0)
            {
                minKSet.Insert(squaredDistance, i);
            }
        }

        for (var i = 0; i < numberOfNeighbors; i++)
        {
            distanceToNearestNeighbors[i] = minKSet.GetAscendingKey(i);
            nearestNeighbors[i] = minKSet.GetAscendingInfo(i);
        }
    }

    public int FixedRadiusSearchNeighbors(
        AnnPoint queryPoint,
        double squaredRadius,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnPoint distanceToNearestNeighbors,
        double errorBound = 0
    )
    {
        MinKSet minKSet = new(numberOfNeighbors);
        int pointsInRange = 0;
        for (var i = 0; i < NumberOfPoints; i++)
        {
            AnnDistance squaredDistance = Tools.Distance(Dimension, Points[i], queryPoint);
            if (squaredDistance <= squaredRadius && (Constants.AllowSelfMatch || squaredDistance != 0))
            {
                minKSet.Insert(squaredDistance, i);
                pointsInRange++;
            }
        }

        for (var i = 0; i < numberOfNeighbors; i++)
        {
            if (distanceToNearestNeighbors is not null)
            {
                distanceToNearestNeighbors[i] = minKSet.GetAscendingKey(i);
            }

            if (nearestNeighbors is not null)
            {
                nearestNeighbors[i] = minKSet.GetAscendingInfo(i);
            }
        }

        return pointsInRange;
    }
}