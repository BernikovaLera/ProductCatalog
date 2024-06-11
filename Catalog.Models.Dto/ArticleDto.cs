using System.ComponentModel.DataAnnotations;

namespace Catalog.Models.Dto;

public class ArticleDto
{
    /// <summary>
    /// Наименование товара
    /// </summary>
    [Required]
    public string ArticleName { get; set; }

    /// <summary>
    /// Номер артикула
    /// </summary>
    [Required]
    public string ArticleNumber { get; set; }

    /// <summary>
    /// Идентификатор ProductType
    /// </summary>
    [Required]
    public Guid ProductTypeId { get; set; }
}