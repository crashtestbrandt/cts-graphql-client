using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client;

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

        public async Task<bool> AddSubscription(GraphQLQuery query, Action callbackMethod) {
            HttpContent httpContent = new StringContent(query.getPayload(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, httpContent);

            response.EnsureSuccessStatusCode();

            Stream stream = await response.Content.ReadAsStreamAsync();
            SubscriptionHttpResponseModel result = DeserializeJsonFromStream<SubscriptionHttpResponseModel>(stream);
            Console.WriteLine(JsonConvert.SerializeObject(result));

            Uri wsUri = null;
            string clientId = null;
            string topic = result.extensions.subscription.newSubscriptions["onUpdateCharacter"]["topic"];
            foreach (var conn in result.extensions.subscription.mqttConnections) {
                if (wsUri == null) {
                    foreach(var t in conn.topics) {
                        if (String.Equals(topic, t)) {
                            wsUri = new Uri(conn.url);
                            clientId = conn.client;
                            Console.WriteLine("Topic == " + t);
                            break;
                        } else {
                            Console.WriteLine("Topic != " + t);
                        }
                    }
                }
            }
            if (wsUri == null) Console.WriteLine("wsUri still null");

            string wsEndpoint = wsUri.Host + wsUri.PathAndQuery + wsUri.Fragment;

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithWebSocketServer(wsEndpoint)
                .WithTls()
                .WithClientId(clientId)
                .Build();
            Console.WriteLine(JsonConvert.SerializeObject(options));
            MQTTnet.Client.Connecting.MqttClientAuthenticateResult r0 = mqttClient.ConnectAsync(options, CancellationToken.None).Result;
            Console.WriteLine("MQTT Connect: " + r0.ResultCode.ToString());

            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Received message:");
                Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                Console.WriteLine();
                Console.ResetColor();
            });

            MqttClientSubscribeResult r1 = mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build()).Result;
            Console.WriteLine("MQTT Subscription: " + r1.Items.ToString());
            while (true) {}


/*
            mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("Connected with server!");
                await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
                Console.WriteLine("Subscribed!");

            });
*/
            return true;

            // TODO: Return something about the callback?
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