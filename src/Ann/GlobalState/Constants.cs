using Ann.Nodes;

namespace Ann.GlobalState;

public static class Constants
{
#if ANN_NO_LIMITS
    public const double MaxDouble = double.PositiveInfinity;
#else
    public const double MaxDouble = double.MaxValue;
#endif

    public static KdLeafNode? KdTrivial;
    
    public const double DoubleComparisonEpsilon = 0.00001;
    public const double FairSplitMaxAllowedAspectRatio = 3.0;

    public const double BoxDecompositionGapThreshold = 0.5d;
    public const double BoxDecompositionShrinkSideMinimum = 2;
    public const double BoxDecompositionFraction = 0.5d;
    public const double BoxDecompositionAllowedSplitFractionMaximum = 0.5d;

    public const bool AllowSelfMatch = true;

    public const double AspectRatioCeiling = 1000d;
}