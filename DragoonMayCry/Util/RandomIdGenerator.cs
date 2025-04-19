#region

using System;

#endregion

namespace DragoonMayCry.Util
{
    public class RandomIdGenerator
    {
        private static readonly Random Random = new();

        public static long GenerateId()
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long randomTail = Random.Next(1000);
            return timestamp * 1000 + randomTail;
        }
    }
}
