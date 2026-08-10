using FluentValidation;
using VetLims.WebApi.DTOs;

namespace VetLims.WebApi.Validation;

// ---------- Species ----------
public class SpeciesCreateValidator : AbstractValidator<SpeciesCreateDto>
{
    public SpeciesCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
public class SpeciesUpdateValidator : AbstractValidator<SpeciesUpdateDto>
{
    public SpeciesUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

// Unit
public class UnitCreateValidator : AbstractValidator<UnitCreateDto>
{
    public UnitCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(50);
    }
}
public class UnitUpdateValidator : AbstractValidator<UnitUpdateDto>
{
    public UnitUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(50);
    }
}

// Department
public class DepartmentCreateValidator : AbstractValidator<DepartmentCreateDto>
{
    public DepartmentCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}
public class DepartmentUpdateValidator : AbstractValidator<DepartmentUpdateDto>
{
    public DepartmentUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}

// ---------- Clients ----------
public class ClientCreateValidator : AbstractValidator<ClientCreateDto>
{
    public ClientCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
public class ClientUpdateValidator : AbstractValidator<ClientUpdateDto>
{
    public ClientUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

// Test
public class TestCreateValidator : AbstractValidator<TestCreateDto>
{
    public TestCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Code).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.ValueType).IsInEnum();
    }
}
public class TestUpdateValidator : AbstractValidator<TestUpdateDto>
{
    public TestUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Code).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.ValueType).IsInEnum();
    }
}

// Parameters
public class TestParameterCreateValidator : AbstractValidator<TestParameterCreateDto>
{
    public TestParameterCreateValidator()
    {
        RuleFor(x => x.TestId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
public class TestParameterUpdateValidator : AbstractValidator<TestParameterUpdateDto>
{
    public TestParameterUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

// Reference Ranges
public class TestReferenceRangeCreateValidator : AbstractValidator<TestReferenceRangeCreateDto>
{
    public TestReferenceRangeCreateValidator()
    {
        RuleFor(x => x.TestParameterId).GreaterThan(0);
        RuleFor(x => x.SpeciesId).GreaterThan(0);
        RuleFor(x => x.AgeFromDays).GreaterThanOrEqualTo(0).When(x => x.AgeFromDays.HasValue);
        RuleFor(x => x.AgeToDays).GreaterThanOrEqualTo(0).When(x => x.AgeToDays.HasValue);
        RuleFor(x => x.AgeToDays).GreaterThanOrEqualTo(x => x.AgeFromDays ?? 0)
            .When(x => x.AgeFromDays.HasValue && x.AgeToDays.HasValue);
    }
}
public class TestReferenceRangeUpdateValidator : AbstractValidator<TestReferenceRangeUpdateDto>
{
    public TestReferenceRangeUpdateValidator()
    {
        RuleFor(x => x.SpeciesId).GreaterThan(0);
        RuleFor(x => x.AgeFromDays).GreaterThanOrEqualTo(0).When(x => x.AgeFromDays.HasValue);
        RuleFor(x => x.AgeToDays).GreaterThanOrEqualTo(0).When(x => x.AgeToDays.HasValue);
        RuleFor(x => x.AgeToDays).GreaterThanOrEqualTo(x => x.AgeFromDays ?? 0)
            .When(x => x.AgeFromDays.HasValue && x.AgeToDays.HasValue);
    }
}

// ---------- Profiles ----------
// Profile
public class ProfileCreateValidator : AbstractValidator<ProfileCreateDto>
{
    public ProfileCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
public class ProfileUpdateValidator : AbstractValidator<ProfileUpdateDto>
{
    public ProfileUpdateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class ProfileTestCreateValidator : AbstractValidator<ProfileTestCreateDto>
{
    public ProfileTestCreateValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
        RuleFor(x => x.TestId).GreaterThan(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
public class ProfileTestUpdateValidator : AbstractValidator<ProfileTestUpdateDto>
{
    public ProfileTestUpdateValidator()
    {
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

// ---------- Packages ----------
public class PackageCreateValidator : AbstractValidator<PackageCreateDto>
{
    public PackageCreateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
public class PackageUpdateValidator : AbstractValidator<PackageUpdateDto>
{
    public PackageUpdateValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class PackageItemCreateValidator : AbstractValidator<PackageItemCreateDto>
{
    public PackageItemCreateValidator()
    {
        RuleFor(x => x.PackageId).GreaterThan(0);
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
public class PackageItemUpdateValidator : AbstractValidator<PackageItemUpdateDto>
{
    public PackageItemUpdateValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

// ---------- Rates ----------
public class RateCreateValidator : AbstractValidator<RateCreateDto>
{
    public RateCreateValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}
public class RateUpdateValidator : AbstractValidator<RateUpdateDto>
{
    public RateUpdateValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        // ClientId may be null (global rate)
    }
}

//Client Update 
public class ClientsCreateValidator : AbstractValidator<ClientCreateDto>
{
    public ClientsCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(30).When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

//Client Rate 

public class ClientRateCreateValidator : AbstractValidator<ClientRateCreateDto>
{
    public ClientRateCreateValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
    }
}
public class ClientRateUpdateValidator : AbstractValidator<ClientRateUpdateDto>
{
    public ClientRateUpdateValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
    }
}