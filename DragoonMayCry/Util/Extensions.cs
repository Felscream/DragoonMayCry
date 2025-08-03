#region

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace DragoonMayCry.Util
{
    public static class Extensions
    {
        private static readonly Random Rnd = new();

        public static T PickRandom<T>(this List<T> source)
        {
            var randIndex = Rnd.Next(source.Count);
            return source[randIndex];
        }

        public static Span<T> Shuffle<T>(this List<T> source)
        {
            var span = CollectionsMarshal.AsSpan(source);
            Rnd.Shuffle(span);
            return span;
        }
    }
}
