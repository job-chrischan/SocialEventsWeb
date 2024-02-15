using Microsoft.Graph;
using Azure.Identity;


namespace SocialEventsWeb.Services
{
    public class AuthenticationService
    {
        private readonly IConfiguration _configuration;
        public AuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private GraphServiceClient _graphClient;

        public AuthenticationService()
        {
            var scopes = new[] { "User.Read" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = _configuration["PrivateSettings:ida:TenantID"];

            // Value from app registration
            var clientId = _configuration["PrivateSettings:ida:ClientID"];

            // using Azure.Identity;
            var options = new DeviceCodeCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                ClientId = clientId,
                TenantId = tenantId,
                // Callback function that receives the user prompt
                // Prompt contains the generated device code that user must
                // enter during the auth process in the browser
                DeviceCodeCallback = (code, cancellation) =>
                {
                    Console.WriteLine(code.Message);
                    return Task.FromResult(0);
                },
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential
            var deviceCodeCredential = new DeviceCodeCredential(options);

            _graphClient = new GraphServiceClient(deviceCodeCredential, scopes);
        }
        public void InitiateGraphServiceClient()
        {
            //return null;
        }
    }













    //public void CreateGraphServiceClient()
    //    {
    //        var connectionString = Configuration["PrivateSettings:ida:AppID"];
    //        var apiKey = Configuration["PrivateSettings:ApiKey"];

    //        "PrivateSettings": {
    //            "ida:AppID": "privateSettings:ClientAppID",
    //"ida:AppSecret": "privateSettings:SecretKey",
    //"ida:RedirectUri": "privateSettings:RedirectUri",
    //"ida:AppScopes": "privateSettings:Scopes"

    //        var scopes = new[] { "User.Read" };

    //        // Multi-tenant apps can use "common",
    //        // single-tenant apps must use the tenant ID from the Azure portal
    //        var tenantId = "common";

    //        // Value from app registration
    //        var clientId = "YOUR_CLIENT_ID";

    //        // using Azure.Identity;
    //        var options = new DeviceCodeCredentialOptions
    //        {
    //            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
    //            ClientId = clientId,
    //            TenantId = tenantId,
    //            // Callback function that receives the user prompt
    //            // Prompt contains the generated device code that user must
    //            // enter during the auth process in the browser
    //            DeviceCodeCallback = (code, cancellation) =>
    //            {
    //                Console.WriteLine(code.Message);
    //                return Task.FromResult(0);
    //            },
    //        };

    //        // https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential
    //        var deviceCodeCredential = new DeviceCodeCredential(options);

    //        var graphClient = new GraphServiceClient(deviceCodeCredential, scopes);
    //    }


    //}
}





