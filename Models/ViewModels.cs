using System.ComponentModel.DataAnnotations;

namespace Change_order.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class DashboardViewModel
    {
        public List<ChangeRequest> AllRequests { get; set; } = new();
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int DeployedCount { get; set; }
        public ApplicationUser? CurrentUser { get; set; }
    }

    public class ApprovalViewModel
    {
        public int ChangeRequestId { get; set; }
        public string CRId { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
