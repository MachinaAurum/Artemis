namespace MachinaAurum.Artemis.Http
{
    public class HttpRequestData
    {
        public string Method { get; set; }
        public string Uri { get; set; }
        public string Version { get; set; }
        public string Host { get; set; }
        public long? ContentLength { get; set; }
    }
}
