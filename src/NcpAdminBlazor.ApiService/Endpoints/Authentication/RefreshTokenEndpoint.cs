using FastEndpoints;
using FastEndpoints.Security;
using NcpAdminBlazor.ApiService.Auth;

namespace NcpAdminBlazor.ApiService.Endpoints.Authentication;

public class RefreshEndpoint : Endpoint<TokenRequest, ResponseData<MyTokenResponse>>
{
    public override void Configure()
    {
        Post("/api/auth/refresh-token");
        Description(x => x.WithTags("Authentication"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(TokenRequest req, CancellationToken ct)
    {
        var svc = Resolve<UserTokenService>();
        var envelope = await svc.CreateCustomToken<ResponseData<MyTokenResponse>>(
            userId: req.UserId,
            privileges: _ => { },
            map: tr => tr.AsResponseData(),
            isRenewal: true,
            request: req
        );

        await Send.OkAsync(envelope, ct);
    }
}