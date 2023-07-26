using BlazorTemplateWithAuthentication.Shared;

namespace BlazorTemplateWithAuthentication.Client.Authentication
{
    public interface IAuthService
    {
        Task<SignUpResult> SignUp(SignUp model);
        Task<LoginResult> Login(Login model);
        Task Logout();

    }
}
