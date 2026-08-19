namespace MarionGI.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public bool EstSupprime { get; set; } = false; // Soft Delete
    public DateTime? DateSuppression { get; set; }
}
