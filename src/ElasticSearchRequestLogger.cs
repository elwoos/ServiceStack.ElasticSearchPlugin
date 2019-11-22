//   --------------------------------------------------------------------------------------------------------------------
//   <copyright file=ElasticSearchRequestLogger.cs company="North Lincolnshire Council">
//   Solution : -  ServiceStack.ElasticSearchPlugin
// 
//   </copyright>
//   <summary>
// 
//   Created - 22/11/2019 10:34
//   Altered - 22/11/2019 12:09 - Stephen Ellwood
// 
//   Project : - ServiceStack.ElasticSearchPlugin
// 
//   </summary>
//   --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.ElasticSearchPlugin
{

    using System.Collections.Generic;

    public class ElasticSearchRequestLogger : IRequestLogger
    {
        private readonly ElasticSearchLoggerPlugin _plugin;

        private static int requestId;

        private readonly string eventsUri;



        public ElasticSearchRequestLogger(ElasticSearchLoggerPlugin plugin)
        {
            _plugin = plugin;

            eventsUri = plugin.ServerUri;

            // The request message MUST be serialized to omit NULL TraceChain and other properties. 
            // We enforce the custom configuration here, so that regardless of what AppHost setting we have, 
            // the logger will use the appropriate serialization strategy
            JsConfig<ElasticSearchLogRequest>.RawSerializeFn = obj =>
            {
                using (var config = JsConfig.BeginScope())
                {
                    config.TextCase = TextCase.CamelCase;
                    config.IncludeNullValues = false;
                    return obj.ToJson();
                }
            };
        }

        public bool IsLoggingEnabled
        {
            get => _plugin.Enabled;
            set => _plugin.Enabled = value;
        }

        /// <inheritdoc />
        public void Log(IRequest request, object requestDto, object response, TimeSpan elapsed)
        {
            try
            {
                // bypasses all flags to run raw log event delegate if configured
                _plugin.RawEventLogger?.Invoke(request, requestDto, response, elapsed);

                // if logging disabled
                if (!IsLoggingEnabled) return;

                // check any custom filter
                if (_plugin.SkipLogging?.Invoke(request) == true) return;

                // skip logging any dto exclusion types set
                var requestType = requestDto?.GetType();
                if (ExcludeRequestType(requestType)) return;

                var entry = CreateEntry(request, requestDto, response, elapsed, requestType);
                BufferedLogEntries(entry);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(ElasticSearchRequestLogger))
                    .Error("ElasticSearchRequestLogger threw unexpected exception", ex);
            }
        }


        /// <inheritdoc />
        public List<RequestLogEntry> GetLatestLogs(int? take)
        {
            throw new NotImplementedException();
        }

        public bool EnableSessionTracking
        {
            get => _plugin.EnableSessionTracking;
            set => _plugin.EnableSessionTracking = value;
        }

        public bool EnableRequestBodyTracking
        {
            get => _plugin.EnableRequestBodyTracking;
            set => _plugin.EnableRequestBodyTracking = value;
        }

        public bool EnableResponseTracking
        {
            get => _plugin.EnableResponseTracking;
            set => _plugin.EnableResponseTracking = value;
        }

        public bool EnableErrorTracking
        {
            get => _plugin.EnableErrorTracking;
            set => _plugin.EnableErrorTracking = value;
        }

        /// <inheritdoc />
        public bool LimitToServiceRequests { get; set; }

        public string[] RequiredRoles
        {
            get => _plugin.RequiredRoles?.ToArray();
            set => _plugin.RequiredRoles = value?.ToList();
        }

        /// <inheritdoc />
        public Func<IRequest, bool> SkipLogging { get; set; }

        public Type[] ExcludeRequestDtoTypes
        {
            get => _plugin.ExcludeRequestDtoTypes?.ToArray();
            set => _plugin.ExcludeRequestDtoTypes = value?.ToList();
        }

        public Type[] HideRequestBodyForRequestDtoTypes
        {
            get => _plugin.HideRequestBodyForRequestDtoTypes?.ToArray();
            set => _plugin.HideRequestBodyForRequestDtoTypes = value?.ToList();
        }

        /// <inheritdoc />
        public Action<IRequest, RequestLogEntry> RequestLogFilter { get; set; }

        /// <inheritdoc />
        public Func<DateTime> CurrentDateFn { get; set; }

        protected bool ExcludeRequestType(Type requestType)
        {
            return ExcludeRequestDtoTypes != null
                   && requestType != null
                   && ExcludeRequestDtoTypes.Contains(requestType);
        }

        private void BufferedLogEntries(ElasticSearchRequestLogEntry entry)
        {
            // TODO add buffering to logging for perf
            // scope to force json camel casing off
            using (JsConfig.With(new Config { TextCase = TextCase.Default }))
            {
                eventsUri.PostJsonToUrlAsync(
                    new ElasticSearchLogRequest(entry),
                    webRequest =>
                    {
                        if (!string.IsNullOrWhiteSpace(_plugin.ApiKey))
                            webRequest.Headers.Add("X-Seq-ApiKey", _plugin.ApiKey);
                    });
            }
        }

        protected ElasticSearchRequestLogEntry CreateEntry(
            IRequest request,
            object requestDto,
            object responseMessage,
            TimeSpan requestDuration,
            Type requestType)
        {
            var requestLogEntry = new ElasticSearchRequestLogEntry();
            requestLogEntry.Timestamp = DateTime.UtcNow.ToString("o");
            requestLogEntry.MessageTemplate = "HTTP {HttpMethod} {PathInfo} responded {StatusCode} in {ElapsedMilliseconds}ms";
            requestLogEntry.Properties.Add("IsRequestLog", "True"); // Used for filtering requests easily
            requestLogEntry.Properties.Add("SourceContext", "ServiceStack.Seq.RequestLogsFeature");
            requestLogEntry.Properties.Add("ElapsedMilliseconds", requestDuration.TotalMilliseconds);
            requestLogEntry.Properties.Add("RequestCount", Interlocked.Increment(ref requestId).ToString());
            requestLogEntry.Properties.Add("ServiceName", HostContext.AppHost.ServiceName);

            if (request != null)
            {
                requestLogEntry.Properties.Add("HttpMethod", request.Verb);
                requestLogEntry.Properties.Add("AbsoluteUri", request.AbsoluteUri);
                requestLogEntry.Properties.Add("PathInfo", request.PathInfo);
                requestLogEntry.Properties.Add("IpAddress", request.UserHostAddress);
                requestLogEntry.Properties.Add("ForwardedFor", request.Headers[HttpHeaders.XForwardedFor]);
                requestLogEntry.Properties.Add("Referer", request.Headers[HttpHeaders.Referer]);
                requestLogEntry.Properties.Add("Session", EnableSessionTracking ? request.GetSession(false) : null);
                requestLogEntry.Properties.Add("Items", request.Items.WithoutDuplicates());
                requestLogEntry.Properties.Add("StatusCode", request.Response?.StatusCode);
                requestLogEntry.Properties.Add("StatusDescription", request.Response?.StatusDescription);
                requestLogEntry.Properties.Add("ResponseStatus", request.Response?.GetResponseStatus());
            }

            var isClosed = request.Response.IsClosed;
            if (!isClosed)
            {
                requestLogEntry.Properties.Add("UserAuthId", request.GetItemOrCookie(HttpHeaders.XUserAuthId));
                requestLogEntry.Properties.Add("SessionId", request.GetSessionId());
            }

            if (HideRequestBodyForRequestDtoTypes != null
                && requestType != null
                && !HideRequestBodyForRequestDtoTypes.Contains(requestType))
            {
                requestLogEntry.Properties.Add("RequestDto", requestDto);
                if (request != null)
                {
                    if (!isClosed)
                    {
                        requestLogEntry.Properties.Add("FormData", request.FormData.ToDictionary());
                    }

                    if (EnableRequestBodyTracking)
                    {
                        requestLogEntry.Properties.Add("RequestBody", request.GetRawBody());
                    }
                }
            }

            if (!responseMessage.IsErrorResponse())
            {
                if (EnableResponseTracking)
                {
                    requestLogEntry.Properties.Add("ResponseDto", responseMessage);
                }
            }
            else if (EnableErrorTracking)
            {
                if (responseMessage is IHttpError errorResponse)
                {
                    requestLogEntry.Level = errorResponse.StatusCode >= HttpStatusCode.BadRequest
                                            && errorResponse.StatusCode < HttpStatusCode.InternalServerError
                                                ? "Warning"
                                                : "Error";
                    requestLogEntry.Properties["StatusCode"] = (int)errorResponse.StatusCode;
                    requestLogEntry.Properties.Add("ErrorCode", errorResponse.ErrorCode);
                    requestLogEntry.Properties.Add("ErrorMessage", errorResponse.Message);
                    requestLogEntry.Properties.Add("StackTrace", errorResponse.StackTrace);
                }

                var ex = responseMessage as Exception;
                if (ex != null)
                {
                    if (ex.InnerException != null)
                    {
                        requestLogEntry.Exception = ex.InnerException.ToString();
                        requestLogEntry.Properties.Add("ExceptionSource", ex.InnerException.Source);
                        requestLogEntry.Properties.Add("ExceptionData", ex.InnerException.Data);
                    }
                    else
                    {
                        requestLogEntry.Exception = ex.ToString();
                    }
                }
            }

            if (AppendProperties != null)
            {
                foreach (var kvPair in AppendProperties?.Invoke(request, requestDto, responseMessage, requestDuration).Safe())
                {
                    requestLogEntry.Properties.GetOrAdd(kvPair.Key, key => kvPair.Value);
                }
            }

            foreach (var header in request.Headers.ToDictionary())
            {
                if (!requestLogEntry.Properties.ContainsValue(header.Value))
                {
                    requestLogEntry.Properties.Add($"Header-{header.Key}", header.Value);
                }
            }

            return requestLogEntry;


            //var elasticSearchConfig = new ElasticSearchConfig(_plugin.ApiKey)
            //{
            //    Environment = _plugin.Environment
            //};

            //var messageErrorLevel = ErrorLevel.Info;

            //var loggableRequest = new Request();

            ////    Person loggablePerson = null;

            //if (request != null)
            //{
            //    //if (EnableSessionTracking)
            //    //{
            //    //    var session = request.GetSession();
            //    //    loggablePerson = new Person
            //    //    {
            //    //        Id = session.UserAuthId,
            //    //        Email = session.Email,
            //    //        UserName = session.UserName
            //    //    };
            //    //}

            //    loggableRequest = new Request
            //    {
            //        Url = request.AbsoluteUri,
            //        Method = request.Verb,
            //        Headers = request.Headers.ToDictionary(),
            //        // TODO set up Route params if using with MVC. I dont use MVC, so I haven't bothered
            //        Params = new Dictionary<string, object>(),
            //        UserIp = request.UserHostAddress
            //    };
            //    if (request.Verb == HttpMethods.Get)
            //    {
            //        loggableRequest.GetParams = new Dictionary<string, object>();
            //        foreach (var x in request.GetRequestParams())
            //            loggableRequest.GetParams.Add(new KeyValuePair<string, object>(x.Key, x.Value));
            //        loggableRequest.QueryString = request.PathInfo;
            //    }
            //    else if (request.Verb == HttpMethods.Post)
            //    {
            //        loggableRequest.PostParams = new Dictionary<string, object>();
            //        foreach (var x in request.GetRequestParams())
            //            loggableRequest.PostParams.Add(new KeyValuePair<string, object>(x.Key, x.Value));
            //    }

            //    var isClosed = request?.Response?.IsClosed ?? false;
            //    if (!isClosed)
            //    {
            //        loggableRequest.UserAuthId = request.GetItemOrCookie(HttpHeaders.XUserAuthId);
            //        loggableRequest.SessionId = request.GetSessionId();
            //    }

            //    if (HideRequestBodyForRequestDtoTypes != null
            //        && requestType != null
            //        && !HideRequestBodyForRequestDtoTypes.Contains(requestType))
            //    {
            //        if (!isClosed) loggableRequest.PostBody = request.FormData.ToDictionary().ToJson();

            //        if (EnableRequestBodyTracking) loggableRequest.PostBody = request.GetRawBody();
            //    }
            //}

            //var loggableBody =
            //    new Body("Developer Error: some strange condition occurred in the Rollbar request logging plugin");
            //if (!responseMessage.IsErrorResponse())
            //{
            //    Message loggableMessage;
            //    if (EnableResponseTracking)
            //    {
            //        var messageBody =
            //            $"HTTP {request?.Verb} {request?.PathInfo} responded {request?.Response?.StatusCode} in {requestDuration.TotalMilliseconds}ms";
            //        loggableMessage = new Message(messageBody, responseMessage.ToSafePartialObjectDictionary());
            //    }
            //    else
            //    {
            //        loggableMessage = new Message("Response Tracking currently disabled");
            //    }

            //    loggableBody = new Body(loggableMessage);
            //}
            //else
            //{
            //    if (EnableErrorTracking)
            //    {
            //        if (responseMessage is IHttpError errorResponse)
            //        {
            //            messageErrorLevel = errorResponse.StatusCode >= HttpStatusCode.BadRequest
            //                                && errorResponse.StatusCode < HttpStatusCode.InternalServerError
            //                ? ErrorLevel.Warning
            //                : ErrorLevel.Error;

            //            var messageTxt =
            //                $"HttpError {errorResponse.StatusCode} : {errorResponse.ErrorCode} : {errorResponse.Message}";
            //            var loggableMessage = new Message(messageTxt, errorResponse.ToSafePartialObjectDictionary());
            //            loggableBody = new Body(loggableMessage);
            //        }
            //        else if (responseMessage is Exception ex)
            //        {
            //            loggableBody = new Body(ex);
            //            messageErrorLevel = ErrorLevel.Critical;
            //        }
            //    }
            //    else
            //    {
            //        messageErrorLevel = ErrorLevel.Error;
            //        var loggableMessage = new Message("Error detected but error tracking currently disabled.");
            //        loggableBody = new Body(loggableMessage);
            //    }
            //}

            //var data = new Data(
            //    elasticSearchConfig,
            //    loggableBody
            //)
            //{
            //    Context = request?.GetType().Name,
            //    Request = loggableRequest,
            //    Level = messageErrorLevel,
            //    Person = loggablePerson
            //};

            //var elasticSearchLogRequest = new ElasticSearchLogRequest
            //{
            //    AccessToken = _plugin.ApiKey,
            //    Data = data
            //};

            //if (AppendProperties != null)
            //    foreach (var kvPair in AppendProperties
            //        ?.Invoke(request, requestMessage, responseMessage, requestDuration).Safe())
            //        elasticSearchLogRequest.Data.Custom.Add(kvPair.Key, kvPair.Value);
            //return elasticSearchLogRequest;
        }

        /// <summary>
        /// Input: request, requestDto, response, requestDuration
        /// Output: List of Properties to append to Seq Log entry
        /// </summary>
        public ElasticSearchLoggerPlugin.PropertyAppender AppendProperties
        {
            get => _plugin.AppendProperties;
            set => _plugin.AppendProperties = value;
        }
    }
}