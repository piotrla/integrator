using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace kanc_integrator
{
    public class AuthRepo : IAuthenticationProvider
    {
        private readonly IConfidentialClientApplication msalClient;
        private readonly string[] scopes;
        public AuthRepo(AuthenticationConfig config)
        {
            scopes = new string[] { config.Scopes };

            msalClient = ConfidentialClientApplicationBuilder
                                        .Create(config.ClientId)
                                        .WithClientSecret((config.ClientSecret))
                                        .WithTenantId(config.Tenant)
                                        .Build();

        }

        public async Task<Dictionary<string, string>> GetAccessTokenAsync()
        {
            try
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                var result = await msalClient.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                dic.Add("Success", result.AccessToken);
                return dic;
            }
            catch (Exception exception)
            {
                return new Dictionary<string, string>() { { "Error", exception.Message } };
            }

        }
        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage)
        {
            Dictionary<string, string> dicAccessToken = await GetAccessTokenAsync();
            if (dicAccessToken.First().Key == "Success")
            {
                requestMessage.Headers.Authorization =
                               new AuthenticationHeaderValue("bearer", dicAccessToken.First().Value);
            }
            else
            {
                Console.WriteLine($"GetAccessToken error, {dicAccessToken.First().Value}");
            }
        }
    }

}