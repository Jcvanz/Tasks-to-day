using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Tasks.App.Models;

namespace Tasks.App.Data;

public class AppDbContext : DbContext
{
    public DbSet<TaskColumn> Columns => Set<TaskColumn>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public static string DatabasePath
    {
        get
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TasksApp");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "tasks.db");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relacionamento Coluna -> Tarefas
        modelBuilder.Entity<TaskColumn>()
            .HasMany(c => c.Tasks)
            .WithOne(t => t.Column)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento Tarefa -> Checklist
        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.Checklist)
            .WithOne(c => c.TaskItem)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
