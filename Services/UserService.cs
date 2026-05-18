using Change_order.Models;
using Microsoft.Data.SqlClient;

namespace Change_order.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserAsync(string windowsUsername);
        Task<string?> GetSignatureAsync(string windowsUsername);  // ← NEW
    }

    public class UserService : IUserService
    {
        private readonly string _connString =
            "Server=168.89.27.118;Database=UserManagement;User ID=sa;Password=Code@007;TrustServerCertificate=true;";

        private const string SystemId = "10";

        public async Task<ApplicationUser?> GetUserAsync(string windowsUsername)
        {
            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.Login", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", windowsUsername);
                cmd.Parameters.AddWithValue("@System", SystemId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var roleName = reader["Role"]?.ToString() ?? "";
                    var firstName = reader["FirstName"]?.ToString() ?? "";
                    var surname = reader["Surname"]?.ToString() ?? "";
                    var department = reader["Department"]?.ToString() ?? "";

                    return new ApplicationUser
                    {
                        WindowsUsername = windowsUsername,
                        FullName = $"{firstName} {surname}".Trim(),
                        Email = reader["EmailAddress"]?.ToString() ?? "",
                        Department = department,
                        Role = MapRole(roleName)
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ── NEW: fetch signature from UserManagement.dbo.Users ────────────
        public async Task<string?> GetSignatureAsync(string windowsUsername)
        {
            try
            {
                await using var conn = new SqlConnection(_connString);
                await conn.OpenAsync();

                // Username column in Users table stores the Windows login
                await using var cmd = new SqlCommand(
                    "SELECT Signature FROM [dbo].[Users] WHERE Username = @u", conn);
                cmd.Parameters.AddWithValue("@u", windowsUsername);

                var result = await cmd.ExecuteScalarAsync();
                var sig = result?.ToString();

                // Return null if empty or just whitespace
                return string.IsNullOrWhiteSpace(sig) ? null : sig;
            }
            catch
            {
                return null;
            }
        }

        private static UserRole MapRole(string roleName) => roleName.ToLower() switch
        {
            "admin" => UserRole.Admin,
            "manager1" => UserRole.Manager1,
            "manager2" => UserRole.Manager2,
            "developer" => UserRole.Developer,
            _ => UserRole.Developer
        };
    }
}