using Ann.Performance;

namespace Ann.Primitives;

public interface IKdNode
{
    void Search(AnnDistance distance);
    void PrioritySearch(AnnDistance distance);
    void FixedRadiusSearch(AnnDistance distance);
    void GetStatistics(KdStatistics statistics, int dimension,
        OrthogonalRectangle boundingBox);
    void Print(
        int level,
        IOutputStream outputStream
    );
    void Dump(IOutputStream outputStream);
}