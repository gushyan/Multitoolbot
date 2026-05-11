using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class FavStopsConfiguration : IEntityTypeConfiguration<FavStops>
    {
        public void Configure(EntityTypeBuilder<FavStops> builder)
        {
            builder.Property(f => f.StopId).HasColumnName("stop_id");
            builder.Property(f => f.ChatId).HasColumnName("chat_id");

            builder.ToTable("fav_stops");

            builder.HasKey(f => new { f.ChatId, f.StopId });
            
        }
    }
}
