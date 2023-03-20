using Ann.Enums;
using Ann.Utilities;

namespace Ann.Helpers;

public class PriorityQueue<T>
{
    private int _count;
    private int _capacity;
    private PriorityQueueNode<T>[] _nodes;

    public PriorityQueue(int capacity)
    {
        _count = 0;
        _capacity = capacity;
        _nodes = new PriorityQueueNode<T>[capacity + 1];
    }

    public bool IsEmpty => _count is 0;
    public bool IsNotEmpty => _count is not 0;

    public void Reset() => _count = 0;

    public void Insert(AnnDistance key, T info)
    {
        if (++_count > _capacity)
        {
            ExceptionHandler.Instance.LogError("Priority queue overflow.", ErrorLevel.Abort);
            return;
        }
        int r = _count;
        while (r > 1)
        {
            int p = r / 2;
            Performance.Global.LogFloatOp(1);
            if (_nodes[p].Key <= key) break;
            _nodes[r] = _nodes[p];
            r = p;
        }
        _nodes[r].Key = key;
        _nodes[r].Info = info;
    }

    public (AnnDistance, T) ExtractMin()
    {
        var keyValue = _nodes[1].Key;
        var infoValue = _nodes[1].Info;
        AnnDistance lastKey = _nodes[_count--].Key;
        int p = 1;
        int r = p << 1;
        while (r <= _count)
        {
            Performance.Global.LogFloatOp(2);
            if (r < _count && _nodes[r].Key > _nodes[r + 1].Key) r++;
            if (lastKey <= _nodes[r].Key) break;
            _nodes[p] = _nodes[r];
            p = r;
            r = p << 1;
        }
        _nodes[p] = _nodes[_count + 1];
        return (keyValue, infoValue);
    }
}