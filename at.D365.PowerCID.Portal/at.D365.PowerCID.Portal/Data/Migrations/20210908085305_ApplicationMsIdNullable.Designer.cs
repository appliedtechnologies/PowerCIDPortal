﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using at.D365.PowerCID.Portal.Data.Models;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    [DbContext(typeof(atPowerCIDContext))]
    [Migration("20210908085305_ApplicationMsIdNullable")]
    partial class ApplicationMsIdNullable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("AsyncJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Action")
                        .HasColumnType("int");

                    b.Property<Guid?>("AsyncOperationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("ImportTargetEnvironment")
                        .HasColumnType("int");

                    b.Property<bool>("IsManaged")
                        .HasColumnType("bit");

                    b.Property<Guid?>("JobId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Action");

                    b.HasIndex("ImportTargetEnvironment");

                    b.ToTable("AsyncJob");
                });

            modelBuilder.Entity("DeploymentPath", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int")
                        .HasColumnName("Created By");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Created On");

                    b.Property<int>("ModifiedBy")
                        .HasColumnType("int")
                        .HasColumnName("Modified By");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Modified On");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(250)
                        .IsUnicode(false)
                        .HasColumnType("varchar(250)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("ModifiedBy");

                    b.ToTable("Deploymentpath");
                });

            modelBuilder.Entity("DeploymentPathEnvironment", b =>
                {
                    b.Property<int>("DeploymentPath")
                        .HasColumnType("int");

                    b.Property<int>("Environment")
                        .HasColumnType("int");

                    b.Property<int>("StepNumber")
                        .HasColumnType("int");

                    b.HasKey("DeploymentPath", "Environment");

                    b.HasIndex("Environment");

                    b.ToTable("DeploymentPathEnvironment");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Action", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int")
                        .HasColumnName("Created By");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Created On");

                    b.Property<string>("ErrorMessage")
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)")
                        .HasColumnName("Error Message");

                    b.Property<bool?>("ExportOnly")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("FinishTime")
                        .HasColumnType("datetime2")
                        .HasColumnName("Finish Time");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<int?>("Result")
                        .HasColumnType("int");

                    b.Property<int?>("Solution")
                        .HasColumnType("int");

                    b.Property<DateTime?>("StartTime")
                        .HasColumnType("datetime2")
                        .HasColumnName("Start Time");

                    b.Property<int?>("Status")
                        .HasColumnType("int");

                    b.Property<int>("TargetEnvironment")
                        .HasColumnType("int")
                        .HasColumnName("Target Environment");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("Result");

                    b.HasIndex("Solution");

                    b.HasIndex("Status");

                    b.HasIndex("TargetEnvironment");

                    b.HasIndex("Type");

                    b.ToTable("Action");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Result")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("ActionResult");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("ActionStatus");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id");

                    b.ToTable("ActionType");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Application", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int")
                        .HasColumnName("Created By");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Created On");

                    b.Property<int>("DevelopmentEnvironment")
                        .HasColumnType("int")
                        .HasColumnName("Development Environment");

                    b.Property<int>("ModifiedBy")
                        .HasColumnType("int")
                        .HasColumnName("Modified By");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Modified On");

                    b.Property<Guid?>("MsId")
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MS Id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<int?>("OrdinalNumber")
                        .HasColumnType("int")
                        .HasColumnName("Ordinal Number");

                    b.Property<int>("Publisher")
                        .HasColumnType("int");

                    b.Property<string>("SolutionUniqueName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("Solution Unique Name");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("DevelopmentEnvironment");

                    b.HasIndex("ModifiedBy");

                    b.HasIndex("Publisher");

                    b.ToTable("Application");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Environment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("BasicUrl")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("Basic URL");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int")
                        .HasColumnName("Created By");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Created On");

                    b.Property<bool>("IsDevelopmentEnvironment")
                        .HasColumnType("bit")
                        .HasColumnName("Is Development Environment");

                    b.Property<int>("ModifiedBy")
                        .HasColumnType("int")
                        .HasColumnName("Modified By");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Modified On");

                    b.Property<Guid>("MsId")
                        .HasMaxLength(36)
                        .IsUnicode(false)
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MS Id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<int?>("OrdinalNumber")
                        .HasColumnType("int")
                        .HasColumnName("Ordinal Number");

                    b.Property<int>("Tenant")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("ModifiedBy");

                    b.HasIndex("Tenant");

                    b.ToTable("Environment");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Publisher", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<Guid>("MsId")
                        .HasMaxLength(36)
                        .IsUnicode(false)
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MS Id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Publisher");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Solution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Application")
                        .HasColumnType("int");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int")
                        .HasColumnName("Created By");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Created On");

                    b.Property<int>("ModifiedBy")
                        .HasColumnType("int")
                        .HasColumnName("Modified By");

                    b.Property<DateTime>("ModifiedOn")
                        .HasColumnType("datetime2")
                        .HasColumnName("Modified On");

                    b.Property<Guid>("MsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<int>("SolutionType")
                        .HasColumnType("int");

                    b.Property<string>("UniqueName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("Unique Name");

                    b.Property<string>("UrlMakerportal")
                        .IsRequired()
                        .HasMaxLength(512)
                        .IsUnicode(false)
                        .HasColumnType("varchar(512)")
                        .HasColumnName("URL Makerportal");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("Application");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("ModifiedBy");

                    b.ToTable("Solution");

                    b.HasDiscriminator<int>("SolutionType");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Tenant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("GitHubInstallationId")
                        .HasColumnType("int");

                    b.Property<string>("GitHubRepositoryName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("MsId")
                        .HasMaxLength(36)
                        .IsUnicode(false)
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MS Id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Tenant");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Firstname")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Lastname")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)");

                    b.Property<Guid>("MsId")
                        .HasMaxLength(36)
                        .IsUnicode(false)
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("MS Id");

                    b.Property<int>("Tenant")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Tenant");

                    b.ToTable("User");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Patch", b =>
                {
                    b.HasBaseType("at.D365.PowerCID.Portal.Data.Models.Solution");

                    b.ToTable("Solution");

                    b.HasDiscriminator().HasValue(1);
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Upgrade", b =>
                {
                    b.HasBaseType("at.D365.PowerCID.Portal.Data.Models.Solution");

                    b.Property<bool>("ApplyManually")
                        .HasColumnType("bit")
                        .HasColumnName("Apply Manually");

                    b.ToTable("Solution");

                    b.HasDiscriminator().HasValue(2);
                });

            modelBuilder.Entity("AsyncJob", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Action", "ActionNavigation")
                        .WithMany("AsyncJobs")
                        .HasForeignKey("Action")
                        .HasConstraintName("FK_AsyncJob_Action")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Environment", "ImportTargetEnvironmentNavigation")
                        .WithMany("AsyncJobs")
                        .HasForeignKey("ImportTargetEnvironment")
                        .HasConstraintName("FK_AsyncJob_Environment");

                    b.Navigation("ActionNavigation");

                    b.Navigation("ImportTargetEnvironmentNavigation");
                });

            modelBuilder.Entity("DeploymentPath", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "CreatedByNavigation")
                        .WithMany("DeploymentPathCreatedByNavigations")
                        .HasForeignKey("CreatedBy")
                        .HasConstraintName("FK_DeploymentPath_Created_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "ModifiedByNavigation")
                        .WithMany("DeploymentPathModifiedByNavigations")
                        .HasForeignKey("ModifiedBy")
                        .HasConstraintName("FK_DeploymentPath_Modified_By")
                        .IsRequired();

                    b.Navigation("CreatedByNavigation");

                    b.Navigation("ModifiedByNavigation");
                });

            modelBuilder.Entity("DeploymentPathEnvironment", b =>
                {
                    b.HasOne("DeploymentPath", "DeploymentPathNavigation")
                        .WithMany("DeploymentPathEnvironments")
                        .HasForeignKey("DeploymentPath")
                        .HasConstraintName("FK_DeploymentPathEnvironment_DeploymentPath")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Environment", "EnvironmentNavigation")
                        .WithMany("DeploymentPathEnvironments")
                        .HasForeignKey("Environment")
                        .HasConstraintName("FK_DeploymentPathEnvironment_Environment")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DeploymentPathNavigation");

                    b.Navigation("EnvironmentNavigation");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Action", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "CreatedByNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("CreatedBy")
                        .HasConstraintName("FK_Action_Created_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.ActionResult", "ResultNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("Result")
                        .HasConstraintName("FK_Action_Result");

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Solution", "SolutionNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("Solution")
                        .HasConstraintName("FK_Action_Solution");

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.ActionStatus", "StatusNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("Status")
                        .HasConstraintName("FK_Action_Status");

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Environment", "TargetEnvironmentNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("TargetEnvironment")
                        .HasConstraintName("FK_Action_Target_Environment")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.ActionType", "TypeNavigation")
                        .WithMany("Actions")
                        .HasForeignKey("Type")
                        .HasConstraintName("FK_Action_Type")
                        .IsRequired();

                    b.Navigation("CreatedByNavigation");

                    b.Navigation("ResultNavigation");

                    b.Navigation("SolutionNavigation");

                    b.Navigation("StatusNavigation");

                    b.Navigation("TargetEnvironmentNavigation");

                    b.Navigation("TypeNavigation");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Application", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "CreatedByNavigation")
                        .WithMany("ApplicationCreatedByNavigations")
                        .HasForeignKey("CreatedBy")
                        .HasConstraintName("FK_Application_Created_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Environment", "DevelopmentEnvironmentNavigation")
                        .WithMany("ApplicationDevelopmentEnviromentNavigation")
                        .HasForeignKey("DevelopmentEnvironment")
                        .HasConstraintName("FK_Application_Development_Environment")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "ModifiedByNavigation")
                        .WithMany("ApplicationModifiedByNavigations")
                        .HasForeignKey("ModifiedBy")
                        .HasConstraintName("FK_Application_Modified_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Publisher", "PublisherNavigation")
                        .WithMany("Applications")
                        .HasForeignKey("Publisher")
                        .HasConstraintName("FK_Application_Publisher")
                        .IsRequired();

                    b.Navigation("CreatedByNavigation");

                    b.Navigation("DevelopmentEnvironmentNavigation");

                    b.Navigation("ModifiedByNavigation");

                    b.Navigation("PublisherNavigation");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Environment", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "CreatedByNavigation")
                        .WithMany("EnvironmentCreatedByNavigations")
                        .HasForeignKey("CreatedBy")
                        .HasConstraintName("FK_Environment_Created_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "ModifiedByNavigation")
                        .WithMany("EnvironmentModifiedByNavigations")
                        .HasForeignKey("ModifiedBy")
                        .HasConstraintName("FK_Environment_Modified_By")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Tenant", "TenantNavigation")
                        .WithMany("Environments")
                        .HasForeignKey("Tenant")
                        .HasConstraintName("FK_Environment_Tenant")
                        .IsRequired();

                    b.Navigation("CreatedByNavigation");

                    b.Navigation("ModifiedByNavigation");

                    b.Navigation("TenantNavigation");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Solution", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Application", "ApplicationNavigation")
                        .WithMany("Solutions")
                        .HasForeignKey("Application")
                        .HasConstraintName("FK_Solution_Application")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "CreatedByNavigation")
                        .WithMany("SolutionCreatedByNavigations")
                        .HasForeignKey("CreatedBy")
                        .HasConstraintName("FK_Solution_Created_by")
                        .IsRequired();

                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.User", "ModifiedByNavigation")
                        .WithMany("SolutionModifiedByNavigations")
                        .HasForeignKey("ModifiedBy")
                        .HasConstraintName("FK_Solution_Modified_by")
                        .IsRequired();

                    b.Navigation("ApplicationNavigation");

                    b.Navigation("CreatedByNavigation");

                    b.Navigation("ModifiedByNavigation");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.User", b =>
                {
                    b.HasOne("at.D365.PowerCID.Portal.Data.Models.Tenant", "TenantNavigation")
                        .WithMany("Users")
                        .HasForeignKey("Tenant")
                        .HasConstraintName("FK_User_Tenant")
                        .IsRequired();

                    b.Navigation("TenantNavigation");
                });

            modelBuilder.Entity("DeploymentPath", b =>
                {
                    b.Navigation("DeploymentPathEnvironments");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Action", b =>
                {
                    b.Navigation("AsyncJobs");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionResult", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionStatus", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.ActionType", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Application", b =>
                {
                    b.Navigation("Solutions");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Environment", b =>
                {
                    b.Navigation("Actions");

                    b.Navigation("ApplicationDevelopmentEnviromentNavigation");

                    b.Navigation("AsyncJobs");

                    b.Navigation("DeploymentPathEnvironments");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Publisher", b =>
                {
                    b.Navigation("Applications");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Solution", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.Tenant", b =>
                {
                    b.Navigation("Environments");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("at.D365.PowerCID.Portal.Data.Models.User", b =>
                {
                    b.Navigation("Actions");

                    b.Navigation("ApplicationCreatedByNavigations");

                    b.Navigation("ApplicationModifiedByNavigations");

                    b.Navigation("DeploymentPathCreatedByNavigations");

                    b.Navigation("DeploymentPathModifiedByNavigations");

                    b.Navigation("EnvironmentCreatedByNavigations");

                    b.Navigation("EnvironmentModifiedByNavigations");

                    b.Navigation("SolutionCreatedByNavigations");

                    b.Navigation("SolutionModifiedByNavigations");
                });
#pragma warning restore 612, 618
        }
    }
}
