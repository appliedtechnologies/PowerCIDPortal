using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class atPowerCIDContext : DbContext
    {
        public atPowerCIDContext()
        {
        }

        public atPowerCIDContext(DbContextOptions<atPowerCIDContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Action> Actions { get; set; }
        public virtual DbSet<ActionResult> ActionResults { get; set; }
        public virtual DbSet<ActionStatus> ActionStatuses { get; set; }
        public virtual DbSet<ActionType> ActionTypes { get; set; }
        public virtual DbSet<Application> Applications { get; set; }
        public virtual DbSet<Environment> Environments { get; set; }
        public virtual DbSet<Patch> Patches { get; set; }
        public virtual DbSet<Tenant> Tenants { get; set; }
        public virtual DbSet<Upgrade> Upgrades { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Solution> Solutions { get; set; }
        public virtual DbSet<DeploymentPath> DeploymentPaths { get; set; }
        public virtual DbSet<DeploymentPathEnvironment> DeploymentPathEnvironments { get; set; }
        public virtual DbSet<ApplicationDeploymentPath> ApplicationDeploymentPaths { get; set; }
        public virtual DbSet<ConnectionReference> ConnectionReferences { get; set; }
        public virtual DbSet<ConnectionReferenceEnvironment> ConnectionReferenceEnvironments { get; set; }
        public virtual DbSet<EnvironmentVariable> EnvironmentVariables { get; set; }
        public virtual DbSet<EnvironmentVariableEnvironment> EnvironmentVariableEnvironments { get; set; }

        public virtual DbSet<Publisher> Publishers { get; set; }
        public virtual DbSet<AsyncJob> AsyncJobs { get; set; }
        public virtual DbSet<UserEnvironment> UserEnvironments { get; set; }
        public Guid MsIdCurrentUser { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<ConnectionReference>(entity =>
            {
                entity.ToTable("ConnectionReference");

                entity.Property(e => e.MsId)
                    .HasColumnName("Ms Id")
                    .IsRequired();

                entity.Property(e => e.LogicalName)
                    .HasColumnName("Logical Name")
                    .IsRequired();

                entity.Property(e => e.ConnectorId)
                    .HasColumnName("Connector Id")
                    .IsRequired();

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.HasOne(d => d.ApplicationNavigation)
                    .WithMany(p => p.ConnectionReferences)
                    .HasForeignKey(d => d.Application)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConnectionReference_Application");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.ConnectionReferenceCreatedByNavigation)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConnectionReference_Created_By");

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.ConnectionReferenceModifiedByNavigation)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConnectionReference_Modified_By");

                entity
                    .HasMany(e => e.Environments)
                    .WithMany(e => e.ConnectionReferences)
                    .UsingEntity<ConnectionReferenceEnvironment>(
                        j => j.HasOne(ce => ce.EnvironmentNavigation).WithMany(e => e.ConnectionReferenceEnvironments).HasForeignKey(ce => ce.Environment).HasConstraintName("FK_ConnectionReferenceEnvironment_Environment"),
                        j => j.HasOne(ce => ce.ConnectionReferenceNavigation).WithMany(c => c.ConnectionReferenceEnvironments).HasForeignKey(ce => ce.ConnectionReference).HasConstraintName("FK_ConnectionReferenceEnvironment_ConnectionReference")
                    ).ToTable("ConnectionReferenceEnvironment").HasKey(ce => new { ce.ConnectionReference, ce.Environment });
            });
    
            modelBuilder.Entity<EnvironmentVariable>(entity => {
                entity.ToTable("EnvironmentVariable");

                entity.Property(e => e.MsId)
                    .HasColumnName("Ms Id")
                    .IsRequired();

                entity.Property(e => e.LogicalName)
                    .HasColumnName("Logical Name")
                    .IsRequired();

                entity.Property(e => e.DisplayName)
                    .HasColumnName("Display Name")
                    .IsRequired();

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.HasOne(d => d.ApplicationNavigation)
                    .WithMany(p => p.EnvironmentVariables)
                    .HasForeignKey(d => d.Application)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.EnvironmentVariableCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.EnvironmentVariableModifiedByNavigations)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity
                    .HasMany(e => e.Environments)
                    .WithMany(e => e.EnvironmentVariables)
                    .UsingEntity<EnvironmentVariableEnvironment>(
                        j => j.HasOne(ce => ce.EnvironmentNavigation).WithMany(e => e.EnvironmentVariableEnvironments).HasForeignKey(ce => ce.Environment),
                        j => j.HasOne(ce => ce.EnvironmentVariableNavigation).WithMany(c => c.EnvironmentVariableEnvironments).HasForeignKey(ce => ce.EnvironmentVariable)
                    ).ToTable("EnvironmentVariableEnvironment").HasKey(ev => new { ev.EnvironmentVariable, ev.Environment });
            });

            modelBuilder.Entity<AsyncJob>(entity =>
            {
                entity.ToTable("AsyncJob");

                entity.HasOne(d => d.ActionNavigation)
                    .WithMany(p => p.AsyncJobs)
                    .HasForeignKey(d => d.Action)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AsyncJob_Action");

                entity.HasOne(d => d.ImportTargetEnvironmentNavigation)
                    .WithMany(p => p.AsyncJobs)
                    .HasForeignKey(d => d.ImportTargetEnvironment)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AsyncJob_Environment");
            });

            modelBuilder.Entity<DeploymentPath>(entity =>
            {
                entity.ToTable("Deploymentpath");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.HasOne(d => d.TenantNavigation)
                    .WithMany(p => p.DeploymentPaths)
                    .HasForeignKey(d => d.Tenant)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeploymentPath_Tenant");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.DeploymentPathCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeploymentPath_Created_By");

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.DeploymentPathModifiedByNavigations)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_DeploymentPath_Modified_By");

                entity
                    .HasMany(d => d.Environments)
                    .WithMany(e => e.DeloymentPaths)
                    .UsingEntity<DeploymentPathEnvironment>(
                        j => j.HasOne(de => de.EnvironmentNavigation).WithMany(e => e.DeploymentPathEnvironments).HasForeignKey(e => e.Environment).HasConstraintName("FK_DeploymentPathEnvironment_Environment"),
                        j => j.HasOne(de => de.DeploymentPathNavigation).WithMany(d => d.DeploymentPathEnvironments).HasForeignKey(e => e.DeploymentPath).HasConstraintName("FK_DeploymentPathEnvironment_DeploymentPath")
                    ).ToTable("DeploymentPathEnvironment").Ignore(e => e.Id).HasKey(de => new { de.DeploymentPath, de.Environment });

                entity
                    .HasMany(d => d.Applications)
                    .WithMany(a => a.DeploymentPaths)
                    .UsingEntity<ApplicationDeploymentPath>(
                        j => j.HasOne(ad => ad.ApplicationNavigation).WithMany(a => a.ApplicationDeploymentPaths).HasForeignKey(ad => ad.Application).HasConstraintName("FK_ApplicationDeploymentPath_Application"),
                        j => j.HasOne(ad => ad.DeploymentPathNavigation).WithMany(d => d.ApplicationDeploymentPaths).HasForeignKey(ad => ad.DeploymentPath).HasConstraintName("FK_ApplicationDeploymentPath_DeploymentPath")
                    ).ToTable("ApplicationDeploymentPath").HasKey(ad => new { ad.Application, ad.DeploymentPath });


            });
            modelBuilder.Entity<Action>(entity =>
            {
                entity.ToTable("Action");

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.ErrorMessage)
                    .IsUnicode(false)
                    .HasColumnName("Error Message");

                entity.Property(e => e.FinishTime).HasColumnName("Finish Time");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.StartTime).HasColumnName("Start Time");

                entity.Property(e => e.TargetEnvironment).HasColumnName("Target Environment");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Action_Created_By");

                entity.HasOne(d => d.SolutionNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.Solution)
                    .HasConstraintName("FK_Action_Solution");

                entity.HasOne(d => d.ResultNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.Result)
                    .HasConstraintName("FK_Action_Result");

                entity.HasOne(d => d.StatusNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.Status)
                    .HasConstraintName("FK_Action_Status");

                entity.HasOne(d => d.TargetEnvironmentNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.TargetEnvironment)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Action_Target_Environment");

                entity.HasOne(d => d.TypeNavigation)
                    .WithMany(p => p.Actions)
                    .HasForeignKey(d => d.Type)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Action_Type");
            });

            modelBuilder.Entity<ActionResult>(entity =>
            {
                entity.ToTable("ActionResult");

                entity.Property(e => e.Result)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.HasData(new ActionResult {
                    Id = 1,
                    Result = "success" 
                }, new ActionResult {
                    Id = 2,
                    Result = "failure" 
                });
            });

            modelBuilder.Entity<ActionStatus>(entity =>
            {
                entity.ToTable("ActionStatus");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.HasData(new ActionStatus {
                    Id = 1,
                    Status = "queued" 
                }, new ActionStatus {
                    Id = 2,
                    Status = "in progress" 
                }, new ActionStatus {
                    Id = 3,
                    Status = "completed" 
                }, new ActionStatus {
                    Id = 4,
                    Status = "applying upgrade" 
                });
            });

            modelBuilder.Entity<ActionType>(entity =>
            {
                entity.ToTable("ActionType");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(30)
                    .IsUnicode(false);

                entity.HasData(new ActionType {
                    Id = 1,
                    Type = "export" 
                }, new ActionType {
                    Id = 2,
                    Type = "import" 
                });
            });

            modelBuilder.Entity<Application>(entity =>
            {
                entity.ToTable("Application");

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.DevelopmentEnvironment).HasColumnName("Development Environment");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.Property(e => e.MsId)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("MS Id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.OrdinalNumber).HasColumnName("Ordinal Number");

                entity.Property(e => e.SolutionUniqueName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Solution Unique Name");

                entity.HasOne(d => d.DevelopmentEnvironmentNavigation)
                    .WithMany(p => p.ApplicationDevelopmentEnviromentNavigation)
                    .HasForeignKey(d => d.DevelopmentEnvironment)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Application_Development_Environment");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.ApplicationCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Application_Created_By");

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.ApplicationModifiedByNavigations)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Application_Modified_By");

                entity.HasOne(d => d.PublisherNavigation)
                    .WithMany(p => p.Applications)
                    .HasForeignKey(d => d.Publisher)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Application_Publisher");

            });

            modelBuilder.Entity<Environment>(entity =>
            {
                entity.ToTable("Environment");

                entity.Property(e => e.BasicUrl)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Basic URL");

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.IsDevelopmentEnvironment).HasColumnName("Is Development Environment");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.Property(e => e.MsId)
                    .IsRequired()
                    .HasMaxLength(36)
                    .IsUnicode(false)
                    .HasColumnName("MS Id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.OrdinalNumber).HasColumnName("Ordinal Number");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.EnvironmentCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Environment_Created_By");

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.EnvironmentModifiedByNavigations)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Environment_Modified_By");

                entity.HasOne(d => d.TenantNavigation)
                    .WithMany(p => p.Environments)
                    .HasForeignKey(d => d.Tenant)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Environment_Tenant");
            });

            modelBuilder.Entity<Patch>(entity =>
            {
                entity.ToTable("Solution");

            });

            modelBuilder.Entity<Solution>(entity =>
            {
                entity.ToTable("Solution")
                    .HasDiscriminator<int>("SolutionType")
                    .HasValue<Patch>(1)
                    .HasValue<Upgrade>(2);

                entity.Property(e => e.CreatedBy).HasColumnName("Created By");

                entity.Property(e => e.CreatedOn).HasColumnName("Created On");

                entity.Property(e => e.ModifiedBy).HasColumnName("Modified By");

                entity.Property(e => e.ModifiedOn).HasColumnName("Modified On");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.UniqueName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Unique Name");

                entity.Property(e => e.UrlMakerportal)
                    .IsRequired()
                    .HasMaxLength(512)
                    .IsUnicode(false)
                    .HasColumnName("URL Makerportal");

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.OverwriteUnmanagedCustomizations)
                    .HasColumnName("Overwrite Unmanaged Customizations")
                    .HasDefaultValue(true);

                entity.Property(e => e.EnableWorkflows)
                    .HasColumnName("Enable Workflows")
                    .HasDefaultValue(true);

                entity.HasOne(d => d.ApplicationNavigation)
                    .WithMany(p => p.Solutions)
                    .HasForeignKey(d => d.Application)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Solution_Application");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.SolutionCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Solution_Created_by");

                entity.HasOne(d => d.ModifiedByNavigation)
                    .WithMany(p => p.SolutionModifiedByNavigations)
                    .HasForeignKey(d => d.ModifiedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Solution_Modified_by");
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenant");

                entity.Property(e => e.MsId)
                    .IsRequired()
                    .HasMaxLength(36)
                    .IsUnicode(false)
                    .HasColumnName("MS Id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Publisher>(entity =>
            {
                entity.ToTable("Publisher");

                entity.Property(e => e.MsId)
                    .IsRequired()
                    .HasMaxLength(36)
                    .IsUnicode(false)
                    .HasColumnName("MS Id");

                entity.HasOne(d => d.EnvironmentNavigation)
                    .WithMany(p => p.Publishers)
                    .HasForeignKey(d => d.Environment)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });


            modelBuilder.Entity<Upgrade>(entity =>
            {
                entity.ToTable("Solution");

                entity.Property(e => e.ApplyManually).HasColumnName("Apply Manually");

            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Firstname)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Lastname)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.MsId)
                    .IsRequired()
                    .HasMaxLength(36)
                    .IsUnicode(false)
                    .HasColumnName("MS Id");

                entity.HasOne(d => d.TenantNavigation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.Tenant)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity
                    .HasMany(d => d.Environments)
                    .WithMany(u => u.Users)
                    .UsingEntity<UserEnvironment>(
                        j => j.HasOne(de => de.EnvironmentNavigation).WithMany(e => e.UserEnvironments).HasForeignKey(e => e.Environment).HasConstraintName("FK_UserEnvironment_Environment"),
                        j => j.HasOne(de => de.UserNavigation).WithMany(d => d.UserEnvironments).HasForeignKey(e => e.User).HasConstraintName("FK_UserEnvironment_User")
                    ).ToTable("UserEnvironment").HasKey(de => new { de.User, de.Environment });
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        protected virtual void OnBeforeSaving(Guid msIdCurrentUser = default)
        {
            this.ChangeTracker.DetectChanges();

            var added = this.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added && e.Entity is ITrackCreated)
                        .Select(e => e.Entity);

            var modified = this.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Modified && e.Entity is ITrackModified)
                        .Select(e => e.Entity);

            if (added.Count() > 0 || modified.Count() > 0)
            {
                User currentUser;
                if (msIdCurrentUser != default)
                {
                    currentUser = this.Users.First(e => e.MsId == msIdCurrentUser);

                }
                else
                {
                    currentUser = this.Users.First(e => e.MsId == this.MsIdCurrentUser);
                }
                foreach (ITrackCreated entity in added)
                {
                    entity.CreatedOn = DateTime.Now;
                    entity.CreatedBy = currentUser.Id;
                }

                foreach (ITrackModified entity in modified.Union(added.Where(e => e is ITrackModified).Cast<ITrackModified>()))
                {
                    entity.ModifiedOn = DateTime.Now;
                    entity.ModifiedBy = currentUser.Id;
                }

            }
        }

        public override int SaveChanges()
        {
            this.OnBeforeSaving();
            return base.SaveChanges();
        }
        public int SaveChanges(Guid msIdCurrentUser)
        {
            this.OnBeforeSaving(msIdCurrentUser);
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this.OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public Task<int> SaveChangesAsync(Guid msIdCurrentUser, bool acceptAllChangesOnSuccess = true,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this.OnBeforeSaving(msIdCurrentUser);
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
