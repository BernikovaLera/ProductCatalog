using Catalog.Data;
using Catalog.Models.Dto;
using Catalog.Models.Dto.Responses;
using Catalog.Rabbit;
using Catalog.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using Catalog.RabbitMQ;

namespace Catalog;

[ApiController]
[Route("api/[controller]")]
public class ArticleController(ApplicationContext db, IRabbitMqService mqService) : ControllerBase
{
    // private readonly IRabbitMqService _mqService;
    //
    // public ArticleController(IRabbitMqService mqService)
    // {
    //     _mqService = mqService;
    // }
    
    [HttpGet("{articleId:guid}")]
    public async Task<IActionResult> GetArticleById(Guid articleId, Cache articleService)
    {
        // При чтении по идентификатору артикула выводим только артикулы.
        ArticleResponseDto responseDto = await articleService.GetArticleByIdFromCacheOrDatabase(articleId, async (id) =>
        {
            Article article = await db.Articles.Include(inc => inc.ProductType).FirstOrDefaultAsync(p => p.ArticleId == id);
            if (article == null) return null; 
            return new ArticleResponseDto
            {
                ArticleNumber = article.ArticleNumber,
                ArticleName = article.ArticleName,
                ProductType = article.ProductType.TypeName
            };
        });
        if (responseDto == null)
        {
            return NotFound(new { message = "Информация о товаре не найдена" });
        }
        
        mqService.SendMessage(message:"");
        return Ok(responseDto);
    }
    
    [HttpGet("{articleId:guid}/{date}")]
    public async Task<IActionResult> GetArticlePriceByDate(Guid articleId, DateTime date, Cache articleService)
    {
        // При чтении по номеру артикула и дате выводим информацию о цене артикула на эту дату.
        ArticleResponseDto articleResponseDto = await articleService.GetArticlePriceByDateFromCacheOrDatabase(articleId, async _ =>
        {
            Price price = await db.Prices.Include(inc => inc.Article).FirstOrDefaultAsync(p => p.ArticleId == articleId && p.StartDate <= date && p.EndDate >= date);
            if (price == null) return null; 
            return new ArticleResponseDto
            {
                ArticleNumber = price.Article.ArticleNumber,
                ArticleName = price.Article.ArticleName
            };
        });
        if (articleResponseDto == null)
        {
            return NotFound(new { message = "Информация о цене на эту дату не найдена" });
        }
        Price price = await db.Prices.Include(inc => inc.Article).FirstOrDefaultAsync(p => p.ArticleId == articleId && p.StartDate <= date && p.EndDate >= date);
        PriceResponseDto priceResponseDto = new()
        {
            Price = price.Cost,
            StartDate = price.StartDate,
            EndDate = price.EndDate
        };
        return Ok(new { articleResponseDto, priceResponseDto });
    }

    [HttpGet("{articleId:guid}/prices")]
    public async Task<IActionResult> GetAllArticlePrices(Guid articleId, Cache articleService)
    {
        // Чтение всех цен с периодами действия по артикулу.
        ArticleResponseDto articleResponseDto = await articleService.GetAllArticlePricesFromCacheOrDatabase(articleId, async (id) =>
        {
            Price price = await db.Prices.Include(inc => inc.Article).FirstOrDefaultAsync(p => p.ArticleId == id);
            if (price == null) return null; 
            return new ArticleResponseDto
            {
                ArticleNumber = price.Article.ArticleNumber,
                ArticleName = price.Article.ArticleName,
            };
        });
        if (articleResponseDto == null)
        {
            return NotFound(new { message = "Информация о цене не найдена" });
        }
        var prices = await db.Prices.Where(p => p.ArticleId == articleId).Select(p => new { p.Cost, p.StartDate, p.EndDate }).ToListAsync();
        return Ok(new { articleResponseDto, prices });
    }
            


    [HttpPost("{articleId:guid}")]
    public async Task<IActionResult> AddOrUpdateArticle(Guid articleId, [FromBody] ArticleDto articleDto, Cache articleService)
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
            dbArticle.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            return Ok(new {  articleDto, message = "Артикул обновлен" });
        }

        dbArticle = new Article
        {
            ArticleNumber = articleDto.ArticleNumber,
            ArticleName = articleDto.ArticleName,
            ProductTypeId = articleDto.ProductTypeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        // При обновлении цен проверяем наличие артикула в БД и пересечения с другими ценами. 
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow, // ?
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
    public async Task<IActionResult> SoftDeleteArticle(Guid articleId, Cache articleService)
    {
        // Используется «мягкое» удаление, т.е. строка из БД не удаляется, а ставится пометка IsDeleted = true.
        Article article = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
        if (article == null) return NotFound(new { message = "Информация об удалении товаре не найден" });
        if (article.IsDeleted)
        {
            return Ok(new { message = "Полное удаление из базы данных" });
        }
        article.IsDeleted = true;
        article.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = "Товар удален" });
    }
    
    [HttpDelete("delete/{articleId:guid}")]
    public async Task<IActionResult> DeleteArticleWithPrices(Guid articleId)
    {
        // При удалении артикулов удаляются и цены.
        Article article = await db.Articles.Include(p => p.Prices).FirstOrDefaultAsync(p => p.ArticleId == articleId && !p.IsDeleted);
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
        Price price = await db.Prices.Include(p => p.Article).FirstOrDefaultAsync(p => p.PriceId == priceId && !p.IsDeleted);
        if (price == null) return NotFound(new { message = "Информация о товаре не найден" });

        List<Price> previousDate = await db.Prices.Where(p => p.ArticleId == price.ArticleId && 
                                                              (p.StartDate <= price.StartDate && p.StartDate <= price.EndDate)  
                                                              || (p.StartDate >= price.StartDate && p.StartDate >= price.EndDate) && !p.IsDeleted).ToListAsync();
        foreach (Price currentDate in previousDate)
        {
            if (currentDate.EndDate < price.StartDate)
            {
                currentDate.EndDate = price.EndDate;
            }
            currentDate.UpdatedAt = DateTime.UtcNow;
        }

        price.IsDeleted = true;
        price.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Цена удалена" });
    }
}