using Microsoft.EntityFrameworkCore;
using MedFind.Domain.Entities;
using NetTopologySuite.Geometries;

namespace MedFind.Infrastructure.Data;

public class MedFindDbContext : DbContext
{
    public MedFindDbContext(DbContextOptions<MedFindDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostGIS extension (declared here; migration will emit it)
        modelBuilder.HasPostgresExtension("postgis");
        // Case-insensitive text for email
        modelBuilder.HasPostgresExtension("citext");

        // ── Users ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(u =>
        {
            u.ToTable("users", t => t.HasCheckConstraint("ck_users_role", "role IN ('Patient', 'PharmacyAdmin')"));
            u.HasKey(x => x.Id);
            u.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            u.Property(x => x.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
            u.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            u.Property(x => x.FullName).HasColumnName("full_name");
            u.Property(x => x.Role).HasColumnName("role").IsRequired();
            u.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
            u.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
            u.HasIndex(x => x.Email).IsUnique();
        });

        // ── Pharmacies ─────────────────────────────────────────────────────
        modelBuilder.Entity<Pharmacy>(p =>
        {
            p.ToTable("pharmacies");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            p.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
            p.Property(x => x.Name).HasColumnName("name").IsRequired();
            p.Property(x => x.AddressLine).HasColumnName("address_line");
            p.Property(x => x.City).HasColumnName("city");
            p.Property(x => x.Phone).HasColumnName("phone");
            // PostGIS geography(Point, 4326) — Npgsql stores as Point with SRID 4326
            p.Property(x => x.Location).HasColumnName("location")
                .HasColumnType("geography(Point, 4326)").IsRequired();
            p.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            p.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
            p.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");

            p.HasOne(x => x.Owner).WithOne(u => u.OwnedPharmacy)
                .HasForeignKey<Pharmacy>(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);

            // GiST index — the interview talking point
            p.HasIndex(x => x.Location).HasDatabaseName("ix_pharmacies_location")
                .HasMethod("GIST");
        });

        // ── Medicines ──────────────────────────────────────────────────────
        modelBuilder.Entity<Medicine>(m =>
        {
            m.ToTable("medicines");
            m.HasKey(x => x.Id);
            m.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            m.Property(x => x.Name).HasColumnName("name").IsRequired();
            m.Property(x => x.GenericName).HasColumnName("generic_name");
            m.Property(x => x.Brand).HasColumnName("brand");
            m.Property(x => x.Form).HasColumnName("form");
            m.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
            m.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
        });

        // ── Inventory Items ────────────────────────────────────────────────
        modelBuilder.Entity<InventoryItem>(i =>
        {
            i.ToTable("inventory_items", t => t.HasCheckConstraint("ck_inventory_quantity", "quantity >= 0"));
            i.HasKey(x => x.Id);
            i.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            i.Property(x => x.PharmacyId).HasColumnName("pharmacy_id");
            i.Property(x => x.MedicineId).HasColumnName("medicine_id");
            i.Property(x => x.Quantity).HasColumnName("quantity").HasDefaultValue(0);
            i.Property(x => x.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamptz");
            i.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");
            i.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");

            i.HasOne(x => x.Pharmacy).WithMany(p => p.Inventory)
                .HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            i.HasOne(x => x.Medicine).WithMany(m => m.InventoryItems)
                .HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);

            i.HasIndex(x => new { x.PharmacyId, x.MedicineId }).IsUnique()
                .HasDatabaseName("ix_inventory_pharmacy_medicine");
            i.HasIndex(x => x.VerifiedAt).HasDatabaseName("ix_inventory_verified");
            i.HasIndex(x => x.PharmacyId).HasDatabaseName("ix_inventory_pharmacy");
        });
    }
}
