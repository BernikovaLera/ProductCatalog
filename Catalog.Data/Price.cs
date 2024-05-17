using System.ComponentModel.DataAnnotations;

namespace Catalog.Data;

public class Price : BaseDbEntity
{
    [Key] public Guid PriceId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Cost { get; set; }
    
    public Guid ArticleId { get; set; }
    public virtual Article Article { get; set; }

}