using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure.Entities;
using VetLims.Infrastructure.Identity;
using VetLims.Infrastructure;

namespace VetLims.Infrastructure;

public class VetLimsDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    private readonly ICurrentUserService? _currentUserService;

    public VetLimsDbContext(DbContextOptions<VetLimsDbContext> options, ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // ==== DbSets: declare each **once** ====
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();
    public DbSet<ScreenPermission> ScreenPermissions => Set<ScreenPermission>();
    public DbSet<ButtonPermission> ButtonPermissions => Set<ButtonPermission>();
    public DbSet<Species> Species => Set<Species>();

    // User-location assignments
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<Breed> Breeds => Set<Breed>();
    public DbSet<AnimalColor> AnimalColors => Set<AnimalColor>();
    public DbSet<Gender> Genders => Set<Gender>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<SampleType> SampleTypes => Set<SampleType>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<Method> Methods => Set<Method>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientRate> ClientRates => Set<ClientRate>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<MedicalHistoryEntry> MedicalHistoryEntries => Set<MedicalHistoryEntry>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<ClinicalEncounter> ClinicalEncounters => Set<ClinicalEncounter>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestParameter> TestParameters => Set<TestParameter>();
    public DbSet<TestReferenceRange> TestReferenceRanges => Set<TestReferenceRange>();

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileTest> ProfileTests => Set<ProfileTest>();

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<PackageItem> PackageItems => Set<PackageItem>();

    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<RegistrationAnimal> RegistrationAnimals => Set<RegistrationAnimal>();
    public DbSet<RegistrationItem> RegistrationItems => Set<RegistrationItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BarcodeSequence> BarcodeSequences => Set<BarcodeSequence>();

    public DbSet<Result> Results => Set<Result>();
    public DbSet<ResultValue> ResultValues => Set<ResultValue>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingInvoiceLine> BillingInvoiceLines => Set<BillingInvoiceLine>();
    public DbSet<BillingTransaction> BillingTransactions => Set<BillingTransaction>();
    public DbSet<InventoryPurchase> InventoryPurchases => Set<InventoryPurchase>();
    public DbSet<InventoryPurchaseLine> InventoryPurchaseLines => Set<InventoryPurchaseLine>();
    public DbSet<InventoryPurchaseReturn> InventoryPurchaseReturns => Set<InventoryPurchaseReturn>();
    public DbSet<InventoryPurchaseReturnLine> InventoryPurchaseReturnLines => Set<InventoryPurchaseReturnLine>();
    public DbSet<InventoryConsumption> InventoryConsumptions => Set<InventoryConsumption>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<WhatsAppMessageQueue> WhatsAppMessageQueue => Set<WhatsAppMessageQueue>();



    // If you have barcode sequences or others, add them ONCE:
    // public DbSet<BarcodeSequence> BarcodeSequences => Set<BarcodeSequence>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var username = _currentUserService?.UserName ?? "system";

        var currentOrgId = _currentUserService?.OrganizationId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not BaseEntity baseEntity) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    baseEntity.CreatedDate = now;
                    baseEntity.CreatedBy ??= username;

                    // Propagate tenant context for new entities when available
                    baseEntity.OrganizationId ??= currentOrgId;
                    break;
                case EntityState.Modified:
                    baseEntity.ModifiedDate = now;
                    baseEntity.ModifiedBy = username;

                    // Optionally keep tenant fields in sync for modifications
                    baseEntity.OrganizationId ??= currentOrgId;
                    break;
            }

            if (entry.State == EntityState.Deleted && entry.Entity is BaseEntity softDeleteEntity)
            {
                entry.State = EntityState.Modified;
                softDeleteEntity.IsDeleted = true;
                softDeleteEntity.DeletedDate = now;
                softDeleteEntity.ModifiedDate = now;
                softDeleteEntity.ModifiedBy = username;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.Property(x => x.TableName).IsRequired().HasMaxLength(200);
            e.Property(x => x.RecordId).IsRequired().HasMaxLength(200);
            e.Property(x => x.Action).IsRequired().HasMaxLength(50);
            e.Property(x => x.ChangedBy).HasMaxLength(200);
            e.Property(x => x.Details).HasColumnType("text");
            e.HasIndex(x => x.TableName);
            e.HasIndex(x => x.RecordId);
            e.HasIndex(x => x.ChangedAt);
        });

        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.RegistrationNumber).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.ZipOrPin).HasMaxLength(20);
            e.Property(x => x.Website).HasMaxLength(200);
            e.Property(x => x.Logo).HasMaxLength(500);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Clinic>(e =>
        {
            e.ToTable("clinics");
            e.Property(x => x.OrganizationId).IsRequired();
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ShortName).HasMaxLength(50);
            e.Property(x => x.LicenseNumber).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Gst).HasMaxLength(100);
            e.Property(x => x.RegistrationNumber).HasMaxLength(100);
            e.Property(x => x.WhatsAppNumber).HasMaxLength(30);
            e.Property(x => x.Website).HasMaxLength(200);
            e.Property(x => x.Timezone).HasMaxLength(100);
            e.Property(x => x.Currency).HasMaxLength(20);
            e.Property(x => x.Language).HasMaxLength(20);
            e.Property(x => x.EmailSettingsJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasOne(x => x.Organization)
             .WithMany()
             .HasForeignKey(x => x.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.ToTable("branches");
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.LocationType).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.State).HasMaxLength(100);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.Pin).HasMaxLength(20);
            e.Property(x => x.Manager).HasMaxLength(200);
            e.Property(x => x.Latitude).HasColumnType("numeric");
            e.Property(x => x.Longitude).HasColumnType("numeric");
            e.HasIndex(x => new { x.ClinicId, x.Code }).IsUnique();
            e.HasOne(x => x.Clinic)
             .WithMany()
             .HasForeignKey(x => x.ClinicId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.OrganizationId);
            e.HasOne(x => x.Clinic)
             .WithMany()
             .HasForeignKey(x => x.ClinicId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Organization)
             .WithMany()
             .HasForeignKey(x => x.OrganizationId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserLocation>(e =>
        {
            e.ToTable("user_locations");
            e.Property(x => x.IsDefault).HasDefaultValue(false);
            e.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserLocations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Key).IsRequired().HasMaxLength(150);
            e.Property(x => x.GroupName).HasMaxLength(150);
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.Property(x => x.RoleName).IsRequired().HasMaxLength(200);
            e.Property(x => x.PermissionKey).IsRequired().HasMaxLength(150);
            e.HasIndex(x => new { x.RoleName, x.PermissionKey }).IsUnique();
            e.HasOne(x => x.Permission)
             .WithMany()
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRefreshToken>(e =>
        {
            e.ToTable("user_refresh_tokens");
            e.Property(x => x.Token).IsRequired();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoginHistory>(e =>
        {
            e.ToTable("login_histories");
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Success);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserActivityLog>(e =>
        {
            e.ToTable("user_activity_logs");
            e.Property(x => x.Action).IsRequired().HasMaxLength(200);
            e.Property(x => x.EntityName).HasMaxLength(200);
            e.Property(x => x.EntityId).HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Action);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuPermission>(e =>
        {
            e.ToTable("menu_permissions");
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<ScreenPermission>(e =>
        {
            e.ToTable("screen_permissions");
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.MenuPermission)
             .WithMany()
             .HasForeignKey(x => x.MenuPermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ButtonPermission>(e =>
        {
            e.ToTable("button_permissions");
            e.Property(x => x.Code).IsRequired().HasMaxLength(100);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.ScreenPermission)
             .WithMany()
             .HasForeignKey(x => x.ScreenPermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Ignore the abstract BaseEntity so EF Core does not create a separate BaseEntity table.
        // The shared audit and soft-delete fields remain available on the concrete mapped entities.
        modelBuilder.Ignore<BaseEntity>();

        // === Species ===
        modelBuilder.Entity<Species>(e =>
        {
            e.ToTable("species");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Breed>(e =>
        {
            e.ToTable("breeds");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<AnimalColor>(e =>
        {
            e.ToTable("animal_colors");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Gender>(e =>
        {
            e.ToTable("genders");
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Doctor>(e =>
        {
            e.ToTable("doctors");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.LicenseNumber).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SampleType>(e =>
        {
            e.ToTable("sample_types");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Container>(e =>
        {
            e.ToTable("containers");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Method>(e =>
        {
            e.ToTable("methods");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Instrument>(e =>
        {
            e.ToTable("instruments");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Manufacturer).HasMaxLength(200);
            e.Property(x => x.Model).HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Medicine>(e =>
        {
            e.ToTable("medicines");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.GenericName).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(200);
            e.Property(x => x.Unit).HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<InventoryCategory>(e =>
        {
            e.ToTable("inventory_categories");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // === Client ===
        modelBuilder.Entity<Client>(e =>
        {
            e.ToTable("clients");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Code).HasMaxLength(30);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Code);
        });

        // === ClientRate ===
        modelBuilder.Entity<ClientRate>(e =>
        {
            e.ToTable("client_rates");
            e.Property(x => x.Rate).HasColumnType("numeric(18,2)");
            e.Property(x => x.ItemType).HasConversion<int>();
            e.HasIndex(x => new { x.ClientId, x.ItemType, x.ItemId }).IsUnique();
            e.HasOne(x => x.Client)
             .WithMany()
             .HasForeignKey(x => x.ClientId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Step 6: Owner and animal master records are independent from legacy registration snapshots.
        modelBuilder.Entity<Owner>(e =>
        {
            e.ToTable("owners");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Mobile).HasMaxLength(30);
            e.Property(x => x.WhatsApp).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Mobile);
        });

        modelBuilder.Entity<Animal>(e =>
        {
            e.ToTable("animals");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.WeightKg).HasColumnType("numeric(8,3)");
            e.Property(x => x.MicrochipNumber).HasMaxLength(100);
            e.Property(x => x.PhotoUrl).HasMaxLength(1000);
            e.Property(x => x.VaccinationStatus).HasMaxLength(100);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.MicrochipNumber).IsUnique();
            e.HasIndex(x => new { x.OwnerId, x.Name });
            e.HasOne(x => x.Owner).WithMany(x => x.Animals).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Species).WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Breed).WithMany().HasForeignKey(x => x.BreedId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Gender).WithMany().HasForeignKey(x => x.GenderId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Color).WithMany().HasForeignKey(x => x.ColorId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MedicalHistoryEntry>(e =>
        {
            e.ToTable("medical_history_entries");
            e.Property(x => x.EntryType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Diagnosis).HasMaxLength(1000);
            e.Property(x => x.Treatment).HasMaxLength(2000);
            e.Property(x => x.Medication).HasMaxLength(1000);
            e.Property(x => x.Notes).HasColumnType("text");
            e.HasIndex(x => new { x.AnimalId, x.OccurredAt });
            e.HasOne(x => x.Animal).WithMany(x => x.MedicalHistory).HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Cascade);
        });

        // Step 7: Clinical appointment, queue and EMR aggregate.
        modelBuilder.Entity<Appointment>(e =>
        {
            e.ToTable("appointments"); e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => new { x.ScheduledAt, x.Status });
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Animal).WithMany().HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<QueueEntry>(e => { e.ToTable("queue_entries"); e.HasIndex(x => new { x.QueuedAt, x.Status }); e.HasIndex(x => x.AppointmentId).IsUnique(); e.HasOne(x => x.Appointment).WithOne(x => x.QueueEntry).HasForeignKey<QueueEntry>(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ClinicalEncounter>(e => { e.ToTable("clinical_encounters"); e.HasIndex(x => x.AppointmentId).IsUnique(); e.HasOne(x => x.Appointment).WithOne(x => x.Encounter).HasForeignKey<ClinicalEncounter>(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<PrescriptionItem>(e => { e.ToTable("prescription_items"); e.Property(x => x.MedicineName).IsRequired().HasMaxLength(200); e.HasOne(x => x.ClinicalEncounter).WithMany(x => x.Prescriptions).HasForeignKey(x => x.ClinicalEncounterId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Medicine).WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<VaccinationRecord>(e => { e.ToTable("vaccination_records"); e.Property(x => x.VaccineName).IsRequired().HasMaxLength(200); e.HasOne(x => x.ClinicalEncounter).WithMany(x => x.Vaccinations).HasForeignKey(x => x.ClinicalEncounterId).OnDelete(DeleteBehavior.Cascade); });

        // === Department ===
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
        });

        // === Unit ===
        modelBuilder.Entity<Unit>(e =>
        {
            e.ToTable("units");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Symbol).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Symbol).IsUnique();
        });

        // === Test ===
        modelBuilder.Entity<Test>(e =>
        {
            e.ToTable("tests");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.ValueType).HasConversion<int>().IsRequired();
            e.HasIndex(x => x.Name);

            e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // === TestParameter ===
        modelBuilder.Entity<TestParameter>(e =>
        {
            e.ToTable("test_parameters");
            e.Property(x => x.Name).IsRequired();
            e.HasOne(x => x.Test)
             .WithMany(t => t.Parameters)
             .HasForeignKey(x => x.TestId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Unit)
             .WithMany()
             .HasForeignKey(x => x.UnitId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.TestId, x.SortOrder });
        });

        // === TestReferenceRange ===
        modelBuilder.Entity<TestReferenceRange>(e =>
        {
            e.ToTable("test_reference_ranges");

            // ✅ Requires TestParameter.ReferenceRanges navigation to exist
            e.HasOne(x => x.TestParameter)
            .WithMany(p => p.ReferenceRanges)
            .HasForeignKey(x => x.TestParameterId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Species)
            .WithMany()
            .HasForeignKey(x => x.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.TestParameterId, x.SpeciesId, x.AgeFromDays, x.AgeToDays }).IsUnique();
        });

        // === Profile ===
        modelBuilder.Entity<Profile>(e =>
        {
            e.ToTable("profiles");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();  // optional but recommended
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<ProfileTest>(e =>
        {
            e.ToTable("profile_tests");
            e.HasKey(x => new { x.ProfileId, x.TestId }); // composite PK
            e.HasOne(x => x.Profile)
            .WithMany(p => p.ProfileTests)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Test)
            .WithMany() // we don't need back-collection on Test
            .HasForeignKey(x => x.TestId)
            .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ProfileId, x.DisplayOrder });
        });

        // === Package ===
        modelBuilder.Entity<Package>(e =>
        {
            e.ToTable("packages");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<PackageItem>(e =>
        {
            e.ToTable("package_items");
            e.Property(x => x.ItemType).IsRequired();
            e.HasOne(x => x.Package)
            .WithMany(p => p.Items)
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

            // Avoid duplicates of the same Test/Profile in a Package
            e.HasIndex(x => new { x.PackageId, x.ItemType, x.ItemId }).IsUnique();
            e.HasIndex(x => new { x.PackageId, x.DisplayOrder });
        });
        
        // Registration
        modelBuilder.Entity<Registration>(e =>
        {
            e.ToTable("registrations");
            e.Property(x => x.OwnerName).IsRequired().HasMaxLength(300);
            e.Property(x => x.Barcode).IsRequired().HasMaxLength(50);
            e.Property(x => x.ReferenceNumber).HasMaxLength(50);
            e.Property(x => x.ClinicalNotes).HasMaxLength(2000);
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.SetNull);
        });

        // RegistrationAnimal
        modelBuilder.Entity<RegistrationAnimal>(e =>
        {
            e.ToTable("registration_animals");
            e.HasOne(x => x.Registration).WithMany(r => r.Animals).HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Animal).WithMany().HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Species).WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict);
        });

        // RegistrationItem
        modelBuilder.Entity<RegistrationItem>(e =>
        {
            e.ToTable("registration_items");
            e.Property(x => x.ItemType).IsRequired();
            e.Property(x => x.ItemBarcode).IsRequired().HasMaxLength(50);
            e.Property(x => x.SampleNotes).HasMaxLength(500);
            e.HasIndex(x => x.ItemBarcode).IsUnique();

            e.HasOne(x => x.Registration).WithMany(r => r.Items).HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Species).WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SampleType).WithMany().HasForeignKey(x => x.SampleTypeId).OnDelete(DeleteBehavior.SetNull);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Currency).HasMaxLength(10);
            e.HasOne(x => x.Registration).WithMany(r => r.Payments).HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });

        // BarcodeSequence
        modelBuilder.Entity<BarcodeSequence>(e =>
        {
            e.ToTable("barcode_sequences");
            e.Property(x => x.Prefix).HasMaxLength(10);
        });

        // Results
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("results");
            e.Property(x => x.TechnicianVerifiedBy).HasMaxLength(200);
            e.Property(x => x.PathologistAuthenticatedBy).HasMaxLength(200);
            e.Property(x => x.DigitalSignature).HasMaxLength(1000);
            e.Property(x => x.VerificationRemarks).HasMaxLength(1000);
            e.HasOne(x => x.RegistrationItem)
            .WithMany()                                   // one result set per item
            .HasForeignKey(x => x.RegistrationItemId)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });

        // ResultValues
        modelBuilder.Entity<ResultValue>(e =>
        {
            e.ToTable("result_values");
            e.Property(x => x.PositiveNegativeValue).HasMaxLength(50);
            e.Property(x => x.FormulaExpression).HasMaxLength(500);
            e.Property(x => x.FormulaResult).HasMaxLength(200);
            e.Property(x => x.ImageUrl).HasMaxLength(1000);
            e.Property(x => x.PdfAttachmentUrl).HasMaxLength(1000);
            e.HasOne(x => x.Result)
            .WithMany(r => r.Values)
            .HasForeignKey(x => x.ResultId)
            .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.TestParameter)
            .WithMany()
            .HasForeignKey(x => x.TestParameterId)
            .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ResultId, x.TestParameterId }).IsUnique();
        });

        modelBuilder.Entity<BillingInvoice>(e =>
        {
            e.ToTable("billing_invoices");
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(10);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.SubtotalAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.NetAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.PaymentAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.RefundAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.CreditAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.OutstandingAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.CreditBalanceAmount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => new { x.InvoiceDate, x.Status });
            e.HasIndex(x => x.OwnerId);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Animal).WithMany().HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Registration).WithMany().HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BillingInvoiceLine>(e =>
        {
            e.ToTable("billing_invoice_lines");
            e.Property(x => x.Description).IsRequired().HasMaxLength(500);
            e.Property(x => x.Quantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.BillingInvoiceId, x.ModuleType });
            e.HasOne(x => x.BillingInvoice)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.BillingInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillingTransaction>(e =>
        {
            e.ToTable("billing_transactions");
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Reference).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.BillingInvoiceId, x.TransactedAt });
            e.HasOne(x => x.BillingInvoice)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.BillingInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryPurchase>(e =>
        {
            e.ToTable("inventory_purchases");
            e.Property(x => x.PurchaseNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(10);
            e.Property(x => x.InvoiceReference).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => x.PurchaseNumber).IsUnique();
            e.HasIndex(x => x.PurchaseDate);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryPurchaseLine>(e =>
        {
            e.ToTable("inventory_purchase_lines");
            e.Property(x => x.BatchNumber).IsRequired().HasMaxLength(100);
            e.Property(x => x.Quantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.UnitCost).HasColumnType("numeric(18,2)");
            e.Property(x => x.LowStockThreshold).HasColumnType("numeric(18,3)");
            e.Property(x => x.ReturnedQuantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.ConsumedQuantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.AvailableQuantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.LineAmount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.MedicineId, x.BatchNumber });
            e.HasIndex(x => x.ExpiryDate);
            e.HasOne(x => x.InventoryPurchase).WithMany(x => x.Lines).HasForeignKey(x => x.InventoryPurchaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Medicine).WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventoryCategory).WithMany().HasForeignKey(x => x.InventoryCategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryPurchaseReturn>(e =>
        {
            e.ToTable("inventory_purchase_returns");
            e.Property(x => x.ReturnNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.TotalReturnAmount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => x.ReturnNumber).IsUnique();
            e.HasIndex(x => x.ReturnDate);
            e.HasOne(x => x.InventoryPurchase).WithMany(x => x.Returns).HasForeignKey(x => x.InventoryPurchaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryPurchaseReturnLine>(e =>
        {
            e.ToTable("inventory_purchase_return_lines");
            e.Property(x => x.Quantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.UnitCost).HasColumnType("numeric(18,2)");
            e.Property(x => x.ReturnAmount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasOne(x => x.InventoryPurchaseReturn).WithMany(x => x.Lines).HasForeignKey(x => x.InventoryPurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InventoryPurchaseLine).WithMany(x => x.ReturnLines).HasForeignKey(x => x.InventoryPurchaseLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryConsumption>(e =>
        {
            e.ToTable("inventory_consumptions");
            e.Property(x => x.Quantity).HasColumnType("numeric(18,3)");
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => x.ConsumedAt);
            e.HasOne(x => x.InventoryPurchaseLine).WithMany(x => x.Consumptions).HasForeignKey(x => x.InventoryPurchaseLineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WhatsAppTemplate>(e =>
        {
            e.ToTable("whatsapp_templates");
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10);
            e.Property(x => x.HeaderText).HasMaxLength(500);
            e.Property(x => x.BodyTemplate).IsRequired().HasMaxLength(4000);
            e.Property(x => x.FooterText).HasMaxLength(500);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.MessageType, x.LanguageCode, x.IsActive });
        });

        modelBuilder.Entity<WhatsAppMessageQueue>(e =>
        {
            e.ToTable("whatsapp_message_queue");
            e.Property(x => x.ToNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.RecipientName).IsRequired().HasMaxLength(200);
            e.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10);
            e.Property(x => x.ExternalReference).HasMaxLength(100);
            e.Property(x => x.MessageHeader).HasMaxLength(500);
            e.Property(x => x.MessageBody).IsRequired().HasMaxLength(4000);
            e.Property(x => x.MessageFooter).HasMaxLength(500);
            e.Property(x => x.PayloadJson).HasMaxLength(4000);
            e.Property(x => x.AttachmentFileName).HasMaxLength(255);
            e.Property(x => x.AttachmentContentType).HasMaxLength(100);
            e.Property(x => x.ProviderMessageId).HasMaxLength(200);
            e.Property(x => x.LastError).HasMaxLength(2000);

            e.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Registration).WithMany().HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RegistrationItem).WithMany().HasForeignKey(x => x.RegistrationItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.VaccinationRecord).WithMany().HasForeignKey(x => x.VaccinationRecordId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => new { x.Status, x.NextAttemptAt });
            e.HasIndex(x => x.ProviderMessageId);
            e.HasIndex(x => new { x.MessageType, x.CreatedDate });
        });

        // === If you have BarcodeSequence entity ===
        // modelBuilder.Entity<BarcodeSequence>(e => { e.ToTable("barcode_sequences"); ... });
    }
}
