using FastEndpoints;
using NcpAdminBlazor.ApiService.Application.Queries.UsersManagement;
using NcpAdminBlazor.ApiService.Auth;

namespace NcpAdminBlazor.ApiService.Endpoints.UsersManagement;

public class ProfileEndpoint(IMediator mediator, ICurrentUser currentUser)
    : EndpointWithoutRequest<ResponseData<UserInfoDto>>
{
    public override void Configure()
    {
        Get("/api/user/profile");
        Description(x => x.WithTags("User"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new KnownException("not logged in");
        var dto = await mediator.Send(new GetUserInfoQuery(userId), ct);
        await Send.OkAsync(dto.AsResponseData(), ct);
    }
}

public sealed class ProfileSummary : Summary<ProfileEndpoint>
{
    public ProfileSummary()
    {
        Summary = "获取当前用户信息";
        Description = "返回当前登录用户的详细资料";
    }
}