using System;
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
            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer");
            httpClient.DefaultRequestHeaders.Add("authorization", apiToken);
            httpClient.DefaultRequestHeaders.Add("x-api-key", config.apiKey);
        }

        public async Task<string> PostQuery(GraphQLQuery query) {
            HttpContent content = new StringContent(query.getPayload(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, content);
            return response.Content.ReadAsStringAsync().Result;
        }

        public async void AddSubscription(string queryString, Action callbackMethod) {
            HttpContent content = new StringContent("{\"query\":" + queryString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, content);  
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