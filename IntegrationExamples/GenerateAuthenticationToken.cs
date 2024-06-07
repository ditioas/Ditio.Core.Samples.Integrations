using IdentityModel.Client; //https://github.com/IdentityModel
using System.Net.Http.Headers;

namespace IntegrationExamples;

public static class GenerateAuthenticationToken
{
    private static TokenResponse existingTokenResponse = null;
    private static DateTime? existingTokenResponseExpires = null;
    private static readonly string identityBaseUrl = "https://ditio-ids-we-test.azurewebsites.net";
    private static readonly string apiBaseUrl = "https://ditio-api-test.azurewebsites.net";
    private static readonly string clientId = "";
    private static readonly string clientSecret = "";
    private static readonly string scope = "";

    private static readonly string companyId = "";

    public static async Task<string> GetToken()
    {
        if (existingTokenResponse != null && existingTokenResponseExpires > DateTime.UtcNow.AddHours(1))
        {
            return existingTokenResponse.AccessToken;
        }

        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = $"{identityBaseUrl}/connect/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scope = scope,
        });

        Console.WriteLine(response.Json);
        existingTokenResponse = response;
        existingTokenResponseExpires = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

        if (!string.IsNullOrEmpty(response.AccessToken))
        {
            Console.WriteLine("Authentication succeeded");
            Console.WriteLine($"Token is valid until: {DateTime.Now.AddSeconds(response.ExpiresIn)}");
            Console.WriteLine("-------------- Token is value bellow this line ---------->");
            Console.WriteLine(response.AccessToken);
            Console.WriteLine("<------------- and above this line -----------------------");
        }
        else
        {
            Console.WriteLine("Authentication failed!");
            throw new Exception(
                $"Authentication failed! Response: {response.Json}. Please check your parameters and try again");
        }

        return response.AccessToken;
    }
}