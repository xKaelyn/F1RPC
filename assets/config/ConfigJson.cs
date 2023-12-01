using Newtonsoft.Json;

namespace F1RPC.Configuration
{
    public struct ConfigJson : IEquatable<ConfigJson>
    {
        [JsonProperty("appid")]
        public string AppId { get; private set; }

        [JsonProperty("port")]
        public int Port { get; private set; }

        [JsonProperty("webhook_url")]
        public string WebhookUrl { get; private set; }

        public bool Equals(ConfigJson other)
        {
            throw new NotImplementedException();
        }
    }
}
