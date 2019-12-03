using System;
using System.IO;
using System.Net.Mqtt;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace CTS
{
    public class GraphQLClient {
        private string httpEndpoint;
        private HttpClient httpClient;
        public GraphQLClient(Config config, string apiToken) {
            httpEndpoint = config.httpEndpoint;
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("authorization", apiToken);
            httpClient.DefaultRequestHeaders.Add("x-api-key", config.apiKey);
        }

        public async Task<dynamic> PostQuery(GraphQLQuery query) {
            HttpContent httpContent = new StringContent(query.getPayload(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, httpContent);
            response.EnsureSuccessStatusCode();
            Stream stream = await response.Content.ReadAsStreamAsync();
            return DeserializeJsonFromStream<dynamic>(stream);
        }

        public async Task<SubscriptionHttpResponseModel> AddSubscription(GraphQLQuery query, Action callbackMethod) {
            HttpContent httpContent = new StringContent(query.getPayload(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, httpContent);

            response.EnsureSuccessStatusCode();

            Stream stream = await response.Content.ReadAsStreamAsync();
            SubscriptionHttpResponseModel result = DeserializeJsonFromStream<SubscriptionHttpResponseModel>(stream);
            string wsEndpoint = result.extensions.subscription.mqttConnections[0].url;
            string topic = result.extensions.subscription.mqttConnections[0].topics[0];
            Console.WriteLine(wsEndpoint);
            return result;
        }

        private static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default(T);

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                var searchResult = js.Deserialize<T>(jtr);
                return searchResult;
            }
        }

        private static async Task<string> StreamToStringAsync(Stream stream)
        {
            string content = null;

            if (stream != null)
                using (var sr = new StreamReader(stream))
                    content = await sr.ReadToEndAsync();

            return content;
        }

        public class SubscriptionHttpResponseModel {
            public Extensions extensions;
            public Dictionary<string, string> data;

            public class Extensions {
                public Subscription subscription;

                public class Subscription {
                    public List<MqttConnections> mqttConnections;
                    public Dictionary<string, Dictionary<string, string>> newSubscriptions;

                    public class MqttConnections {
                        public string url;
                        public List<string> topics;
                        public string client;
                    }
                }
            }
        }
    }

    public class GraphQLQuery {
        Payload payload;
        public GraphQLQuery(string query) {
            payload = new Payload();
            payload.query = query;
        }

        public GraphQLQuery(string query, string operation, string variables) {
            payload = new Payload();
            payload.query = query;
            payload.operation = operation;
            payload.variables = variables;
        }

        public string getPayload() {
            return JsonConvert.SerializeObject(payload);
        }

        class Payload {
            public string query;
            public string operation = null;
            public string variables = null;
        }
    }

    public class Config {
        public string httpEndpoint;
        public string apiKey;
        public string userId;
        public string userPass;
        public string userPoolId;
        public string userPoolClientId;
        public string identityPoolId;
    }
}