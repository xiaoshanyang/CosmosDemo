namespace todo.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System.Web;

    public class Item
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "isComplete")]
        public bool Completed { get; set; }

        [JsonProperty(PropertyName = "FileName")]
        public HttpPostedFileBase FileName { get; set; }
    }
}