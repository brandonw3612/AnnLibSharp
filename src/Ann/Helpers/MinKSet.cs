using Ann.GlobalState;

namespace Ann.Helpers;

public class MinKSet
{
    public const AnnDistance PriorityQueueNullKey = Constants.MaxDouble;
    public const int PriorityQueueNullInfo = -1;
    
    private readonly int _capacity;
    private int _count;
    private readonly MinKNode[] _nodes;

    public MinKSet(int capacity)
    {
        _capacity = capacity;
        _count = 0;
        _nodes = new MinKNode[capacity + 1];
    }

    public AnnDistance MinimumKey => _count > 0 ? _nodes[0].Key : PriorityQueueNullKey;
    public AnnDistance MaximumKey => _count == _capacity ? _nodes[_count - 1].Key : PriorityQueueNullKey;

    public AnnDistance GetAscendingKey(int index) => index < _count ? _nodes[index].Key : PriorityQueueNullKey;
    public int GetAscendingInfo(int index) => index < _count ? _nodes[index].Info : PriorityQueueNullInfo;

    public void Insert(AnnDistance key, int info)
    {
        int i;
        for (i = _count; i > 0; i--)
        {
            if (_nodes[i - 1].Key > key)
                _nodes[i] = _nodes[i - 1];
            else break;
        }
        _nodes[i].Key = key;
        _nodes[i].Info = info;
        if (_count < _capacity) _count++;
        Performance.Global.LogFloatOp(_capacity - i + 1);
    }
}