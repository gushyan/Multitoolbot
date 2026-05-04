using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class FavStopsConfiguration : IEntityTypeConfiguration<FavStops>
    {
        public void Configure(EntityTypeBuilder<FavStops> builder)
        {
            builder.HasKey(s => s.ChatId);
            builder.Property(s => s.ChatId)
                .ValueGeneratedNever();
        }
    }
}
