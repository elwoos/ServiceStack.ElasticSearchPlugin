//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticClientProvider.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - // 
//   Altered - 22/11/2019 15:52 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System;
using Elasticsearch.Net;
using Nest;

namespace ServiceStack.ElasticSearchPlugin
{
    public class ElasticClientProvider
    {
        /// <summary>
        /// Node connection
        /// </summary>
        /// <param name="uri">defaults to localhost if not provided</param>
        /// <param name="settings"></param>
        public ElasticClientProvider(Uri uri, Microsoft.Extensions.Options.IOptions<ElasticSearchSettings> settings)
        {
            // connect to a single node

            var node = uri;
            var settngs = new ConnectionSettings(node);
            this.Client = new ElasticClient(settngs);
        }

        /// <summary>
        /// Pool connection
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="settings"></param>
        public ElasticClientProvider(Uri[] uri, Microsoft.Extensions.Options.IOptions<ElasticSearchSettings> settings)
        {
            // connect to a multiplenodes node
            var nodes = uri;
            var pool = new StaticConnectionPool(nodes);
            var settngs = new ConnectionSettings(pool);
            this.Client = new ElasticClient(settngs);
        }

        //public ElasticClientProvider(Microsoft.Extensions.Options.IOptions<ElasticSearchSettings> settings)
        //{
        //    // Create the connection settings
        //    ConnectionSettings connectionSettings =
        //        // Get the cluster URL from appsettings.json and pass it in
        //        new ConnectionSettings(new System.Uri(settings.Value.ClusterUrl));
        //    // This is going to enable us to see the raw queries sent to elastic when debugging (really useful)
        //    connectionSettings.EnableDebugMode();

        //    if (settings.Value.DefaultIndex != null)
        //    {
        //        // Get the index name from appsettings.json and pass it in
        //        connectionSettings.DefaultIndex(settings.Value.DefaultIndex);
        //    }
        //    // Create the actual client
        //    this.Client = new ElasticClient(connectionSettings);
        //}

        public ElasticClient Client { get; }
    }
}