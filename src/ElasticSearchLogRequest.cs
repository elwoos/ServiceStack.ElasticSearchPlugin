//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticSearchLogRequest.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - 22/11/2019 11:50
//   Altered - 22/11/2019 15:18 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

namespace ServiceStack.ElasticSearchPlugin
{
    public class ElasticSearchLogRequest : IReturnVoid
    {
        public ElasticSearchLogRequest(params ElasticSearchRequestLogEntry[] events)
        {
            Events = events;
        }

        public ElasticSearchRequestLogEntry[] Events { get; set; }
    }
}