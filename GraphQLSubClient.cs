using System;
using System.Net.Mqtt;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace GraphQL
{
    public class SubClient {

        private string apiUrl;
        private HttpClient httpClient;

        public SubClient(string _apiUrl, Dictionary<string, string> headers) {
            apiUrl = _apiUrl;
            httpClient = new HttpClient();
            foreach (KeyValuePair<string, string> header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async void AddSubscription(string queryString, Action callbackMethod) {
            HttpContent content = new StringContent("{\"query\":" + queryString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
            
        }


    }
}