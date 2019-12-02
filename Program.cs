using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using CTS;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = LoadConfig();
            CredsManager credsManager = new CredsManager(config);
            while (credsManager.getAccessToken() == null) {
                // nop
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("authorization", credsManager.getAccessToken());
            headers.Add("x-api-key", config.apiKey);
            GraphQLClient graphQLClient = new GraphQLClient(config.httpEndpoint, headers);
        }

        static Config LoadConfig()
        {
            Config config;
            using (StreamReader r = new StreamReader("config.json"))
            {
                string json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<Config>(json);
            }
            return config;
        }
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

public class CredsManager {
    private CognitoAWSCredentials credentials = null;
    private string accessToken = null;

    public CredsManager(Config config) {
        GetCredsAsync(config);
    }

    public string getAccessToken() => accessToken;

    private async void GetCredsAsync(Config config) {
        AmazonCognitoIdentityProviderClient provider =
            new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), RegionEndpoint.USEast2);
        CognitoUserPool userPool = new CognitoUserPool(config.userPoolId, config.userPoolClientId, provider);
        CognitoUser user = new CognitoUser(config.userId, config.userPoolClientId, userPool, provider);
        InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest() {
            Password = config.userPass
        };

        AuthFlowResponse context = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);

        if (context.AuthenticationResult != null) Console.WriteLine("Authentication success");
        else Console.WriteLine("Failed PSG authentication!");

        credentials = user.GetCognitoAWSCredentials(config.identityPoolId, RegionEndpoint.USEast2);
        if (credentials != null) Console.WriteLine("Acquired user credentials");
        else Console.WriteLine("Failed to acquire user credentials!");

        accessToken = context.AuthenticationResult.AccessToken;
    }
}