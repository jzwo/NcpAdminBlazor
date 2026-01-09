using NcpAdminBlazor.Domain.AggregatesModel.RoleAggregate;
using NcpAdminBlazor.Domain.Common;
using NcpAdminBlazor.Domain.DomainEvents;

namespace NcpAdminBlazor.Domain.AggregatesModel.UserAggregate
{
    public partial record UserId : IGuidStronglyTypedId;

    public class User : Entity<UserId>, IAggregateRoot, ISoftDeletable
    {
        protected User()
        {
        }

        public string Username { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string RealName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public ICollection<RoleId> AssignedRoleIds { get; private set; } = [];
        public string RefreshToken { get; private set; } = string.Empty;
        public DateTimeOffset RefreshExpiry { get; private set; } = DateTimeOffset.MinValue;
        public DateTimeOffset CreatedAt { get; init; }
        public Deleted IsDeleted { get; private set; } = false;
        public DeletedTime DeletedAt { get; private set; } = new(DateTimeOffset.MinValue);

        public User(
            string username,
            string passwordHash,
            string realName,
            string email,
            string phone,
            ICollection<RoleId> assignedRoleIds)
        {
            CreatedAt = DateTimeOffset.UtcNow;
            Username = username;
            RealName = realName;
            Email = email;
            Phone = phone;
            PasswordHash = passwordHash;
            AssignedRoleIds = assignedRoleIds;
            AddDomainEvent(new UserCreatedDomainEvent(this));
        }

        public void UpdateInfo(string username, string realName, string email, string phone,
            ICollection<RoleId> assignedRoleIds)
        {
            Username = username;
            RealName = realName;
            Email = email;
            Phone = phone;
            AssignedRoleIds = assignedRoleIds;
            AddDomainEvent(new UserInfoUpdatedDomainEvent(this));
        }

        public void ChangePassword(string newPasswordHash)
        {
            if (PasswordHash == newPasswordHash)
                throw new KnownException("新密码不能与旧密码相同");
            PasswordHash = newPasswordHash;
            AddDomainEvent(new UserPasswordChangedDomainEvent(this));
        }

        public void Login()
        {
            AddDomainEvent(new UserLoginDomainEvent(this));
        }

        public void SetRefreshToken(string refreshToken, DateTimeOffset refreshExpiry)
        {
            RefreshToken = refreshToken;
            RefreshExpiry = refreshExpiry;
        }

        public void Delete()
        {
            if (IsDeleted) throw new KnownException("用户已经被删除！");
            IsDeleted = true;
            AddDomainEvent(new UserDeletedDomainEvent(this));
        }
    }
}