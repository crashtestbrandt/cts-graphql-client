using System;
using System.Collections.Generic;
using GraphQL;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://[apiurl.com]/[graphqlendpoint]";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer: [TOKEN]");
            SubClient subClient = new SubClient(url, headers);        }
    }
}