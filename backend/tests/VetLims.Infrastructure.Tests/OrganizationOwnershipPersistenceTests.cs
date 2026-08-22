using Microsoft.EntityFrameworkCore;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;
using Xunit;

namespace VetLims.Infrastructure.Tests;

public sealed class OrganizationOwnershipPersistenceTests
{
    [Fact]
    public void Added_owned_entity_gets_tenant_organization_and_audit_fields()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7, UserName = "tenant-user" });
        var group = new Laboratory { Code = "G1", Name = "Group 1" };

        context.Laboratories.Add(group);
        context.SaveChanges();

        Assert.Equal(7, group.OrganizationId);
        Assert.Equal("tenant-user", group.CreatedBy);
        Assert.False(group.IsDeleted);
    }

    [Fact]
    public void Added_owned_entity_with_matching_organization_is_allowed()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        var clinic = new Clinic { OrganizationId = 7, Code = "C1", Name = "Clinic 1" };

        context.Clinics.Add(clinic);
        context.SaveChanges();

        Assert.Equal(7, clinic.OrganizationId);
    }

    [Fact]
    public void Added_owned_entity_with_conflicting_organization_is_rejected()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        context.Tests.Add(new Test { OrganizationId = 8, Name = "Conflicting test" });

        var exception = Assert.Throws<OrganizationOwnershipException>(() => context.SaveChanges());

        Assert.Contains("conflicts", exception.Message);
    }

    [Fact]
    public void Owned_entity_without_tenant_or_system_context_is_rejected()
    {
        var context = CreateContext(null);
        context.Laboratories.Add(new Laboratory { Code = "G1", Name = "Group 1" });

        Assert.Throws<OrganizationOwnershipException>(() => context.SaveChanges());
    }

    [Fact]
    public void Explicit_system_context_requires_and_accepts_explicit_organization()
    {
        var context = CreateContext(new TestExecutionContext { IsSystemExecution = true, OrganizationId = 11 });
        var clinic = new Clinic { Code = "C1", Name = "Clinic 1" };

        context.Clinics.Add(clinic);
        context.SaveChanges();

        Assert.Equal(11, clinic.OrganizationId);
    }

    [Fact]
    public void Explicit_system_context_without_organization_is_rejected_for_owned_entity()
    {
        var context = CreateContext(new TestExecutionContext { IsSystemExecution = true });
        context.Tests.Add(new Test { Name = "Unscoped test" });

        Assert.Throws<OrganizationOwnershipException>(() => context.SaveChanges());
    }

    [Fact]
    public void Department_can_be_created_without_organization_context()
    {
        var context = CreateContext(null);
        var department = new Department { Name = "Global Department", Code = "GD" };

        context.Departments.Add(department);
        context.SaveChanges();

        Assert.NotEqual(0, department.Id);
        Assert.Null(department.OrganizationId);
        Assert.False(department.IsDeleted);
    }

    [Fact]
    public void Department_legacy_organization_is_ignored_when_modified()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7, UserName = "editor" });
        var department = new Department
        {
            Name = "Global Department",
            Code = "GD",
            OrganizationId = 99
        };
        context.Departments.Add(department);
        context.SaveChanges();
        context.Entry(department).State = EntityState.Detached;

        var loaded = new Department
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            OrganizationId = 99
        };
        context.Departments.Attach(loaded);
        loaded.Name = "Renamed Department";
        context.SaveChanges();

        Assert.Equal("Renamed Department", loaded.Name);
        Assert.Equal(99, loaded.OrganizationId);
        Assert.Equal("editor", loaded.ModifiedBy);
    }

    [Fact]
    public void Department_delete_uses_soft_delete_audit_without_organization_validation()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7, UserName = "deleter" });
        var department = new Department
        {
            Name = "Global Department",
            Code = "GD",
            OrganizationId = 99
        };
        context.Departments.Add(department);
        context.SaveChanges();
        context.Entry(department).State = EntityState.Detached;

        var loaded = new Department
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            OrganizationId = 99
        };
        context.Departments.Attach(loaded);
        context.Departments.Remove(loaded);
        context.SaveChanges();

        Assert.True(loaded.IsDeleted);
        Assert.Equal("deleter", loaded.DeletedBy);
        Assert.Equal("deleter", loaded.ModifiedBy);
        Assert.Equal(EntityState.Unchanged, context.Entry(loaded).State);
    }

    [Fact]
    public void Organization_root_can_be_created_without_context()
    {
        var context = CreateContext(null);
        var organization = new Organization { Code = "ORG", Name = "Organization" };

        context.Organizations.Add(organization);
        context.SaveChanges();

        Assert.Null(organization.OrganizationId);
    }

    [Fact]
    public void Organization_root_is_not_tenant_propagated()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        var organization = new Organization { Code = "ORG", Name = "Organization" };

        context.Organizations.Add(organization);
        context.SaveChanges();

        Assert.Null(organization.OrganizationId);
    }

    [Fact]
    public void Delete_validates_ownership_then_converts_to_soft_delete()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7, UserName = "deleter" });
        var group = new Laboratory { OrganizationId = 7, Code = "G1", Name = "Group 1" };
        context.Laboratories.Add(group);
        context.SaveChanges();
        context.Entry(group).State = EntityState.Detached;

        var loaded = new Laboratory { Id = group.Id, OrganizationId = 7, Code = "G1", Name = "Group 1" };
        context.Laboratories.Attach(loaded);
        context.Laboratories.Remove(loaded);
        context.SaveChanges();

        Assert.True(loaded.IsDeleted);
        Assert.Equal("deleter", loaded.DeletedBy);
        Assert.Equal("deleter", loaded.ModifiedBy);
        Assert.Equal(EntityState.Unchanged, context.Entry(loaded).State);
    }

    [Fact]
    public void Modified_owned_entity_without_organization_is_rejected()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        var group = new Laboratory { Code = "G1", Name = "Group 1" };
        context.Laboratories.Attach(group);
        context.Entry(group).State = EntityState.Modified;

        Assert.Throws<OrganizationOwnershipException>(() => context.SaveChanges());
    }

    [Fact]
    public void Deleted_owned_entity_with_conflicting_organization_is_rejected()
    {
        var context = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        var group = new Laboratory { Id = 123, OrganizationId = 8, Code = "G1", Name = "Group 1" };
        context.Laboratories.Attach(group);
        context.Laboratories.Remove(group);

        Assert.Throws<OrganizationOwnershipException>(() => context.SaveChanges());
    }

    [Fact]
    public async Task Sync_and_async_paths_apply_the_same_ownership_policy()
    {
        var syncContext = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        syncContext.Tests.Add(new Test { OrganizationId = 8, Name = "Sync conflict" });
        var syncException = Assert.Throws<OrganizationOwnershipException>(() => syncContext.SaveChanges());

        var asyncContext = CreateContext(new TestExecutionContext { OrganizationId = 7 });
        asyncContext.Tests.Add(new Test { OrganizationId = 8, Name = "Async conflict" });
        var asyncException = await Assert.ThrowsAsync<OrganizationOwnershipException>(() => asyncContext.SaveChangesAsync());

        Assert.Equal(syncException.Message, asyncException.Message);
    }

    [Theory]
    [InlineData(typeof(Organization), false)]
    [InlineData(typeof(Department), false)]
    [InlineData(typeof(Clinic), true)]
    [InlineData(typeof(Test), true)]
    [InlineData(typeof(BranchDepartment), true)]
    [InlineData(typeof(BranchTest), true)]
    [InlineData(typeof(UserLocation), true)]
    public void Model_maps_organization_only_for_owned_entities(
        Type entityClrType,
        bool shouldMapOrganizationId)
    {
        using var context = CreateContext(null);

        var entityType = context.Model.FindEntityType(entityClrType);
        var organizationProperty = entityType?.FindProperty(nameof(BaseEntity.OrganizationId));

        Assert.NotNull(entityType);
        Assert.Equal(shouldMapOrganizationId, organizationProperty is not null);
    }

    private static VetLimsDbContext CreateContext(TestExecutionContext? executionContext)
    {
        var options = new DbContextOptionsBuilder<VetLimsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VetLimsDbContext(options, executionContext);
    }

    private sealed class TestExecutionContext : ICurrentUserService
    {
        public bool IsSystemExecution { get; init; }
        public int? UserId => null;
        public string? UserName { get; init; }
        public int? OrganizationId { get; init; }
        public int? LaboratoryId => null;
        public bool HasClinicAccess => false;
        public bool HasLaboratoryAccess => false;
        public int? BranchId => null;
        public IReadOnlyCollection<int> BranchIds => Array.Empty<int>();
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
    }
}
