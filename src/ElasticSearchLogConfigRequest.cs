namespace ServiceStack.ElasticSearchPlugin
{
    [Route("/ElasticSearchLogConfig")]
    public class ElasticSearchLogConfigRequest : IReturn<ElasticSearchLogConfigRequest>
    {
        public bool? Enabled { get; set; }

        public bool? EnableSessionTracking { get; set; }

        public bool? EnableRequestBodyTracking { get; set; }

        public bool? EnableResponseTracking { get; set; }

        public bool? EnableErrorTracking { get; set; }
    }
}