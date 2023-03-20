using System;
using Ann.GlobalState;
using Ann.Performance;
using Ann.Primitives;

namespace Ann.Nodes;

public class KdSplitNode : IKdNode
{
    private int _cuttingDimension;
    private AnnCoordinate _cuttingValue;
    private (AnnCoordinate Lower, AnnCoordinate Upper) _cuttingBounds;
    private (IKdNode? Low, IKdNode? High)? _children;
    
    public KdSplitNode(
        int cuttingDimension,
        AnnCoordinate cuttingValue,
        AnnCoordinate lowerBound,
        AnnCoordinate upperBound,
        (IKdNode? Low, IKdNode? High)? children = null)
    {
        _cuttingDimension = cuttingDimension;
        _cuttingValue = cuttingValue;
        _cuttingBounds = (lowerBound, upperBound);
        _children = children;
    }
    
    public void Search(double boxDistance)
    {
        if (SearchLimit.MaximumVisit != 0 && SearchLimit.Visited > SearchLimit.MaximumVisit) return;

        var queryPoint = Query.Search.QueryPoint!;

        AnnCoordinate cuttingPlaneDistance = queryPoint[_cuttingDimension] - _cuttingValue;
        if (cuttingPlaneDistance < 0)
        {
            _children!.Value.Low!.Search(boxDistance);
            AnnCoordinate boxDifference = _cuttingBounds.Lower - queryPoint[_cuttingDimension];
            if (boxDifference < 0) boxDifference = 0;
            boxDistance += Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (boxDistance * Query.Search.MaxSquaredToleranceError < Query.Search.PointMinKSet!.MaximumKey)
            {
                _children!.Value.High!.Search(boxDistance);
            }
        }
        else
        {
            _children!.Value.High!.Search(boxDistance);
            AnnCoordinate boxDifference = queryPoint[_cuttingDimension] - _cuttingBounds.Upper;
            if (boxDifference < 0) boxDifference = 0;
            boxDistance += Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (boxDistance * Query.Search.MaxSquaredToleranceError < Query.Search.PointMinKSet!.MaximumKey)
            {
                _children!.Value.Low!.Search(boxDistance);
            }
        }
        Global.LogFloatOp(10);
        Global.LogSplitNodeVisit(1);
    }

    public void PrioritySearch(double boxDistance)
    {
        AnnDistance newDistance;

        var queryPoint = Query.PrioritySearch.QueryPoint!;

        AnnCoordinate cuttingPlaneDistance = queryPoint[_cuttingDimension] - _cuttingValue;

        if (cuttingPlaneDistance < 0)
        {
            AnnCoordinate boxDifference = _cuttingBounds.Lower - queryPoint[_cuttingDimension];
            if (boxDifference < 0) boxDifference = 0;
            newDistance = boxDistance + Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (_children!.Value.High != Constants.KdTrivial)
            {
                Query.PrioritySearch.BoxPriorityQueue!.Insert(newDistance, _children!.Value.High!);
            }
            _children!.Value.Low!.PrioritySearch(boxDistance);
        }
        else
        {
            AnnCoordinate boxDifference = queryPoint[_cuttingDimension] - _cuttingBounds.Upper;
            if (boxDifference < 0) boxDifference = 0;
            newDistance = boxDistance + Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (_children!.Value.Low != Constants.KdTrivial)
            {
                Query.PrioritySearch.BoxPriorityQueue!.Insert(newDistance, _children!.Value.Low!);
            }
            _children!.Value.High!.PrioritySearch(boxDistance);
        }
        Global.LogFloatOp(8);
        Global.LogSplitNodeVisit(1);
    }

    public void FixedRadiusSearch(double boxDistance)
    {
        if (SearchLimit.MaximumVisit != 0 &&
            Query.FixedRadiusSearch.NumberOfPointsVisited > SearchLimit.MaximumVisit) return;

        var queryPoint = Query.FixedRadiusSearch.QueryPoint!;

        AnnCoordinate cuttingPlaneDistance = queryPoint[_cuttingDimension] - _cuttingValue;

        if (cuttingPlaneDistance < 0)
        {
            _children?.Low?.FixedRadiusSearch(boxDistance);
            AnnCoordinate boxDifference = _cuttingBounds.Lower - queryPoint[_cuttingDimension];
            if (boxDifference < 0) boxDifference = 0;
            boxDistance += Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (boxDistance * Query.FixedRadiusSearch.MaxSquaredToleranceError <=
                Query.FixedRadiusSearch.SquaredRadiusSearchBound)
            {
                _children?.High?.FixedRadiusSearch(boxDistance);
            }
        }
        else
        {
            _children?.High?.FixedRadiusSearch(boxDistance);
            AnnCoordinate boxDifference = queryPoint[_cuttingDimension] - _cuttingBounds.Upper;
            if (boxDifference < 0) boxDifference = 0;
            boxDistance += Math.Pow(cuttingPlaneDistance, 2) - Math.Pow(boxDifference, 2);
            if (boxDistance * Query.FixedRadiusSearch.MaxSquaredToleranceError <=
                Query.FixedRadiusSearch.SquaredRadiusSearchBound)
            {
                _children?.Low?.FixedRadiusSearch(boxDistance);
            }
        }
        Global.LogFloatOp(13);
        Global.LogSplitNodeVisit(1);
    }

    public void GetStatistics(KdStatistics statistics, int dimension, OrthogonalRectangle boundingBox)
    {
        KdStatistics childStatistics = new();
        AnnCoordinate highValue = boundingBox.UpperBounds[_cuttingDimension];
        boundingBox.UpperBounds[_cuttingDimension] = _cuttingValue;
        childStatistics.Reset();
        _children!.Value.Low!.GetStatistics(childStatistics, dimension, boundingBox);
        statistics.Merge(childStatistics);
        boundingBox.UpperBounds[_cuttingDimension] = highValue;

        AnnCoordinate lowValue = boundingBox.LowerBounds[_cuttingDimension];
        boundingBox.LowerBounds[_cuttingDimension] = _cuttingValue;
        childStatistics.Reset();
        _children!.Value.High!.GetStatistics(childStatistics, dimension, boundingBox);
        statistics.Merge(childStatistics);
        boundingBox.LowerBounds[_cuttingDimension] = lowValue;

        statistics.Depth++;
        statistics.NumberOfSplittingNodes++;
    }

    public void Print(int level, IOutputStream outputStream)
    {
        _children!.Value.High!.Print(level + 1, outputStream);
        outputStream.Output("    ");
        for (var i = 0; i < level; i++) outputStream.Output("..");
        outputStream.Output($"Split cd={_cuttingDimension} cv={_cuttingValue} ");
        outputStream.Output($"lbnd={_cuttingBounds.Lower} hbnd={_cuttingBounds.Upper}\n");
        _children!.Value.Low!.Print(level + 1, outputStream);
    }

    public void Dump(IOutputStream outputStream)
    {
        outputStream.Output(
            $"split {_cuttingDimension} {_cuttingValue} {_cuttingBounds.Lower} {_cuttingBounds.Upper}\n");
        _children!.Value.Low!.Dump(outputStream);
        _children!.Value.High!.Dump(outputStream);
    }
}