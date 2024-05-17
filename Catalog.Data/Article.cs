using System.ComponentModel.DataAnnotations;

namespace Catalog.Data;

public class Article : BaseDbEntity 
{
    [Key] public Guid ArticleId { get; set; }
    [MaxLength(100)] public string ArticleName { get; set; }
    [MaxLength(50)] public string ArticleNumber { get; set; }
    
    public virtual List<Price> Prices { get; set; } = new List<Price>();
    
    public Guid ProductTypeId { get; set; }
    public virtual ProductType ProductType  { get; set; }
}