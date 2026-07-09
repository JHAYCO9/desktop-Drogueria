using System;

namespace DrogueriaMickey.Desktop.Services
{
    public class SessionService
    {
        public Guid? UserId { get; private set; }
        public string? Username { get; private set; }
        public Guid? RoleId { get; private set; }
        public string? RoleName { get; private set; }
        public string? Token { get; private set; }
        public bool IsOfflineSession { get; private set; }

        public bool IsAdministrator => RoleName == "Administrador";

        public void SetSession(Guid userId, string username, Guid roleId, string? roleName, string? token, bool isOffline)
        {
            UserId = userId;
            Username = username;
            RoleId = roleId;
            RoleName = roleName;
            Token = token;
            IsOfflineSession = isOffline;
        }

        public void Clear()
        {
            UserId = null; Username = null; RoleId = null; RoleName = null; Token = null; IsOfflineSession = false;
        }
    }
}