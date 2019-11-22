//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticSearchSettings.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - // 
//   Altered - 22/11/2019 11:52 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace ServiceStack.ElasticSearchPlugin
{
    /// <summary>
/// Encapsulate all the appsettings.json configuration for the ElasticSearch RequestLogger feature
/// </summary>
public class ElasticSearchSettings
    {
        public string ClusterUrl { get; set; }

        public string DefaultIndex
        {
            get
            {
                return this.defaultIndex;
            }
            set
            {
                this.defaultIndex = value.ToLower();
            }
        }

        private string defaultIndex;

        public bool Enabled { get; set; }

        public bool EnableErrorTracking { get; set; }

        public bool EnableRequestBodyTracking { get; set; }

        public bool EnableSessionTracking { get; set; }

        public bool EnableResponseTracking { get; set; }

        public List<string> RequiredRoles { get; set; } = new List<string>();


        /// <summary>
        /// Manually set the environment to track the events origin/host
        /// </summary>
        public string Environment { get; set; } = "Production";
    }
}