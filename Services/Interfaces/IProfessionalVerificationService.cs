using SubExplore.Models.DTOs;
using SubExplore.Models.Enums;

namespace SubExplore.Services.Interfaces
{
    /// <summary>
    /// Professional account verification service
    /// Handles verification requests, document validation, and organization management
    /// </summary>
    public interface IProfessionalVerificationService
    {
        #region Verification Requests

        /// <summary>
        /// Submit professional verification request
        /// </summary>
        Task<SubmissionResult> SubmitVerificationRequestAsync(Guid userId, ProfessionalVerificationRequest request);

        /// <summary>
        /// Get verification request by ID
        /// </summary>
        Task<ProfessionalVerificationRequest?> GetVerificationRequestAsync(Guid requestId);

        /// <summary>
        /// Get verification requests by user
        /// </summary>
        Task<IEnumerable<ProfessionalVerificationRequest>> GetUserVerificationRequestsAsync(Guid userId);

        /// <summary>
        /// Get pending verification requests (admin only)
        /// </summary>
        Task<IEnumerable<ProfessionalVerificationRequest>> GetPendingVerificationRequestsAsync();

        /// <summary>
        /// Update verification request status
        /// </summary>
        Task<bool> UpdateVerificationStatusAsync(Guid requestId, VerificationStatus status, string notes, Guid reviewerId);

        #endregion

        #region Document Management

        /// <summary>
        /// Upload verification document
        /// </summary>
        Task<DocumentUploadResult> UploadVerificationDocumentAsync(Guid requestId, string fileName, byte[] fileContent, string contentType);

        /// <summary>
        /// Get verification documents
        /// </summary>
        Task<IEnumerable<VerificationDocument>> GetVerificationDocumentsAsync(Guid requestId);

        /// <summary>
        /// Delete verification document
        /// </summary>
        Task<bool> DeleteVerificationDocumentAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Validate document requirements
        /// </summary>
        Task<DocumentValidationResult> ValidateDocumentRequirementsAsync(Guid requestId);

        #endregion

        #region Organization Management

        /// <summary>
        /// Verify organization information
        /// </summary>
        Task<OrganizationVerificationResult> VerifyOrganizationAsync(string organizationName, string website);

        /// <summary>
        /// Get organization by domain
        /// </summary>
        Task<OrganizationInfo?> GetOrganizationByDomainAsync(string emailDomain);

        /// <summary>
        /// Register new organization
        /// </summary>
        Task<Guid> RegisterOrganizationAsync(OrganizationRegistrationRequest request);

        /// <summary>
        /// Update organization information
        /// </summary>
        Task<bool> UpdateOrganizationAsync(Guid organizationId, OrganizationInfo organization);

        #endregion

        #region Verification Process

        /// <summary>
        /// Process verification request (admin only)
        /// </summary>
        Task<bool> ProcessVerificationRequestAsync(Guid requestId, bool approved, string processorNotes, Guid processedBy);

        /// <summary>
        /// Request additional information
        /// </summary>
        Task<bool> RequestAdditionalInformationAsync(Guid requestId, List<string> requiredInfo, string message);

        /// <summary>
        /// Get verification checklist
        /// </summary>
        Task<VerificationChecklist> GetVerificationChecklistAsync(Guid requestId);

        /// <summary>
        /// Update checklist item
        /// </summary>
        Task<bool> UpdateChecklistItemAsync(Guid requestId, string itemId, bool completed, string notes);

        #endregion

        #region Analytics & Reporting

        /// <summary>
        /// Get verification statistics
        /// </summary>
        Task<VerificationStatistics> GetVerificationStatisticsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get processor performance metrics
        /// </summary>
        Task<IEnumerable<ProcessorPerformanceMetrics>> GetProcessorPerformanceAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Generate verification report
        /// </summary>
        Task<byte[]> GenerateVerificationReportAsync(Guid requestId);

        #endregion

        #region Communication

        /// <summary>
        /// Send verification status notification
        /// </summary>
        Task<bool> SendStatusNotificationAsync(Guid userId, VerificationStatus status, string message);

        /// <summary>
        /// Send reminder for pending requirements
        /// </summary>
        Task<bool> SendRequirementReminderAsync(Guid userId, List<string> pendingRequirements);

        /// <summary>
        /// Get communication history
        /// </summary>
        Task<IEnumerable<CommunicationRecord>> GetCommunicationHistoryAsync(Guid requestId);

        #endregion
    }

    /// <summary>
    /// Verification submission result
    /// </summary>
    public class SubmissionResult
    {
        public bool Success { get; set; }
        public Guid? RequestId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public List<string> RequiredDocuments { get; set; } = new();
        public DateTime? EstimatedCompletionDate { get; set; }
    }

    /// <summary>
    /// Document upload result
    /// </summary>
    public class DocumentUploadResult
    {
        public bool Success { get; set; }
        public Guid? DocumentId { get; set; }
        public string? ErrorMessage { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// Verification document
    /// </summary>
    public class VerificationDocument
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public DocumentType DocumentType { get; set; }
        public DocumentStatus Status { get; set; }
        public string? ReviewNotes { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    /// <summary>
    /// Document types
    /// </summary>
    public enum DocumentType
    {
        BusinessRegistration,
        ProfessionalLicense,
        LiabilityInsurance,
        TaxDocument,
        ProfessionalId,
        OrganizationChart,
        Other
    }

    /// <summary>
    /// Document status
    /// </summary>
    public enum DocumentStatus
    {
        Uploaded,
        UnderReview,
        Approved,
        Rejected,
        RequiresUpdate
    }

    /// <summary>
    /// Document validation result
    /// </summary>
    public class DocumentValidationResult
    {
        public bool AllRequirementsMet { get; set; }
        public List<string> MissingDocuments { get; set; } = new();
        public List<string> RejectedDocuments { get; set; } = new();
        public List<string> PendingDocuments { get; set; } = new();
        public int CompletionPercentage { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Organization verification result
    /// </summary>
    public class OrganizationVerificationResult
    {
        public bool IsVerified { get; set; }
        public bool Exists { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string? VerifiedWebsite { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public string? Country { get; set; }
        public string? Industry { get; set; }
        public List<string> ValidationIssues { get; set; } = new();
        public DateTime? LastVerified { get; set; }
    }

    /// <summary>
    /// Organization information
    /// </summary>
    public class OrganizationInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Country { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public List<string> EmailDomains { get; set; } = new();
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// Organization registration request
    /// </summary>
    public class OrganizationRegistrationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Country { get; set; } = string.Empty;
        public string? Industry { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public List<string> EmailDomains { get; set; } = new();
        public Guid RequestedBy { get; set; }
    }

    /// <summary>
    /// Verification checklist
    /// </summary>
    public class VerificationChecklist
    {
        public Guid RequestId { get; set; }
        public List<ChecklistItem> Items { get; set; } = new();
        public int CompletedItems { get; set; }
        public int TotalItems { get; set; }
        public double CompletionPercentage { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Checklist item
    /// </summary>
    public class ChecklistItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsCompleted { get; set; }
        public string? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public ChecklistItemType Type { get; set; }
    }

    /// <summary>
    /// Checklist item types
    /// </summary>
    public enum ChecklistItemType
    {
        DocumentUpload,
        InformationVerification,
        OrganizationValidation,
        ContactVerification,
        ManualReview,
        SystemCheck
    }

    /// <summary>
    /// Verification statistics
    /// </summary>
    public class VerificationStatistics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public double ApprovalRate { get; set; }
        public double AverageProcessingDays { get; set; }
        public Dictionary<string, int> RequestsByCountry { get; set; } = new();
        public Dictionary<string, int> RequestsByIndustry { get; set; } = new();
        public Dictionary<DocumentType, int> DocumentTypeDistribution { get; set; } = new();
    }

    /// <summary>
    /// Processor performance metrics
    /// </summary>
    public class ProcessorPerformanceMetrics
    {
        public Guid ProcessorId { get; set; }
        public string ProcessorName { get; set; } = string.Empty;
        public int RequestsProcessed { get; set; }
        public int RequestsApproved { get; set; }
        public int RequestsRejected { get; set; }
        public double ApprovalRate { get; set; }
        public double AverageProcessingTimeHours { get; set; }
        public int QualityScore { get; set; } // 0-100
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Communication record
    /// </summary>
    public class CommunicationRecord
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public CommunicationType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Recipient { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    /// <summary>
    /// Communication types
    /// </summary>
    public enum CommunicationType
    {
        StatusUpdate,
        RequirementReminder,
        AdditionalInfoRequest,
        DocumentRequest,
        ApprovalNotification,
        RejectionNotification,
        GeneralInquiry
    }
}