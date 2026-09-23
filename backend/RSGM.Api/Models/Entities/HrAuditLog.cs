namespace RSGM.Api.Models.Entities;

public class HrAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Result { get; set; } = "Success";

    public string MetadataSummary { get; set; } = string.Empty;
}
