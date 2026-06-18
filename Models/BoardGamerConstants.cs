namespace BoardGamerApp.Models;

public static class BoardGamerConstants
{
    public static class GroupRoles
    {
        public const string Owner = "owner";
        public const string Admin = "admin";
        public const string Member = "member";
    }

    public static class GroupMemberStatus
    {
        public const string Active = "active";
        public const string Invited = "invited";
        public const string Left = "left";
        public const string Removed = "removed";
    }

    public static class GameNightStatus
    {
        public const string Planned = "planned";
        public const string Cancelled = "cancelled";
        public const string Completed = "completed";
    }

    public static class AttendanceStatus
    {
        public const string Accepted = "accepted";
        public const string Declined = "declined";
        public const string Maybe = "maybe";
    }

    public static class SyncOperations
    {
        public const string Insert = "INSERT";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
    }
}