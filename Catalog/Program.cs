//using System.Security.Cryptography;
using Catalog.Data;
using Catalog.Models.Dto;
//using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

//Чтение
app.MapGet("/api/article/{articleId:guid}", async (Guid articleId, ApplicationContext db) =>
{
    // При чтении по идентификатору артикула выводим только артикулы. Работает
    Article? article = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
    if (article == null) return Results.NotFound(new { message = "Информация о товаре не найден" });
    return Results.Json(new {article.ArticleNumber,  message = "Товар найден" });
});

app.MapGet("/api/article/{articleNumber}/{date}", async ([FromRoute] string articleNumber, DateTime date, ApplicationContext db) =>
{
    // При чтении по номеру артикула и дате выводим информацию о цене артикула на эту дату. Работает
    Price? price = await db.Prices.FirstOrDefaultAsync(p => p.Article.ArticleNumber == articleNumber && p.StartDate <= date && p.EndDate >= date);
    if (price == null) return Results.NotFound(new { message = "Информация о цене не найдена" });
    return Results.Json(price.Cost);
});

app.MapGet("/api/article/{articleNumber}/prices", async ([FromRoute] string articleNumber, ApplicationContext db) =>
{
    // Чтение всех цен с периодами действия по артикулу. Работает
    Price? price = await db.Prices.FirstOrDefaultAsync(p => p.Article.ArticleNumber == articleNumber);
    if (price == null) return Results.NotFound(new { message = "Информация не найдена" });
    return Results.Json(new {price.Cost, price.StartDate, price.EndDate});
});



//Вставка и обновление
app.MapPut("/api/article/{articleId:guid}", async (Guid articleId,[FromBody] ArticleDto articleUpdateAt, ApplicationContext db) =>
{
    // При вставке/обновлении нужно заполнять поля UpdatedAt.Работает
    var newUpdateAt = await db.Articles.FirstOrDefaultAsync(p => p.ArticleId == articleId);
    if (newUpdateAt == null) return Results.NotFound(new { message = "Информация об обновлении артикула не найден" });
    newUpdateAt.UpdatedAt = articleUpdateAt.UpdatedAt;
    await db.SaveChangesAsync();
    return Results.Json(new { articleUpdateAt, message = "UpdatedAt обновлен" });
});

app.MapPut("/api/article/{articleNumber}", async ([FromRoute] string articleNumber, [FromBody] ArticleDto updatedArticle, ApplicationContext db) =>
{
    // При обновлении артикула обновляем только его. Работает
    var existingArticle = await db.Articles.FirstOrDefaultAsync(p=>p.ArticleNumber == articleNumber);
    if (existingArticle == null) return Results.NotFound(new { message = "Информация об обновлении товара не найден" });
    existingArticle.ArticleNumber = updatedArticle.ArticleNumber;
    await db.SaveChangesAsync();
    return Results.Json(new { existingArticle, message = "Товар обновлен" });
});





app.MapPut("/api/article/{articleNumber}/new-prices", async ([FromRoute] string articleNumber, [FromBody] PriceDto newPrice, ApplicationContext db) => //[FromQuery] decimal? cost,
{
    var dbArticle = await db.Articles.Include(p=>p.Prices).FirstOrDefaultAsync(p=>p.ArticleNumber == articleNumber);
    if (dbArticle == null) return Results.NotFound(new { message = "Информация об обновлении цены не найдена" });
    //var articlePrice = db.Prices.Where(p => p.ArticleId == dbArticle.ArticleId).Select(p => new { p.Cost, p.StartDate, p.EndDate}).ToList();
    foreach (var price in dbArticle.Prices)
    { //10.06.24, 11.06.24
        // 15.06.24, 16.06.24
        var intersectionFrom = price.StartDate > newPrice.StartDate ? price.StartDate : newPrice.StartDate; // начало пересечения
        var intersectionTo = price.EndDate < newPrice.EndDate ? price.EndDate : newPrice.EndDate; // конец пересечения
        
        if (intersectionFrom <= intersectionTo) // даты пересекаются
        {
            var newEndDate = newPrice.StartDate.AddDays(-1);
            // if (price.StartDate > newEndDate) //
            // {
            //     dbArticle.Prices.Remove(price);
            // }
            //else
            //{
                price.EndDate = newEndDate;
            //}
           
            await db.SaveChangesAsync();
        }
    }
    // добавляю новую цену
    db.Prices.Add(new Price
    {
        PriceId = Guid.NewGuid(),
        StartDate = newPrice.StartDate,
        EndDate = newPrice.EndDate,
        Cost = newPrice.Price,
        ArticleId = dbArticle.ArticleId,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        IsDeleted = false
    });
    await db.SaveChangesAsync();
    return Results.Json(new { newPrice, message = "Информация об обновлении цены найдена" });
});










//Удаление
app.MapDelete("/api/article/{articleId:guid}", async (Guid articleId , ApplicationContext db) =>
{
    // Используется «мягкое» удаление, т.е. строка из БД не удаляется, а ставится пометка IsDeleted = true. Работает
    Article? articleDelete = await db.Articles.FirstOrDefaultAsync(p=>p.ArticleId == articleId);
    if (articleDelete == null) return Results.NotFound(new { message = "Информация об удалении товаре не найден" });
    articleDelete.IsDeleted = true;
    await db.SaveChangesAsync();
    return Results.Json(new { message = "Товар удален" });

});

app.MapDelete("/api/article/delete/{articleId:guid}", async (Guid articleId, ApplicationContext db) =>
{
    // При удалении артикулов удаляются и цены. Работает
    var article = await db.Articles.Include(p => p.Prices).FirstOrDefaultAsync(p => p.ArticleId == articleId && !p.IsDeleted);
    if (article == null) return Results.NotFound(new { message = "Информация о товаре не найден" });
    article.IsDeleted = true;
    article.UpdatedAt = DateTime.UtcNow;
    foreach (var price in article.Prices)
    {
        price.IsDeleted = true;
        price.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Json(new { message = "Артикул и цены удалены" });
});

app.MapDelete("/api/Price/delete/{priceId:guid}", async (Guid priceId, ApplicationContext db) =>
{
    // При удалении цен следует проверить, разрывала ли эта цена другие периоды. Если разрывала, то вновь соединить их.
    var price = await db.Prices.Include(p => p.Article).FirstOrDefaultAsync(p => p.PriceId == priceId && !p.IsDeleted);
    if (price == null) return Results.NotFound(new { message = "Информация о товаре не найден" });
    {
        var overlappingPrices = await db.Prices.Where(p => p.ArticleId == price.ArticleId && p.ArticleId != price.ArticleId &&
                                                           p.StartDate <= price.EndDate && p.EndDate >= price.StartDate && !p.IsDeleted).ToListAsync();

        if (overlappingPrices.Any())
        {
            foreach (var overlappingPrice in overlappingPrices)
            {
                if (overlappingPrice.StartDate > price.EndDate)
                {
                    overlappingPrice.StartDate = price.EndDate.AddDays(1);
                    overlappingPrice.UpdatedAt = DateTime.UtcNow;
                }
                else if (overlappingPrice.EndDate < price.StartDate)
                {
                    overlappingPrice.EndDate = price.StartDate.AddDays(-1);
                    overlappingPrice.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        price.IsDeleted = true;
        price.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    return Results.Json(new { message = "Артикул и цены удалены" });
});




app.Run();

