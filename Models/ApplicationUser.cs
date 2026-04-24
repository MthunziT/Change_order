namespace Change_order.Models
{
        public class ApplicationUser
        {
            public string WindowsUsername { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public UserRole Role { get; set; } = UserRole.Developer;
            public string Department { get; set; } = string.Empty;
        }

        public enum UserRole
        {
            Developer,
            Manager1,
            Manager2,
            Admin
        }
}
     public class UserManagementLoginResult
     {
    public int UserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
     }