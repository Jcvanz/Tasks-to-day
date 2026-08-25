using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tasks.App.Models;

namespace Tasks.App.Data;

public class AppDbContext : DbContext
{
    // Autenticação & Usuários
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Meu Dia a Dia
    public DbSet<DailyTask> DailyTasks => Set<DailyTask>();
    public DbSet<DailyChecklistItem> DailyChecklistItems => Set<DailyChecklistItem>();
    public DbSet<DailyTaskCompletion> DailyTaskCompletions => Set<DailyTaskCompletion>();
    public DbSet<DailyChecklistCompletion> DailyChecklistCompletions => Set<DailyChecklistCompletion>();

    // Quadro de Objetivos (Kanban)
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

    public async Task EnsureTablesCreatedAsync()
    {
        await Database.EnsureCreatedAsync();

        const string sql = @"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Email"" TEXT NOT NULL,
                ""PasswordHash"" TEXT NOT NULL,
                ""PasswordSalt"" TEXT NOT NULL,
                ""IsEmailVerified"" INTEGER NOT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""EmailVerificationCodes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""ExpiresAt"" TEXT NOT NULL,
                ""IsUsed"" INTEGER NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""UserSessions"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL,
                ""SessionToken"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""DailyTasks"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL DEFAULT 1,
                ""Title"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""Priority"" INTEGER NOT NULL,
                ""Recurrence"" INTEGER NOT NULL,
                ""StartDate"" TEXT NOT NULL,
                ""EndDate"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""DailyChecklistItems"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""Title"" TEXT NOT NULL,
                ""Order"" INTEGER NOT NULL,
                ""DailyTaskId"" INTEGER NOT NULL,
                FOREIGN KEY (""DailyTaskId"") REFERENCES ""DailyTasks"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""DailyTaskCompletions"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""DailyTaskId"" INTEGER NOT NULL,
                ""Date"" TEXT NOT NULL,
                ""IsCompleted"" INTEGER NOT NULL,
                ""CompletedAt"" TEXT NULL,
                FOREIGN KEY (""DailyTaskId"") REFERENCES ""DailyTasks"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""DailyChecklistCompletions"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""DailyChecklistItemId"" INTEGER NOT NULL,
                ""Date"" TEXT NOT NULL,
                ""IsCompleted"" INTEGER NOT NULL,
                FOREIGN KEY (""DailyChecklistItemId"") REFERENCES ""DailyChecklistItems"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""Columns"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL DEFAULT 1,
                ""Title"" TEXT NOT NULL,
                ""Order"" INTEGER NOT NULL,
                ""ColorHex"" TEXT NOT NULL,
                FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""Tasks"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""Title"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""Priority"" INTEGER NOT NULL,
                ""DueDate"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""Order"" INTEGER NOT NULL,
                ""ColumnId"" INTEGER NOT NULL,
                FOREIGN KEY (""ColumnId"") REFERENCES ""Columns"" (""Id"") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ""ChecklistItems"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""Title"" TEXT NOT NULL,
                ""IsCompleted"" INTEGER NOT NULL,
                ""Order"" INTEGER NOT NULL,
                ""TaskItemId"" INTEGER NOT NULL,
                FOREIGN KEY (""TaskItemId"") REFERENCES ""Tasks"" (""Id"") ON DELETE CASCADE
            );
        ";

        await Database.ExecuteSqlRawAsync(sql);

        // Garante a presença da coluna UserId em bancos legados
        try
        {
            await Database.ExecuteSqlRawAsync("ALTER TABLE \"DailyTasks\" ADD COLUMN \"UserId\" INTEGER NOT NULL DEFAULT 1;");
        }
        catch { /* Coluna já existe */ }

        try
        {
            await Database.ExecuteSqlRawAsync("ALTER TABLE \"Columns\" ADD COLUMN \"UserId\" INTEGER NOT NULL DEFAULT 1;");
        }
        catch { /* Coluna já existe */ }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuário -> Tarefas Diárias & Colunas
        modelBuilder.Entity<User>()
            .HasMany(u => u.DailyTasks)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.GoalColumns)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamentos DailyTask
        modelBuilder.Entity<DailyTask>()
            .HasMany(t => t.Checklists)
            .WithOne(c => c.DailyTask)
            .HasForeignKey(c => c.DailyTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyTask>()
            .HasMany(t => t.Completions)
            .WithOne(c => c.DailyTask)
            .HasForeignKey(c => c.DailyTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyChecklistItem>()
            .HasMany(c => c.Completions)
            .WithOne(cl => cl.DailyChecklistItem)
            .HasForeignKey(cl => cl.DailyChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento Coluna -> Tarefas (Kanban)
        modelBuilder.Entity<TaskColumn>()
            .HasMany(c => c.Tasks)
            .WithOne(t => t.Column)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento Tarefa -> Checklist (Kanban)
        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.Checklist)
            .WithOne(c => c.TaskItem)
            .HasForeignKey(c => c.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
