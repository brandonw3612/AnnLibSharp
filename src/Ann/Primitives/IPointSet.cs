namespace Ann.Primitives;

public interface IPointSet
{
    void SearchNeighbors(
        AnnPoint queryPoint,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0d
    );

    int FixedRadiusSearchNeighbors(
        AnnPoint queryPoint,
        AnnDistance squaredRadius,
        int numberOfNeighbors,
        AnnIndexCollection nearestNeighbors,
        AnnDistanceCollection distanceToNearestNeighbors,
        double errorBound = 0d
    );
    
    int Dimension { get; }
    int NumberOfPoints { get; }
    
    AnnPointCollection Points { get; }
}