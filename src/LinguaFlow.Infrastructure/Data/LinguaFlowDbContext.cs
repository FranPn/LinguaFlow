using LinguaFlow.Core;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Infrastructure.Data;

public class LinguaFlowDbContext(DbContextOptions<LinguaFlowDbContext> options) : DbContext(options)
{
    public DbSet<ConversationSession> Sessions => Set<ConversationSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationSession>(session =>
        {
            session.ToTable("Sessions");
            session.HasKey(s => s.Id);
            session.Property(s => s.TargetLanguage).IsRequired();
            session.Property(s => s.Topic).IsRequired();

            // Turns e Corrections sono modellati come owned collection: non hanno identità
            // o ciclo di vita propri fuori dalla ConversationSession a cui appartengono.
            session.OwnsMany(s => s.Turns, turns =>
            {
                turns.ToTable("Turns");
                turns.WithOwner().HasForeignKey("SessionId");
                turns.HasKey(t => t.Id);
                turns.Property(t => t.Role).IsRequired();
                turns.Property(t => t.Text).IsRequired();

                turns.OwnsMany(t => t.Corrections, corrections =>
                {
                    corrections.ToTable("Corrections");
                    corrections.WithOwner().HasForeignKey("TurnId");
                    corrections.HasKey(c => c.Id);
                    corrections.Property(c => c.Original).IsRequired();
                    corrections.Property(c => c.Corrected).IsRequired();
                    corrections.Property(c => c.Category).IsRequired();
                    corrections.Property(c => c.Explanation).IsRequired();
                });
            });

            // Le collezioni owned (Turns, Corrections) vengono caricate automaticamente
            // insieme all'aggregate root: non serve .Include()/.ThenInclude() esplicito.
        });
    }
}
