using System.ComponentModel.DataAnnotations;

namespace Catalog.Data;

public class ProductType
{
    [Key] public Guid ProductTypeId { get; set; }
    [MaxLength(50)] public string TypeName { get; set; }
    
    public virtual List<Article> Articles { get; set; } = new List<Article>();
}