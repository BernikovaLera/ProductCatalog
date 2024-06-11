namespace Catalog.Models.Dto;

public class PriceDto
{
    public Guid PriceId { get; set;}
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    
    public Guid ArticleId { get; set; }
    
}