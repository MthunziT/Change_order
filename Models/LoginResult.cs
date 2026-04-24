// Models/LoginResult.cs
namespace Change_order.Models
{
    public class LoginResult
    {
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string Role { get; set; }  // RoleName from the SP
    }
}