//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticSearchLoggerPlugin.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - 22/11/2019 11:25
//   Altered - 22/11/2019 11:46 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Admin;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.ElasticSearchPlugin
{
    public class ElasticSearchLoggerPlugin : IPlugin
    {
        /// <summary>
        ///     Low level delegate for appending custom properties to a log entry
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestDto"></param>
        /// <param name="response"></param>
        /// <param name="soapDuration"></param>
        public delegate Dictionary<string, object> PropertyAppender(
            IRequest request,
            object requestDto,
            object response,
            TimeSpan soapDuration);

        /// <summary>
        ///     Low level delegate for customised logging
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestDto"></param>
        /// <param name="response"></param>
        /// <param name="requestDuration"></param>
        public delegate void RawLogEvent(
            IRequest request,
            object requestDto,
            object response,
            TimeSpan requestDuration);

        private readonly ILog _log = LogManager.GetLogger(typeof(ElasticSearchLoggerPlugin));

        private IRequestLogger _logger;

        /// <summary>
        ///     Excludes requests for specific DTO types from logging, ignores RequestLog requests by default
        /// </summary>
        public IEnumerable<Type> ExcludeRequestDtoTypes { get; set; } = new List<Type>(new[] {typeof(RequestLogs)});

        /// <summary>
        ///     Exclude request body for specific DTO types from logging, ignores authentication and registration dtos by default
        /// </summary>
        public IEnumerable<Type> HideRequestBodyForRequestDtoTypes { get; set; } =
            new List<Type>(new[] {typeof(Authenticate), typeof(Register)});

        /// <summary>
        ///     Restrict access to the runtime log settings
        /// </summary>
        public List<string> RequiredRoles { get; set; } = new List<string>();

        /// <summary>
        ///     Sets a Rollbar api key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///     Turn the logging on and off, defaults to true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Log errors, defaults to true
        /// </summary>
        public bool EnableErrorTracking { get; set; } = true;

        /// <summary>
        ///     Log request bodies, defaults to false
        /// </summary>
        public bool EnableRequestBodyTracking { get; set; } = false;

        /// <summary>
        ///     Log session details, defaults to false
        /// </summary>
        public bool EnableSessionTracking { get; set; } = false;

        /// <summary>
        ///     Log responses, defaults to false
        /// </summary>
        public bool EnableResponseTracking { get; set; } = false;

        /// <summary>
        ///     Low level request filter for logging, return true to skip logging the request
        /// </summary>
        public Func<IRequest, bool> SkipLogging { get; set; }

        /// <summary>
        ///     Append custom properties to all log entries
        /// </summary>
        public PropertyAppender AppendProperties { get; set; }

        /// <summary>
        ///     Lowest level access to customised logging, executes before any other logging settings
        /// </summary>
        public RawLogEvent RawEventLogger { get; set; }

        /// <summary>
        ///     Sets the Rollbar logger by default, override with a custom implemetation of <see cref="IRequestLogger" />
        /// </summary>
        public IRequestLogger Logger
        {
            get { return _logger = _logger ?? new ElasticSearchRequestLogger(this); }
            set => _logger = value;
        }

        public string Environment { get; set; }
        public string ServerUri { get; set; }

        #region Implementation of IPlugin

        /// <summary>
        ///     Registers the plugin with the apphost
        /// </summary>
        /// <param name="appHost"></param>
        public void Register(IAppHost appHost)
        {
            ConfigureRequestLogger(appHost);

            var atRestPath = new ElasticSearchLogConfigRequest().ToPostUrl();

            appHost.RegisterService(typeof(ElasticSearchLogConfigService), atRestPath);

            if (EnableRequestBodyTracking)
                appHost.PreRequestFilters.Insert(0, (httpReq, httpRes) => { httpReq.UseBufferedStream = true; });

            appHost.GetPlugin<MetadataFeature>()
                .AddDebugLink("https://rollbar.com", "Rollbar Request Logs")
                .AddPluginLink(atRestPath.TrimStart('/'), "Rollbar IRequestLogger Configuration");
        }

        #endregion

        private void ConfigureRequestLogger(IAppHost appHost)
        {
            var requestLogger = Logger;
            requestLogger.EnableSessionTracking = EnableSessionTracking;
            requestLogger.EnableResponseTracking = EnableResponseTracking;
            requestLogger.EnableRequestBodyTracking = EnableRequestBodyTracking;
            requestLogger.EnableErrorTracking = EnableErrorTracking;
            requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes.ToArray();
            requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes.ToArray();
            requestLogger.SkipLogging = SkipLogging;
            appHost.Register(requestLogger);
        }
    }
}