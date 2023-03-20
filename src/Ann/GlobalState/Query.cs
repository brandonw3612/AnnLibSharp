using Ann.Helpers;
using Ann.Primitives;

namespace Ann.GlobalState;

public static class Query
{
    public static class FixedRadiusSearch
    {
        public static int Dimension { get; set; }
        public static AnnPoint? QueryPoint { get; set; }
        public static AnnDistance SquaredRadiusSearchBound { get; set; }
        public static double MaxSquaredToleranceError { get; set; }
        public static AnnPointCollection? Points { get; set; }
        public static MinKSet? ClosestPointSet { get; set; }
        public static int NumberOfPointsVisited { get; set; }
        public static int NumberOfPointsInRange { get; set; }
    }

    public static class PrioritySearch
    {
        public static double ErrorBound { get; set; }
        public static int Dimension { get; set; }
        public static AnnPoint? QueryPoint { get; set; }
        public static double MaxSquaredToleranceError { get; set; }
        public static AnnPointCollection? Points { get; set; }
        public static PriorityQueue<IKdNode>? BoxPriorityQueue { get; set; }
        public static MinKSet? PointMinKSet { get; set; }
    }

    public static class Search
    {
        public static int Dimension { get; set; }
        public static AnnPoint? QueryPoint { get; set; }
        public static double MaxSquaredToleranceError { get; set; }
        public static AnnPointCollection? Points { get; set; }
        public static MinKSet? PointMinKSet { get; set; }
    }
}