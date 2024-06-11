using Catalog.Data;
using Catalog.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog;

[ApiController]
[Route("api/[controller]")]
public class ArticleController(ApplicationContext db) : ControllerBase
{
    [HttpGet("{articleId:guid}")]
    public async Task<IActionResult> GetArticleById(Guid articleId)
    {
        // При чтении по идентификатору артикула выводим только артикулы.
        Article article = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
        if (article == null) return NotFound(new { message = "Информация о товаре не найден" });
        return Ok(new { article.ArticleNumber, message = "Товар найден" });
    }

    [HttpGet("{articleNumber}/{date}")]
    public async Task<IActionResult> GetArticlePriceByDate(string articleNumber, DateTime date)
    {
        // При чтении по номеру артикула и дате выводим информацию о цене артикула на эту дату.
        Price price = await db.Prices.FirstOrDefaultAsync(p =>
            p.Article.ArticleNumber == articleNumber && p.StartDate <= date && p.EndDate >= date);
        if (price == null) return NotFound(new { message = "Информация о цене не найдена" });
        return Ok(price.Cost);
    }

    [HttpGet("{articleNumber}/prices")]
    public async Task<IActionResult> GetAllArticlePrices(string articleNumber)
    {
        // Чтение всех цен с периодами действия по артикулу.
        Price price = await db.Prices.FirstOrDefaultAsync(p => p.Article.ArticleNumber == articleNumber);
        if (price == null) return NotFound(new { message = "Информация не найдена" });
        return Ok(new { price.Cost, price.StartDate, price.EndDate });
    }

    [HttpPost("{articleId:guid}")]
    public async Task<IActionResult> AddOrUpdateArticle(Guid articleId, [FromBody] ArticleDto articleDto)
    {
        // При вставке/обновлении нужно заполнять поля UpdatedAt.
        Article dbArticle = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
        if (dbArticle != null)
        {
            if (!string.IsNullOrWhiteSpace(articleDto.ArticleNumber))
            {
                dbArticle.ArticleNumber = articleDto.ArticleNumber;
            }

            if (!string.IsNullOrWhiteSpace(articleDto.ArticleName))
            {
                dbArticle.ArticleName = articleDto.ArticleName;
            }
            
            dbArticle.ProductTypeId = articleDto.ProductTypeId;
            

            dbArticle.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
            return Ok(new {  articleDto, message = "Артикул обновлен" });
        }

        dbArticle = new Article
        {
            ArticleNumber = articleDto.ArticleNumber,
            ArticleName = articleDto.ArticleName,
            ProductTypeId = articleDto.ProductTypeId,
            CreatedAt = DateTime.UtcNow,
        };

        db.Articles.Add(dbArticle);
        await db.SaveChangesAsync();
        return Ok(new { dbArticle, message = "Артикул добавлен" });
    }

    [HttpPut("{articleId:guid}")]
    public async Task<IActionResult> UpdateArticleById(Guid articleId, [FromBody] ArticleDto articleDto)
    {
        // При обновлении артикула обновляем только его.
        Article article = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
        if (article == null) return NotFound(new { message = "Информация об обновлении товара не найден" });
        
        article.ArticleNumber = articleDto.ArticleNumber;
        article.ArticleName = articleDto.ArticleName;
        article.ProductTypeId = articleDto.ProductTypeId;
        
        article.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { article, message = "Артикул обновлен" });
    }

    [HttpPut("{articleNumber}/new-prices")]
    public async Task<IActionResult> AddOrUpdateArticlePrices(string articleNumber, [FromBody] PriceDto newPrice)
    {
        var article = await db.Articles.Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.ArticleNumber == articleNumber);
    
        if (article == null) return NotFound(new { message = "Информация об обновлении цены не найдена" });

        var newPriceEntity = new Price
        {
            PriceId = Guid.NewGuid(),
            StartDate = newPrice.StartDate,
            EndDate = newPrice.EndDate,
            Cost = newPrice.Price,
            ArticleId = article.ArticleId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        foreach (var price in article.Prices.ToList())
        {
            DateTime intersectionFrom = price.StartDate > newPrice.StartDate ? price.StartDate : newPrice.StartDate;
            DateTime intersectionTo = price.EndDate < newPrice.EndDate ? price.EndDate : newPrice.EndDate;

            if (intersectionFrom <= intersectionTo)
            {
                // Корректируем дату окончания существующей цены.
                price.EndDate = newPrice.StartDate.AddDays(-1);

                // Если период цены становится недействительным, ставим флаг удаленный (или удаляем).
                if (price.StartDate > price.EndDate)
                {
                    db.Prices.Remove(price);
                    //price.IsDeleted = true;
                }
                else
                {
                    await db.SaveChangesAsync();
                }
                // Корректируем дату начала новой цены для оставшихся периодов.
                newPrice.StartDate = price.EndDate.AddDays(1);
            }
        }

        // Добавояем новую цену за указанный период.
        db.Prices.Add(newPriceEntity);

        await db.SaveChangesAsync();
        return Ok(new { newPrice, message = "Информация об обновлении цены найдена" });
    }


    [HttpDelete("{articleId:guid}")]
    public async Task<IActionResult> SoftDeleteArticle(Guid articleId)
    {
        // Используется «мягкое» удаление, т.е. строка из БД не удаляется, а ставится пометка IsDeleted = true.
        Article article = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
        if (article == null) return NotFound(new { message = "Информация об удалении товаре не найден" });
        article.IsDeleted = true;
        await db.SaveChangesAsync();
        return Ok(new { message = "Товар удален" });
    }

    [HttpDelete("delete/{articleId:guid}")]
    public async Task<IActionResult> DeleteArticleWithPrices(Guid articleId)
    {
        // При удалении артикулов удаляются и цены.
        Article article = await db.Articles.Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.ArticleId == articleId && !p.IsDeleted);
        if (article == null) return NotFound(new { message = "Информация о товаре не найден" });
        article.IsDeleted = true;
        article.UpdatedAt = DateTime.UtcNow;
        foreach (Price price in article.Prices)
        {
            price.IsDeleted = true;
            price.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Ok(new { message = "Артикул и цены удалены" });
    }

    [HttpDelete("price/delete/{priceId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid priceId)
    {
        // При удалении цен следует проверить, разрывала ли эта цена другие периоды. Если разрывала, то вновь соединить их.
        Price price = await db.Prices.Include(p => p.Article)
            .FirstOrDefaultAsync(p => p.PriceId == priceId && !p.IsDeleted);
        if (price == null) return NotFound(new { message = "Информация о товаре не найден" });

        List<Price> overlappingPrices = await db.Prices
            .Where(p => p.ArticleId == price.ArticleId && p.PriceId != price.PriceId && p.StartDate <= price.EndDate &&
                        p.EndDate >= price.StartDate && !p.IsDeleted)
            .ToListAsync();

        foreach (Price overlappingPrice in overlappingPrices)
        {
            if (overlappingPrice.StartDate > price.EndDate)
            {
                overlappingPrice.StartDate = price.EndDate.AddDays(1);
            }
            else if (overlappingPrice.EndDate < price.StartDate)
            {
                overlappingPrice.EndDate = price.StartDate.AddDays(-1);
            }

            overlappingPrice.UpdatedAt = DateTime.UtcNow;
        }

        price.IsDeleted = true;
        price.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Цена удалена" });
    }
}