using Microsoft.EntityFrameworkCore;

namespace Catalog.Data;
// dotnet ef migrations add Initial --context ApplicationContext --project ../Catalog.Data/ 
// dotnet ef database update --context ApplicationContext  --project ../Catalog.Data/ 
public class ApplicationContext : DbContext
{
    public DbSet<Article> Articles { get; set; }
    public DbSet<Price> Prices { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
        
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Article>(eb =>
        {
            eb.ToTable(nameof(Article));
            eb.Property(p => p.ArticleId).IsRequired();
            eb.Property(p => p.ArticleName).IsRequired();
            eb.Property(p => p.ArticleNumber).IsRequired();
            eb.Property(p => p.ProductTypeId).IsRequired();
            eb.Property(p => p.CreatedAt).IsRequired();
            
            eb.HasMany(d => d.Prices)
                .WithOne(n => n.Article)
                .HasForeignKey(n => n.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Price>(eb =>
        {
            eb.ToTable(nameof(Price));
            eb.Property(p => p.PriceId).IsRequired();
            eb.Property(p => p.ArticleId).IsRequired();
            eb.Property(p => p.StartDate).IsRequired();
            eb.Property(p => p.EndDate).IsRequired();
            eb.Property(p => p.Cost).IsRequired();
            eb.Property(p => p.CreatedAt).IsRequired();
            
        });
        
        modelBuilder.Entity<ProductType>(eb =>
        {
            eb.ToTable(nameof(ProductType));
            eb.Property(p => p.ProductTypeId).IsRequired();
            eb.Property(p => p.TypeName).IsRequired();
            
            eb.HasMany(d => d.Articles)
                .WithOne(n => n.ProductType)
                .HasForeignKey(n => n.ProductTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
    }
}