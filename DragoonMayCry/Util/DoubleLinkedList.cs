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
        public DoubleLinkedNode<T>? Head { get; private set; }
        DoubleLinkedNode<T>? tail;
        int size;

        public DoubleLinkedList()
        {
            Head = null;
            tail = null;
            size = 0;
        }

        public DoubleLinkedList(T value)
        {
            var node = new DoubleLinkedNode<T>(value);
            Head = node;
            tail = node;
            size = 1;
        }

        public void Add(T value)
        {
            var node = new DoubleLinkedNode<T>(value);
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

    public class DoubleLinkedNode<T>
    {
        public T Value { get; set; }
        public DoubleLinkedNode<T>? Previous { get; set; }
        public DoubleLinkedNode<T>? Next { get; set; }

        public DoubleLinkedNode(T value){
            Value = value;
        }
    }
}
