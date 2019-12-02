using System;
using System.Net.Mqtt;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace CTS
{
    public class GraphQLClient {
        private string httpEndpoint;
        private HttpClient httpClient;
        public GraphQLClient(string _httpEndpoint, Dictionary<string, string> headers) {
            httpEndpoint = _httpEndpoint;
            httpClient = new HttpClient();
            foreach (KeyValuePair<string, string> header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async void PostQuery(string queryString) {
            HttpContent content = new StringContent("{\"query\":" + queryString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, content);
            Console.WriteLine(response.Content);
        }

        public async void AddSubscription(string queryString, Action callbackMethod) {
            HttpContent content = new StringContent("{\"query\":" + queryString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(httpEndpoint, content);  
        }
    }
}