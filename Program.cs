using System;
using System.IO;
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

            // TODO: Block elsewhere
            while (credsManager.getAccessToken() == null) {
                // nop
            }

            GraphQLClient graphQLClient = new GraphQLClient(config, credsManager.getAccessToken());
            /*
            string listCharactersQuery = @"query listCharacters {
                listCharacters {
                    items {
                    id
                    localPosition {
                        x
                        y
                        z
                    }
                    localRotation {
                        w
                        x
                        y
                        z
                    }
                    parentId
                    psgUserId
                    items
                    eventFlow
                    avatar
                    status
                    ship
                    }
                }
            }";
            */
            string onUpdateCharacterQuery = @"subscription OnUpdateCharacter(
            $id: ID
            ) {
            onUpdateCharacter(
                id: $id
            ) {
                id
                localPosition {
                x
                y
                z
                }
                localRotation {
                w
                x
                y
                z
                }
                parentId
                psgUserId
                items
                eventFlow
                avatar
                status
                ship
            }
            }";
            string onUpdateCharacterVars = "{\"id\":\"c969ec45-08ee-47ff-bab3-6f9c57dbde67\"}";
            GraphQLQuery query = new GraphQLQuery(onUpdateCharacterQuery, "OnUpdateCharacter", onUpdateCharacterVars);

            var content = graphQLClient.AddSubscription(query, null).Result;
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