namespace Catalog.Models.Dto;

public class PriceDto
{
    /// <summary>
    /// Дата начала действия цены.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Дата окончания действия цены.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Цена.
    /// </summary>
    public decimal Price { get; set; }
}