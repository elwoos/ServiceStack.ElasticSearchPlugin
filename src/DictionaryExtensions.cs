//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=DictionaryExtensions.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - // 
//   Altered - 22/11/2019 16:11 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace ServiceStack.ElasticSearchPlugin
{
    public static class DictionaryExtensions
    {
        public static V GetOrAdd<K, V>(this SortedDictionary<K, V> map, K key, Func<K, V> createFn)
        {
            //simulate ConcurrentDictionary.GetOrAdd
            lock (map)
            {
                if (!map.TryGetValue(key, out var val))
                    map[key] = val = createFn(key);

                return val;
            }
        }
    }
}