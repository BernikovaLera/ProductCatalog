using System.ComponentModel.DataAnnotations;

namespace Catalog.Models.Dto;

public class ArticleDto
{
    public Guid ArticleId { get; set; }
    public string ArticleNumber { get; set; }
    public DateTime UpdatedAt  { get; set; }
    

}