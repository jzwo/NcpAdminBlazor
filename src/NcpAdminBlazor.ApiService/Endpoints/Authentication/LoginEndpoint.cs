using System.Security.Claims;
using FastEndpoints;
using NcpAdminBlazor.ApiService.Application.Commands.UsersManagement;
using NcpAdminBlazor.ApiService.Application.Queries.UsersManagement;
using NcpAdminBlazor.ApiService.Auth;

namespace NcpAdminBlazor.ApiService.Endpoints.Authentication;

public record LoginRequest(string Username, string Password);

public class LoginEndpoint(IMediator mediator)
    : Endpoint<LoginRequest, ResponseData<MyTokenResponse>>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        Description(x => x.WithTags("Authentication"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var userId = await mediator.Send(new GetUserIdByNameQuery(req.Username), ct);
        if (userId == null) throw new KnownException("用户名或密码错误");

        await mediator.Send(new LoginUserCommand(userId, req.Password), ct);

        var tokenService = Resolve<UserTokenService>();
        var envelope = await tokenService.CreateCustomToken(
            userId.ToString(),
            privileges: privileges =>
            {
                privileges.Claims.AddRange([
                    new Claim("ClientID", "Default"),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, req.Username)
                ]);
            },
            map: tr => tr.AsResponseData()
        );
        await Send.OkAsync(envelope, ct);
    }
}

public sealed class LoginSummary : Summary<LoginEndpoint, LoginRequest>
{
    public LoginSummary()
    {
        Summary = "用户登录";
        Description = "Description text goes here...";
    }
}