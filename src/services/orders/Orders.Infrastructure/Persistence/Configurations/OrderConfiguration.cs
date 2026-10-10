using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int CurrencyLength = 3;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.Currency).HasMaxLength(CurrencyLength).IsFixedLength().IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(order => order.PlacedAt).IsRequired();

        builder.Ignore(order => order.Total);

        builder.OwnsMany(order => order.Lines, lines =>
        {
            lines.ToTable("order_lines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.Property<int>("Id").ValueGeneratedOnAdd();
            lines.HasKey("Id");

            lines.Property(line => line.Sku).HasMaxLength(Order.MaxSkuLength).IsRequired();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Ignore(line => line.LineTotal);

            lines.OwnsOne(line => line.UnitPrice, price =>
            {
                price.Property(money => money.Amount).HasColumnName("unit_price").HasPrecision(18, 2);
                price.Property(money => money.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(CurrencyLength)
                    .IsFixedLength();
            });

            lines.Navigation(line => line.UnitPrice).IsRequired();
        });

        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
