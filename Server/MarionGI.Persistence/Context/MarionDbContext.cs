using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarionGI.Persistence.Context;

public class MarionDbContext : DbContext
{
    public MarionDbContext(DbContextOptions<MarionDbContext> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Bien> Biens => Set<Bien>();
    public DbSet<Contrat> Contrats => Set<Contrat>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<DemandeVisite> DemandesVisite => Set<DemandeVisite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<JournalAudit> JournauxAudit => Set<JournalAudit>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Force EF Core à utiliser "datetime" au lieu de "datetime2" pour toutes les dates
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("datetime");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. REGLE GLOBALE : Désactiver la suppression en cascade par défaut sur TOUTES les clés étrangères
        //    Cela résout définitivement les erreurs "Multiple Cascade Paths" sous SQL Server.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // 2. Filtres globaux pour exclure automatiquement les éléments supprimés logiquement (Soft Delete)
        modelBuilder.Entity<Utilisateur>().HasQueryFilter(u => !u.EstSupprime);
        modelBuilder.Entity<Bien>().HasQueryFilter(b => !b.EstSupprime);
        modelBuilder.Entity<Contrat>().HasQueryFilter(c => !c.EstSupprime);
        modelBuilder.Entity<Paiement>().HasQueryFilter(p => !p.EstSupprime);

        // Utilisateur
        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasIndex(u => u.Telephone).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Nom).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
        });

        // Bien
        modelBuilder.Entity<Bien>(entity =>
        {
            entity.HasIndex(b => b.Reference).IsUnique();
            entity.Property(b => b.Loyer).HasPrecision(18, 2);
            entity.Property(b => b.Superficie).HasPrecision(10, 2);

            // Configuration sécurisée de la conversion JSON pour Photos
            entity.Property(b => b.Photos)
                  .HasColumnType("nvarchar(max)")
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => string.IsNullOrWhiteSpace(v)
                          ? new List<string>()
                          : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.HasOne(b => b.Proprietaire)
                  .WithMany(u => u.BiensProprietaire)
                  .HasForeignKey(b => b.ProprietaireId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Contrat
        modelBuilder.Entity<Contrat>(entity =>
        {
            entity.Property(c => c.MontantLoyer).HasPrecision(18, 2);
            entity.Property(c => c.MontantCaution).HasPrecision(18, 2);

            // Configuration des nouveaux champs
            entity.Property(c => c.FrequencePaiement)
                  .IsRequired()
                  .HasDefaultValue(FrequencePaiement.Mensuel);

            entity.Property(c => c.DelaiJoursTolerance)
                  .IsRequired()
                  .HasDefaultValue(5);

            entity.HasOne(c => c.Bien)
                  .WithMany(b => b.Contrats)
                  .HasForeignKey(c => c.BienId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Locataire)
                  .WithMany(u => u.ContratsLocataire)
                  .HasForeignKey(c => c.LocataireId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Paiement (Garder la suppression en cascade ici car un Paiement dépend strictement d'un Contrat)
        modelBuilder.Entity<Paiement>(entity =>
        {
            entity.HasIndex(p => p.NumeroQuittance).IsUnique();
            entity.Property(p => p.Montant).HasPrecision(18, 2);

            entity.HasOne(p => p.Contrat)
                  .WithMany(c => c.Paiements)
                  .HasForeignKey(p => p.ContratId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}