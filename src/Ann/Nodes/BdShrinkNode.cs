using Ann.GlobalState;
using Ann.Performance;
using Ann.Primitives;
using Ann.Utilities;

namespace Ann.Nodes;

public class BdShrinkNode : IKdNode
{
    private readonly int _numberOfBoundingHalfSpaces;
    private readonly AnnOrthogonalHalfSpaceCollection _boundingHalfSpaces;
    private readonly (IKdNode? In, IKdNode? Out)? _children;

    public BdShrinkNode(
        int numberOfBoundingHalfSpaces,
        AnnOrthogonalHalfSpaceCollection boundingHalfSpaces,
        (IKdNode? In, IKdNode? Out)? children
    )
    {
        _numberOfBoundingHalfSpaces = numberOfBoundingHalfSpaces;
        _boundingHalfSpaces = boundingHalfSpaces;
        _children = children;
    }

    public void Search(double boxDistance)
    {
        var queryPoint = Query.Search.QueryPoint;
        
        if (SearchLimit.MaximumVisit != 0 && SearchLimit.Visited > SearchLimit.MaximumVisit) return;

        AnnDistance innerDistance = 0;

        for (var i = 0; i < _numberOfBoundingHalfSpaces; i++)
        {
            if (_boundingHalfSpaces[i].Outside(queryPoint!))
            {
                innerDistance += _boundingHalfSpaces[i].Distance(queryPoint!);
            }
        }

        if (innerDistance <= boxDistance)
        {
            _children?.In?.Search(innerDistance);
            _children?.Out?.Search(boxDistance);
        }
        else
        {
            _children?.Out?.Search(boxDistance);
            _children?.Out?.Search(innerDistance);
        }
        Global.LogFloatOp(3 * _numberOfBoundingHalfSpaces);
        Global.LogShrinkingNodeVisit(1);
    }

    public void PrioritySearch(double boxDistance)
    {
        var queryPoint = Query.PrioritySearch.QueryPoint;
        
        AnnDistance innerDistance = 0;

        for (var i = 0; i < _numberOfBoundingHalfSpaces; i++)
        {
            if (_boundingHalfSpaces[i].Outside(queryPoint!))
            {
                innerDistance += _boundingHalfSpaces[i].Distance(queryPoint!);
            }
        }

        if (innerDistance <= boxDistance)
        {
            if (_children?.Out != Constants.KdTrivial)
            {
                Query.PrioritySearch.BoxPriorityQueue!.Insert(boxDistance, _children?.Out!);
            }
            _children?.In?.PrioritySearch(innerDistance);
        }
        else
        {
            if (_children?.In != Constants.KdTrivial)
            {
                Query.PrioritySearch.BoxPriorityQueue!.Insert(boxDistance, _children?.In!);
            }
            _children?.Out?.PrioritySearch(boxDistance);
        }

        Global.LogFloatOp(3 * _numberOfBoundingHalfSpaces);
        Global.LogShrinkingNodeVisit(1);
    }

    public void FixedRadiusSearch(double boxDistance)
    {
        var queryPoint = Query.FixedRadiusSearch.QueryPoint;
        
        if (SearchLimit.MaximumVisit != 0 && SearchLimit.Visited > SearchLimit.MaximumVisit) return;

        AnnDistance innerDistance = 0;
        for (var i = 0; i < _numberOfBoundingHalfSpaces; i++)
        {
            if (_boundingHalfSpaces[i].Outside(queryPoint!))
            {
                innerDistance += _boundingHalfSpaces[i].Distance(queryPoint!);
            }
        }
        if (innerDistance <= boxDistance)
        {
            _children?.In?.FixedRadiusSearch(innerDistance);
            _children?.Out?.FixedRadiusSearch(boxDistance);
        }
        else
        {
            _children?.Out?.FixedRadiusSearch(boxDistance);
            _children?.In?.FixedRadiusSearch(innerDistance);
        }
        Global.LogFloatOp(3 * _numberOfBoundingHalfSpaces);
        Global.LogShrinkingNodeVisit(1);
    }

    public void GetStatistics(KdStatistics statistics, int dimension, OrthogonalRectangle boundingBox)
    {
        KdStatistics childrenStatistics = new();
        OrthogonalRectangle innerBox =
            Tools.BoundsToBox(boundingBox, dimension, _numberOfBoundingHalfSpaces, _boundingHalfSpaces);
        
        childrenStatistics.Reset();
        _children?.In?.GetStatistics(statistics, dimension, innerBox);
        statistics.Merge(childrenStatistics);
        
        childrenStatistics.Reset();
        _children?.Out?.GetStatistics(statistics, dimension, boundingBox);
        statistics.Merge(childrenStatistics);

        statistics.Depth++;
        statistics.NumberOfShrinkingNodes++;
    }

    public void Print(int level, IOutputStream outputStream)
    {
        _children?.Out?.Print(level + 1, outputStream);
        
        outputStream.Output("    ");
        for (var i = 0; i < level; i++) outputStream.Output("..");
        outputStream.Output("Shrink");
        for (var j = 0; j < _numberOfBoundingHalfSpaces; j++)
        {
            if (j % 2 == 0)
            {
                outputStream.Output('\n');
                for (var i = 0; i < level + 2; i++)
                {
                    outputStream.Output("  ");
                }
            }
            outputStream
                .Output("  ([").Output(_boundingHalfSpaces[j].CuttingDimension).Output("]")
                .Output(_boundingHalfSpaces[j].Side > 0 ? " >= " : " < ")
                .Output(_boundingHalfSpaces[j].CuttingValue).Output(')');
        }
        outputStream.Output('\n');
        
        _children?.In?.Print(level + 1, outputStream);
    }

    public void Dump(IOutputStream outputStream)
    {
        outputStream.Output($"shrink {_numberOfBoundingHalfSpaces}\n");
        for (var j = 0; j < _numberOfBoundingHalfSpaces; j++)
        {
            _boundingHalfSpaces[j].Dump(outputStream);
        }
        _children?.In?.Dump(outputStream);
        _children?.Out?.Dump(outputStream);
    }
}