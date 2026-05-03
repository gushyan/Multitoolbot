using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class StopPlaceConfiguration : IEntityTypeConfiguration<StopPlace>
    {
        public void Configure(EntityTypeBuilder<StopPlace> builder)
        {
            builder.HasKey(s => s.Id);
            builder
                .HasIndex(s => s.Name)
                .IsUnique();
        }
    }
}
