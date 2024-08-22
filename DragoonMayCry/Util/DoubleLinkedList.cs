using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Util
{
    public class DoubleLinkedList<T>
    {
        public DoubleNode<T>? Head { get; private set; }
        DoubleNode<T>? tail;
        int size;

        public DoubleLinkedList()
        {
            Head = null;
            tail = null;
            size = 0;
        }

        public DoubleLinkedList(T value)
        {
            var node = new DoubleNode<T>(value);
            Head = node;
            tail = node;
            size = 1;
        }

        public void Add(T value)
        {
            var node = new DoubleNode<T>(value);
            Head ??= node;
            
            if(tail == null)
            {
                tail = node;
            } else
            {
                tail.Next = node;
                tail = node;
            }
            
            size++;
        }
    }

    public class DoubleNode<T>
    {
        public T Value { get; set; }
        public DoubleNode<T>? Previous { get; set; }
        public DoubleNode<T>? Next { get; set; }

        public DoubleNode(T value){
            Value = value;
        }
    }
}
