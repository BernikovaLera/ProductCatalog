namespace Catalog.Data;

public abstract class BaseDbEntity
{
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt  { get; set; }
}