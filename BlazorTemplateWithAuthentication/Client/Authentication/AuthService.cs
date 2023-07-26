using Blazored.LocalStorage;
using BlazorTemplateWithAuthentication.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using BlazorTemplateWithAuthentication.Client.Helpers;

namespace BlazorTemplateWithAuthentication.Client.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthService(HttpClient httpClient,
                           ILocalStorageService localStorage,
                           AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<SignUpResult> SignUp(SignUp model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/accounts/signup", model);
            var signUpString = await response.Content.ReadAsStringAsync();
            var signUpResult = JsonSerializer.Deserialize<SignUpResult>(signUpString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            /*if (!response.IsSuccessStatusCode)
            {
                return new SignUpResult { Successful = true, Errors = null };
            }
            return new SignUpResult { Successful = false, Errors = new List<string> { "Error occured" } };*/
            return signUpResult;
        }

        public async Task<LoginResult> Login(Login model)
        {
            //var loginAsJson = JsonSerializer.Serialize(model);
            //var response = await _httpClient.PostAsync("api/accounts/login", new StringContent(loginAsJson, Encoding.UTF8, "application/json"));
            var response = await _httpClient.PostAsJsonAsync("api/accounts/login", model);
            var loginString = await response.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResult>(loginString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!response.IsSuccessStatusCode)
            {
                return loginResult!;
            }

            await _localStorage.SetItemAsync("authToken", loginResult.Token);
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(model.Email!);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", loginResult.Token);

            return loginResult;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
