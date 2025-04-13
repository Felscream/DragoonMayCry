namespace DragoonMayCry.Util
{
    public class DoubleLinkedList<T>
    {
        private readonly int size;

        public DoubleLinkedList(params T[] values)
        {
            size = 0;
            for (var i = 0; i < values.Length; i++)
            {
                DoubleLinkedNode<T> node = new(values[i]);
                if (size == 0)
                {
                    Head = node;
                    Tail = node;
                }
                else if (Tail != null)
                {
                    node.Previous = Tail;
                    Tail.Next = node;
                    Tail = node;
                }
                size++;
            }
        }

        public DoubleLinkedList(T value)
        {
            var node = new DoubleLinkedNode<T>(value);
            Head = node;
            Tail = node;
            size = 1;
        }
        public DoubleLinkedNode<T>? Head { get; private set; }
        public DoubleLinkedNode<T>? Tail { get; private set; }

        public DoubleLinkedNode<T>? Find(T value)
        {
            if (value == null)
            {
                return null;
            }

            var node = Head;
            while (node != null)
            {
                if (value.Equals(node.Value))
                {
                    return node;
                }
                node = node.Next;
            }
            return null;
        }
    }

    public class DoubleLinkedNode<T>
    {

        public DoubleLinkedNode(T value)
        {
            Value = value;
        }
        public T Value { get; set; }
        public DoubleLinkedNode<T>? Previous { get; set; }
        public DoubleLinkedNode<T>? Next { get; set; }
    }
}
