//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticSearchRequestLogEntry.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - // 
//   Altered - 22/11/2019 15:15 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace ServiceStack.ElasticSearchPlugin
{
    /// <summary>
    /// A log entry added by the IRequestLogger
    /// </summary>
    public class ElasticSearchRequestLogEntry
    {
        public ElasticSearchRequestLogEntry()
        {
            MessageTemplate = "Servicestack ElasticSearchRequestLogsFeature";
            Properties = new SortedDictionary<string, object>();
            Level = "Debug";
        }

        public string Timestamp { get; set; }

        public string Level { get; set; }

        public SortedDictionary<string, object> Properties { get; }

        public string MessageTemplate { get; set; }

        public string Exception { get; set; }
    }
}