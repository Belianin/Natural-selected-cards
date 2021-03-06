using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NaturalSelectedCards.Data.Entities;
using NaturalSelectedCards.Data.Repositories;
using NaturalSelectedCards.Utils;
using NaturalSelectedCards.Utils.Constants;
using NaturalSelectedCards.Utils.Constants.ClaimTypes;
using NaturalSelectedCards.Utils.Extensions;

namespace NaturalSelectedCards.Auth
{
    public class GoogleAuthenticationHandler
        : AuthenticationHandler<GoogleAuthenticationSchemeOptions>
    {
        private readonly IUserRepository users;
        public GoogleAuthenticationHandler(
            IOptionsMonitor<GoogleAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IUserRepository users)
            : base(options, logger, encoder, clock)
        {
            this.users = users;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            using var client = new HttpClient();

            if (Request.Cookies.TryGetValue(CookieKeys.AuthorizationToken, out var token))
                return await AuthenticateWithToken(client, token);

            if (Request.Cookies.TryGetValue(CookieKeys.AuthorizationRefreshToken, out var refreshToken))
                return await AuthenticateWithRefresh(client, refreshToken);

            return AuthenticateResult.Fail("No auth or refresh token");
        }

        private async Task<AuthenticateResult> AuthenticateWithRefresh(HttpClient client, string refreshToken)
        {
            var refreshResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                ClientId = EnvironmentVariables.ClientId,
                ClientSecret = EnvironmentVariables.ClientSecret,

                Address = Urls.TokenAddress,
                RefreshToken = refreshToken
            }).ConfigureAwait(false);

            if (refreshResponse.IsError)
                return AuthenticateResult.Fail($"Google refresh fail: {refreshResponse.Error}");

            Response.SetTokenCookies(refreshResponse);

            return await AuthenticateWithToken(client, refreshResponse.AccessToken);
        }

        private async Task<AuthenticateResult> AuthenticateWithToken(HttpClient client, string token)
        {
            var userInfo = await client.GetUserInfoAsync(new UserInfoRequest
            {
                Address = Urls.UserInfoAddress,
                Token = token
            }).ConfigureAwait(false);

            if (userInfo.IsError)
                return AuthenticateResult.Fail($"Google auth fail: {userInfo.Error}");
            return await AuthenticateWithUserInfo(userInfo).ConfigureAwait(false);
        }

        private async Task<AuthenticateResult> AuthenticateWithUserInfo(UserInfoResponse userInfo)
        {
            var googleUserId = userInfo.Claims.GetValueByType(GoogleClaimTypes.Sub);
            if (googleUserId == null)
                return AuthenticateResult.Fail("No sub claim");

            var user = await GetOrCreateUserAsync(googleUserId).ConfigureAwait(false);

            var name = userInfo.Claims.GetValueByType(GoogleClaimTypes.FirstName) ?? "";
            var surname = userInfo.Claims.GetValueByType(GoogleClaimTypes.LastName) ?? "";
            var photo = userInfo.Claims.GetValueByType(GoogleClaimTypes.Picture) ?? "";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Surname, surname),
                new Claim(CustomClaimTypes.Photo, photo)
            };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(GoogleAuthenticationHandler));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        private async Task<UserEntity> GetOrCreateUserAsync(string googleUserId)
        {
            var user = await users.FindByGoogleIdAsync(googleUserId).ConfigureAwait(false);
            if (user != null)
                return user;
            
            return await users.InsertAsync(new UserEntity(googleUserId)).ConfigureAwait(false);
        }
    }
}