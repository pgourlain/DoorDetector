using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDetector
{
    static class LinqExtensions
    {
        public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(
            this IEnumerable<TLeft> leftItems,
            IEnumerable<TRight> rightItems,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector,
            Func<TLeft, TRight, TResult> resultSelector)
        {

            return from left in leftItems
                   join right in rightItems on leftKeySelector(left) equals rightKeySelector(right) into temp
                   from right in temp.DefaultIfEmpty()
                   select resultSelector(left, right);
        }
    }
}
