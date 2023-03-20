namespace Ann.Helpers;

public struct PriorityQueueNode<T>
{
    public AnnDistance Key { get; set; }
    public T Info { get; set; }
}