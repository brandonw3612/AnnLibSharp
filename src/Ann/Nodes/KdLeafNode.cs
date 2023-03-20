using System;
using Ann.GlobalState;
using Ann.Performance;
using Ann.Primitives;
using Ann.Utilities;

namespace Ann.Nodes;

public class KdLeafNode : IKdNode
{
    private int _numberOfPoints;
    private AnnIndexCollection _bucketOfPoints;

    public KdLeafNode(
        int numberOfPoints,
        AnnIndexCollection bucketOfPoints
    )
    {
        _numberOfPoints = numberOfPoints;
        _bucketOfPoints = bucketOfPoints;
    }
    
    public void Search(double boxDistance)
    {
        var minDistance = Query.Search.PointMinKSet!.MaximumKey;
        var queryPoint = Query.Search.QueryPoint!;
        var points = Query.Search.Points!;

        for (var i = 0; i < _numberOfPoints; i++)
        {
            var dataPoint = points[_bucketOfPoints[i]];
            AnnDistance distance = 0;
            int di = 0, qi = 0, d;
            for (d = 0; d < Query.Search.Dimension; d++)
            {
                Global.LogCoordinateHit(1);
                Global.LogFloatOp(4);
                distance += Math.Pow(queryPoint[qi++] - dataPoint[di++], 2);
                if (distance > minDistance) break;
            }

            if (d >= Query.Search.Dimension
                && (Constants.AllowSelfMatch || distance != 0))
            {
                Query.Search.PointMinKSet.Insert(distance, _bucketOfPoints[i]);
                minDistance = Query.Search.PointMinKSet.MaximumKey;
            }
        }
        Global.LogLeafNodeVisit(1);
        Global.LogPointVisit(_numberOfPoints);
        SearchLimit.Visited += _numberOfPoints;
    }

    public void PrioritySearch(double boxDistance)
    {
        var minDistance = Query.PrioritySearch.PointMinKSet!.MaximumKey;
        var queryPoint = Query.PrioritySearch.QueryPoint!;
        var points = Query.PrioritySearch.Points!;

        for (var i = 0; i < _numberOfPoints; i++)
        {
            var dataPoint = points[_bucketOfPoints[i]];
            AnnDistance distance = 0;
            int di = 0, qi = 0, d;
            for (d = 0; d < Query.PrioritySearch.Dimension; d++)
            {
                Global.LogCoordinateHit(1);
                Global.LogFloatOp(4);
                distance += Math.Pow(queryPoint[qi++] - dataPoint[di++], 2);
                if (distance > minDistance) break;
            }
            if (d >= Query.PrioritySearch.Dimension
                && (Constants.AllowSelfMatch || distance != 0))
            {
                Query.PrioritySearch.PointMinKSet.Insert(distance, _bucketOfPoints[i]);
                minDistance = Query.PrioritySearch.PointMinKSet.MaximumKey;
            }
        }
        Global.LogLeafNodeVisit(1);
        Global.LogPointVisit(_numberOfPoints);
        SearchLimit.Visited += _numberOfPoints;
    }

    public void FixedRadiusSearch(double boxDistance)
    {
        var points = Query.FixedRadiusSearch.Points!;
        var queryPoint = Query.FixedRadiusSearch.QueryPoint!;
        
        for (var i = 0; i < _numberOfPoints; i++)
        {
            var dataPoint = points[_bucketOfPoints[i]];
            int di = 0, qi = 0;
            AnnDistance distance = 0;
            int d;
            for (d = 0; d < Query.FixedRadiusSearch.Dimension; d++)
            {
                Global.LogCoordinateHit(1);
                Global.LogFloatOp(5);
                distance += Math.Pow(queryPoint[qi++] - dataPoint[di++], 2);
                if (distance > Query.FixedRadiusSearch.SquaredRadiusSearchBound) break;
            }

            if (d >= Query.FixedRadiusSearch.Dimension
                && (Constants.AllowSelfMatch || distance != 0))
            {
                Query.FixedRadiusSearch.ClosestPointSet!.Insert(distance, _bucketOfPoints[i]);
                Query.FixedRadiusSearch.NumberOfPointsInRange++;
            }
        }
        Global.LogLeafNodeVisit(1);
        Global.LogPointVisit(_numberOfPoints);
        Query.FixedRadiusSearch.NumberOfPointsVisited += _numberOfPoints;
    }

    public void GetStatistics(KdStatistics statistics, int dimension, OrthogonalRectangle boundingBox)
    {
        statistics.Reset();
        statistics.NumberOfLeaves = 1;
        if (this == Constants.KdTrivial) statistics.NumberOfTrivialLeaves = 1;
        double aspectRatio = Tools.AspectRatio(dimension, boundingBox);
        statistics.AspectRatioSum += (float) Math.Min(aspectRatio, Constants.AspectRatioCeiling);
    }

    public void Print(int level, IOutputStream outputStream)
    {
        outputStream.Output("    ");
        for (var i = 0; i < level; i++) outputStream.Output("..");
        if (this == Constants.KdTrivial) outputStream.Output("Leaf (trivial)\n");
        else
        {
            outputStream.Output($"Leaf n={_numberOfPoints} <");
            for (var j = 0; j < _numberOfPoints; j++)
            {
                outputStream.Output(_bucketOfPoints[j]);
                if (j < _numberOfPoints - 1) outputStream.Output(',');
            }
            outputStream.Output(">\n");
        }
    }

    public void Dump(IOutputStream outputStream)
    {
        if (this == Constants.KdTrivial)
        {
            outputStream.Output("leaf 0\n");
            return;
        }
        outputStream.Output($"leaf {_numberOfPoints}");
        for (var j = 0; j < _numberOfPoints; j++)
        {
            outputStream.Output($" {_bucketOfPoints[j]}");
        }
        outputStream.Output('\n');
    }
}