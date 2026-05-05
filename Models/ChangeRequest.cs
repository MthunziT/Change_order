using System.ComponentModel.DataAnnotations;

namespace Change_order.Models
{
    public class ChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string CRId { get; set; } = string.Empty; // e.g. CON001

        [Required]
        [StringLength(200)]
        [Display(Name = "Change Request Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Application / System")]
        public string ApplicationName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Business Justification")]
        public string BusinessJustification { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Priority")]
        public Priority Priority { get; set; }

        [Required]
        [Display(Name = "Category")]
        public ChangeCategory Category { get; set; }

        [Required]
        [Display(Name = "Impact")]
        public ImpactLevel Impact { get; set; }

        [StringLength(500)]
        [Display(Name = "Impact Description")]
        public string? ImpactDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "Rollback Plan")]
        public string? RollbackPlan { get; set; }

        [StringLength(500)]
        [Display(Name = "Test Plan")]
        public string? TestPlan { get; set; }

        [Required]
        [Display(Name = "Planned Deployment Date")]
        [DataType(DataType.Date)]
        public DateTime DeploymentDate { get; set; }

        [Display(Name = "Deployment Window")]
        [StringLength(100)]
        public string? DeploymentWindow { get; set; } // e.g. "22:00 - 02:00"

        [StringLength(100)]
        [Display(Name = "Environment")]
        public string Environment { get; set; } = "Production";

        // Populated from Identity
        public string DeveloperUserId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; } = string.Empty;

        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

        // Approval trail
        public string? Manager1UserId { get; set; }
        public string? Manager1Name { get; set; }
        public DateTime? Manager1ApprovedAt { get; set; }
        public string? Manager1Comments { get; set; }

        public string? Manager2UserId { get; set; }
        public string? Manager2Name { get; set; }
        public DateTime? Manager2ApprovedAt { get; set; }
        public string? Manager2Comments { get; set; }

        public string? RejectedByUserId { get; set; }
        public string? RejectedByName { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime? DeployedAt { get; set; }
    }

    public enum ChangeRequestStatus
    {
        Pending,
        Manager1Approved,
        Manager2Approved, // = Fully Approved
        Rejected,
        Deployed
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ChangeCategory
    {
        Standard,
        Emergency,
        Normal,
        Major
    }

    public enum ImpactLevel
    {
        Low,
        Medium,
        High
    }
}
