using VetLims.Infrastructure.Identity;

namespace VetLims.Infrastructure.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public Permission? Permission { get; set; }
}

public class UserRefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt == null && !IsExpired;
}

public class LoginHistory : BaseEntity
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public bool Success { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? FailureReason { get; set; }
}

public class UserActivityLog : BaseEntity
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class MenuPermission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ScreenPermission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MenuPermissionId { get; set; }
    public MenuPermission? MenuPermission { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ButtonPermission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ScreenPermissionId { get; set; }
    public ScreenPermission? ScreenPermission { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
