using System.Net;
using System.Net.Http.Headers;
using FastEndpoints.Security;
using NcpAdminBlazor.Domain.AggregatesModel.UserAggregate;
using NcpAdminBlazor.ApiService.Application.Queries.UsersManagement;
using NcpAdminBlazor.ApiService.Endpoints.Authentication;
using NcpAdminBlazor.ApiService.Endpoints.UsersManagement;
using NcpAdminBlazor.ApiService.Tests.Fixtures;

namespace NcpAdminBlazor.ApiService.Tests;

[Collection(WebAppTestCollection.Name)]
public class UsersAuthTests(WebAppFixture app, UsersAuthTests.UserState state)
    : TestBase<WebAppFixture, UsersAuthTests.UserState>
{
    [Fact, Priority(1)]
    public async Task RegisterUser_ShouldReturn200_AndUserId()
    {
        // Act
        var (rsp, res) = await app.Client
            .POSTAsync<RegisterUserEndpoint, RegisterUserRequest, ResponseData<RegisterUserResponse>>(
                new RegisterUserRequest(UserState.Username, UserState.Password));
        // Assert
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Data.UserId.Id.ShouldNotBe(Guid.Empty);
        state.RegisteredUserId = res.Data.UserId;
    }

    [Fact, Priority(2)]
    public async Task Login_ShouldReturn_TokenEnvelope()
    {
        // Act: 登录
        var (rsp, res) = await app.Client
            .POSTAsync<LoginEndpoint, LoginRequest, ResponseData<TokenResponse>>(
                new LoginRequest(UserState.Username, UserState.Password));

        // Assert
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Success.ShouldBeTrue();
        res.Data.UserId.ShouldNotBeNullOrEmpty();
        res.Data.AccessToken.ShouldNotBeNullOrWhiteSpace();
        res.Data.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        state.Token = res.Data;
    }

    [Fact, Priority(3)]
    public async Task RefreshToken_ShouldIssue_NewAccessToken()
    {
        var token = state.Token ??
                    throw new InvalidOperationException(
                        "Token is null, ensure that Login_ShouldReturn_TokenEnvelope runs before this test.");
        // Act: 刷新令牌（端点允许匿名访问）
        var (rsp, res) = await app.Client
            .POSTAsync<RefreshEndpoint, TokenRequest, ResponseData<TokenResponse>>(
                new TokenRequest { UserId = token.UserId, RefreshToken = token.RefreshToken });

        // Assert
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Success.ShouldBeTrue();
        res.Data.AccessToken.ShouldNotBeNullOrWhiteSpace();
        res.Data.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }
    
    [Fact, Priority(4)]
    public async Task Profile_ShouldReturn_CurrentUserPayload()
    {
        // Arrange
        var token = state.Token?.AccessToken ?? throw new InvalidOperationException(
            "Token is null, ensure that Login_ShouldReturn_TokenEnvelope runs before this test.");

        app.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var (rsp, res) = await app.Client
            .GETAsync<ProfileEndpoint, ResponseData<UserInfoDto>>();

        // Assert
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Success.ShouldBeTrue();
    res.Data.Username.ShouldBe(UserState.Username);
    }
    
    [Fact, Priority(5)]
    public async Task UserInfo_ShouldReturn_DetailedProfile()
    {
        var userId = state.RegisteredUserId ?? throw new InvalidOperationException("UserId not initialized");

        // Act
        var (rsp, res) = await app.Client
            .GETAsync<UserInfoEndpoint, UserInfoRequest, ResponseData<UserInfoDto>>(
                new UserInfoRequest { UserId = userId });

        // Assert
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Success.ShouldBeTrue();
        res.Data.Id.ShouldBe(userId);
    res.Data.Username.ShouldBe(UserState.Username);
    }

    /// <summary>
    /// use for sharing state between UsersAuthTests
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class UserState : StateFixture
    {
        public const string Username = "tesUser_";
        public const string Password = "Test@1234";

        public TokenResponse? Token;
        public UserId? RegisteredUserId;

        protected override async ValueTask SetupAsync()
        {
            await ValueTask.CompletedTask;
        }

        protected override async ValueTask TearDownAsync()
        {
            await ValueTask.CompletedTask;
        }
    }
}