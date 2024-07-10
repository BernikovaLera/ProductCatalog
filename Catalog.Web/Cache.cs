using System.Text;
using System.Text.Json;
using Catalog.Models.Dto.Responses;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Web;

public class Cache(IDistributedCache distributedCache)
{
    
    private const string ArticleByIdFromCacheOrDatabase = "articleById:{0}";
    private const string ArticlePriceByDateFromCacheOrDatabase = "articlePriceByDate:{0}";
    private const string AllArticlePricesFromCacheOrDatabase = "allArticlePrices:{0}";
    
    public async Task<ArticleResponseDto?> GetArticleByIdFromCacheOrDatabase(Guid articleId, Func<Guid, Task<ArticleResponseDto>> databaseQuery) // Метод для получения данных статьи из кэша или базы данных. Принимает идентификатор статьи и функцию запроса к базе данных
    {
        var cacheKey = $"ArticleById:{articleId}";
        //var cacheKey = $"Article_{articleId}"; // Создание ключа для кэширования данных 
        var cachedData = await distributedCache.GetAsync(cacheKey); // Получение закэшированных данных из кэша по ключу

        if (cachedData != null) // Проверка наличия закэшированных данных
        {
            var serializedData = Encoding.UTF8.GetString(cachedData); // Десериализация закэшированных данных в строку
            Console.WriteLine($"{serializedData} извлечен из кэша");
            return JsonSerializer.Deserialize<ArticleResponseDto>(serializedData); // Десериализация строки в объект типа ArticleResponseDto и возврат его
        }
        var responseDto = await databaseQuery(articleId); // Вызов функции запроса к базе данных для получения данных статьи
        if (true) // Проверка наличия данных статьи в базе данных
        {
            Console.WriteLine($"{responseDto.ArticleName} извлечен из базы данных");
            var serializedResponse = JsonSerializer.Serialize(responseDto); // Сериализация данных статьи для сохранения в кэше
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Создание параметров для записи в кэш с указанием времени жизни
            };
            await distributedCache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializedResponse), options); // Сохранение сериализованных данных статьи в кэше с указанными параметрами
        }
        return responseDto; //  Возврат данных статьи, полученных из базы данных
    }
    
    public async Task<ArticleResponseDto?> GetArticlePriceByDateFromCacheOrDatabase(Guid articleId, Func<Guid, Task<ArticleResponseDto>> databaseQuery) 
    {
        var cacheKey = $"ArticlePriceByDate:{articleId}";
        var cachedData = await distributedCache.GetAsync(cacheKey); 

        if (cachedData != null) 
        {
            var serializedData = Encoding.UTF8.GetString(cachedData); 
            Console.WriteLine($"{serializedData} извлечен из кэша");
            return JsonSerializer.Deserialize<ArticleResponseDto>(serializedData); 
        }
        var responseDto = await databaseQuery(articleId); 
        if (true) 
        {
            Console.WriteLine($"{responseDto.ArticleName} извлечен из базы данных");
            var serializedResponse = JsonSerializer.Serialize(responseDto); 
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
            };
            await distributedCache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializedResponse), options); 
        }
        return responseDto; 
    }
    
    public async Task<ArticleResponseDto?> GetAllArticlePricesFromCacheOrDatabase(Guid articleId, Func<Guid, Task<ArticleResponseDto>> databaseQuery) 
    {
        var cacheKey = $"AllArticlePrices:{articleId}"; 
        var cachedData = await distributedCache.GetAsync(cacheKey); 

        if (cachedData != null) 
        {
            var serializedData = Encoding.UTF8.GetString(cachedData); 
            Console.WriteLine($"{serializedData} извлечен из кэша");
            return JsonSerializer.Deserialize<ArticleResponseDto>(serializedData); 
        }
        var responseDto = await databaseQuery(articleId);
        if (true) 
        {
            Console.WriteLine($"{responseDto.ArticleName} извлечен из базы данных");
            var serializedResponse = JsonSerializer.Serialize(responseDto); 
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
            };
            await distributedCache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializedResponse), options); 
        }
        return responseDto; 
    }
    
}