namespace Catalog.Models.Dto.Responses;

public class ArticleResponseDto
{
    /// <summary>
    /// Наименование товара
    /// </summary>
    public string ArticleName { get; set; }

    /// <summary>
    /// Номер артикула
    /// </summary>
    public string ArticleNumber { get; set; }
    public string ProductType { get; set; }
}