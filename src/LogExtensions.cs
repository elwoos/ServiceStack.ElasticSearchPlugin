//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=LogExtensions.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - // 
//   Altered - 22/11/2019 15:11 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace ServiceStack.ElasticSearchPlugin
{
    public static class LogExtensions
    {
        public static Dictionary<string, object> WithoutDuplicates(this Dictionary<string, object> items)
        {
            items.Remove("__session");
            items.Remove("_requestDurationStopwatch");
            items.Remove("x-mac-requestId");
            return items;
        }
    }
}