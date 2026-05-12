using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Change_order.Models
{
    public class ChangeRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string CRId { get; set; } =  "";

        // ── Section 1: Identification (auto-filled / dropdowns — unchanged) ──
        [Required]
        [StringLength(200)]
        [Display(Name = "Change Request Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Application / System")]
        public string ApplicationName { get; set; } = string.Empty;

        
        [Display(Name = "Priority")]
        public Priority Priority { get; set; }

        [Display(Name = "Impact")]
        public ImpactLevel Impact { get; set; }

        [Display(Name = "Category")]
        public ChangeCategory Category { get; set; }

        
        [Display(Name = "Environment")]
        public DeploymentEnvironment Environment { get; set; }

        // ── Section 2: Description ────────────────────────────────────────────
        [Required]
        [StringLength(2000)]
        [Display(Name = "Change Request Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Business Justification")]
        public string BusinessJustification { get; set; } = string.Empty;

        // ── Section 3: Impact & Proposed Response ─────────────────────────────
        [StringLength(1000)]
        [Display(Name = "Impact Description")]
        public string? ImpactDescription { get; set; }
        // Versioning
        public string Version { get; set; } = "1.0";          // e.g. "1.0", "1.1", "1.2"
        public int? ParentId { get; set; }                      // null = original, set = child version
        public string GroupKey { get; set; } = string.Empty;   // Name + ApplicationName combined key

        [StringLength(1000)]
        [Display(Name = "Impact if Not Making the Change")]
        public string? ImpactIfNotDone { get; set; }

        [StringLength(500)]
        [Display(Name = "Timeline of Impact Occurrence")]
        public string? ImpactTimeline { get; set; }

        [StringLength(1000)]
        [Display(Name = "Recommended Strategy")]
        public string? RecommendedStrategy { get; set; }

        [StringLength(500)]
        [Display(Name = "Cost / Resource / Time Requirements")]
        public string? CostResourceTime { get; set; }

        [StringLength(1000)]
        [Display(Name = "Expected Outcome")]
        public string? ExpectedOutcome { get; set; }

        // ── Section 4: Detailed Assessment ───────────────────────────────────
        [StringLength(200)]
        [Display(Name = "Assessment Assigned To")]
        public string? AssessmentAssignedTo { get; set; }

        [StringLength(1000)]
        [Display(Name = "Tasks Affected")]
        public string? TasksAffected { get; set; }

        [StringLength(1000)]
        [Display(Name = "Stakeholders Affected")]
        public string? StakeholdersAffected { get; set; }

        [StringLength(1000)]
        [Display(Name = "Options Considered")]
        public string? OptionsConsidered { get; set; }

        [StringLength(1000)]
        [Display(Name = "Recommended Actions / Action Plan")]
        public string? RecommendedActions { get; set; }

        [StringLength(500)]
        [Display(Name = "Impact on Scope / Quality / Performance")]
        public string? ImpactOnScope { get; set; }

        [StringLength(500)]
        [Display(Name = "Impact on Schedule")]
        public string? ImpactOnSchedule { get; set; }

        [StringLength(500)]
        [Display(Name = "Additional Resources Required")]
        public string? AdditionalResources { get; set; }

        [StringLength(500)]
        [Display(Name = "Additional Cost")]
        public string? AdditionalCost { get; set; }

        // ── Section 5: Deployment Plan ────────────────────────────────────────
        [Required]
        [Display(Name = "Recommended Implementation Start Date")]
        [DataType(DataType.Date)]
        public DateTime DeploymentDate { get; set; }

        [Display(Name = "Recommended Implementation Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? DeploymentEndDate { get; set; }

        [Display(Name = "Deployment Window")]
        [StringLength(100)]
        public string? DeploymentWindow { get; set; }

        [StringLength(200)]
        [Display(Name = "Person(s) Responsible for Leading Implementation")]
        public string? ImplementationLead { get; set; }

        [StringLength(1000)]
        [Display(Name = "Test Plan")]
        public string? TestPlan { get; set; }

        [StringLength(1000)]
        [Display(Name = "Rollback Plan")]
        public string? RollbackPlan { get; set; }

        // ── Auto-populated from login ──────────────────────────────────────────
        public string DeveloperUserId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; } = string.Empty;

        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

        // ── Approval trail ────────────────────────────────────────────────────
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
        public string? AreasImpacted { get; set; }
    }

    public enum ChangeRequestStatus
    {
        Pending,
        Manager1Approved,
        Manager2Approved,
        Rejected,
        Deployed, 
        PendingApproval
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    // ── Updated to match COJ categories ──────────────────────────────────────
    public enum ChangeCategory
    {
        Implementation,
        Enhancement,
        Development,
        Infrastructure
    }

    public enum ImpactLevel
    {
        Low,
        Medium,
        High
    }
    
    public enum DeploymentEnvironment
    {
        Production,
        UAT
    }
    //public enum ApplicationType
    //{
    //    Liquid,
    //    Zebra,
    //    [Display(Name = "VNS Valuation Notice System")]
    //    VNSValuationNoticeSystem,
    //    Akon,
    //    [Display(Name = "Task Management")]
    //    TaskManagement,
    //    infoUpdate,
    //    [Display(Name = "GV Tool App")]
    //    GVTool,
    //    Notices,
    //    Verification,
    //    SearchPacks,
    //    Other
    //}
}