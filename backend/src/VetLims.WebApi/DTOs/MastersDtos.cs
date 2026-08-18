
using VetLims.Infrastructure.Entities;
namespace VetLims.WebApi.DTOs;
using VetLims.Infrastructure.Entities;


// Simple list/filter
//public record ListQuery(int? Page, int? PageSize, string? Search, string? SortBy, bool? Desc);

// Species
public record SpeciesDto(int Id, string Code, string Name, bool IsActive);
public record SpeciesCreateDto(string Code, string Name);
public record SpeciesUpdateDto(string Code, string Name);


// Unit
public record UnitDto(int Id, string Name, string? Symbol, bool IsActive);
public record UnitCreateDto(string Name, string? Symbol, bool IsActive);
public record UnitUpdateDto(string Name, string? Symbol, bool IsActive);

// Department
public record DepartmentDto(int Id, string Name, string? Code, bool IsActive);
public record DepartmentCreateDto(string Name, string? Code, bool IsActive = true);
public record DepartmentUpdateDto(string Name, string? Code, bool IsActive = true);

public record BreedDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record BreedCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record BreedUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record AnimalColorDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record AnimalColorCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record AnimalColorUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record GenderDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record GenderCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record GenderUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record DoctorDto(int Id, string Code, string Name, string? LicenseNumber, int? DepartmentId, string? Phone, string? Email, bool IsActive);
public record DoctorCreateDto(string Code, string Name, string? LicenseNumber, int? DepartmentId, string? Phone, string? Email, bool IsActive = true);
public record DoctorUpdateDto(string Code, string Name, string? LicenseNumber, int? DepartmentId, string? Phone, string? Email, bool IsActive = true);

public record SampleTypeDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record SampleTypeCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record SampleTypeUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record ContainerDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record ContainerCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record ContainerUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record MethodDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record MethodCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record MethodUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

public record InstrumentDto(int Id, string Code, string Name, string? Manufacturer, string? Model, bool IsActive);
public record InstrumentCreateDto(string Code, string Name, string? Manufacturer, string? Model, bool IsActive = true);
public record InstrumentUpdateDto(string Code, string Name, string? Manufacturer, string? Model, bool IsActive = true);

public record SupplierDto(int Id, string Code, string Name, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);
public record SupplierCreateDto(string Code, string Name, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive = true);
public record SupplierUpdateDto(string Code, string Name, string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive = true);

public record MedicineDto(int Id, string Code, string Name, string? GenericName, string? Category, string? Unit, bool IsActive);
public record MedicineCreateDto(string Code, string Name, string? GenericName, string? Category, string? Unit, bool IsActive = true);
public record MedicineUpdateDto(string Code, string Name, string? GenericName, string? Category, string? Unit, bool IsActive = true);

public record InventoryCategoryDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record InventoryCategoryCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record InventoryCategoryUpdateDto(string Code, string Name, string? Description, bool IsActive = true);

// Step 5: Shared transport contract for the existing master catalog endpoint.
// Optional fields are used only by the master types that support them (for example Doctor and Supplier).
// This keeps the existing GET response contracts intact while allowing a single reusable Angular screen.
public record MasterCatalogUpsertDto(
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true,
    int? DepartmentId = null,
    string? LicenseNumber = null,
    string? Phone = null,
    string? Email = null,
    string? Manufacturer = null,
    string? Model = null,
    string? ContactPerson = null,
    string? Address = null,
    string? GenericName = null,
    string? Category = null,
    string? Unit = null);

public record OrganizationDto(int Id, string Code, string Name, string? LegalName, string? Email, string? Phone, string? Address, bool IsActive);
public record OrganizationCreateDto(string Code, string Name, string? LegalName, string? Email, string? Phone, string? Address, bool IsActive = true);
public record OrganizationUpdateDto(string Code, string Name, string? LegalName, string? Email, string? Phone, string? Address, bool IsActive = true);

public record LaboratoryGroupDto(int Id, int OrganizationId, string Code, string Name, string? Description, bool IsActive, DateTime CreatedDate, DateTime? ModifiedDate, bool IsDeleted, int ClinicCount);
public record LaboratoryGroupCreateDto(string Code, string Name, string? Description, bool IsActive = true);
public record LaboratoryGroupUpdateDto(string Code, string Name, string? Description, bool IsActive = true);
public record LaboratoryGroupClinicDto(int Id, int OrganizationId, string Code, string Name, string? ShortName, bool IsActive, int? LaboratoryGroupId);

public record ClinicDto(int Id, int OrganizationId, int? LaboratoryGroupId, string Code, string Name, string? Logo, string? Address, string? Phone, string? Email, string? Gst, string? RegistrationNumber, string? WhatsAppNumber, string? ReportHeader, string? ReportFooter, string? DigitalSignature, bool IsActive);
public record ClinicCreateDto(string Code, string Name, int? LaboratoryGroupId = null, string? Logo = null, string? Address = null, string? Phone = null, string? Email = null, string? Gst = null, string? RegistrationNumber = null, string? WhatsAppNumber = null, string? ReportHeader = null, string? ReportFooter = null, string? DigitalSignature = null, bool IsActive = true, int? OrganizationId = null);
public record ClinicUpdateDto(string Code, string Name, int? LaboratoryGroupId = null, string? Logo = null, string? Address = null, string? Phone = null, string? Email = null, string? Gst = null, string? RegistrationNumber = null, string? WhatsAppNumber = null, string? ReportHeader = null, string? ReportFooter = null, string? DigitalSignature = null, bool IsActive = true, int? OrganizationId = null);

public record BranchDto(int Id, int ClinicId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive);
public record BranchCreateDto(int ClinicId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive = true);
public record BranchUpdateDto(int ClinicId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive = true);
public record BranchDepartmentDto(int Id, int BranchId, int DepartmentId, string DepartmentName, string? DepartmentCode, bool IsActive);

public record RoleDto(int Id, string Name);
public record RoleCreateDto(string Name);
public record RoleUpdateDto(string Name);
public record RolePermissionDto(string RoleName, string PermissionKey);
public record RolePermissionAssignDto(string PermissionKey);

public record PermissionDto(int Id, string Name, string Key, string? GroupName, string? Description, bool IsActive);
public record PermissionCreateDto(string Name, string Key, string? GroupName, string? Description, bool IsActive = true);
public record PermissionUpdateDto(string Name, string Key, string? GroupName, string? Description, bool IsActive = true);

public record MenuPermissionDto(int Id, string Code, string Name, string? Description, bool IsActive);
public record ScreenPermissionDto(int Id, string Code, string Name, int MenuPermissionId, string? Description, bool IsActive);
public record ButtonPermissionDto(int Id, string Code, string Name, int ScreenPermissionId, string? Description, bool IsActive);

// Client
public record ClientDto(int Id, string Name, string? Code, string? Phone, string? Email, string? Address, bool IsActive);
public record ClientCreateDto(string Name, string? Code, string? Phone, string? Email, string? Address, bool IsActive = true);
public record ClientUpdateDto(string Name, string? Code, string? Phone, string? Email, string? Address, bool IsActive = true);

// Test + Parameter + Reference Ranges
public record TestDto(int Id, string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, TestValueType ValueType, bool IsActive);
public record TestCreateDto(string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, TestValueType ValueType, bool IsActive);
public record TestUpdateDto(string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, TestValueType ValueType, bool IsActive);

public record TestParameterDto(int Id, int TestId, string Name, int? UnitId, int SortOrder, bool IsActive);
public record TestParameterCreateDto(int TestId, string Name, int? UnitId, int SortOrder, bool IsActive);
public record TestParameterUpdateDto(string Name, int? UnitId, int SortOrder, bool IsActive);

public record TestReferenceRangeDto(int Id, int TestParameterId, int SpeciesId, int? AgeFromDays, int? AgeToDays, decimal? Low, decimal? High, string? Note, bool IsActive);
public record TestReferenceRangeCreateDto(int TestParameterId, int SpeciesId, int? AgeFromDays, int? AgeToDays, decimal? Low, decimal? High, string? Note, bool IsActive);
public record TestReferenceRangeUpdateDto(int SpeciesId, int? AgeFromDays, int? AgeToDays, decimal? Low, decimal? High, string? Note, bool IsActive);

// Profile + ProfileTest
public record ProfileDto(int Id, string Code, string Name, bool IsActive);
public record ProfileCreateDto(string Code, string Name, bool IsActive);
public record ProfileUpdateDto(string Code, string Name, bool IsActive);

public record ProfileTestDto(int ProfileId, int TestId, int DisplayOrder);
public record ProfileTestCreateDto(int ProfileId, int TestId, int DisplayOrder);
public record ProfileTestUpdateDto(int DisplayOrder);

// Package + Items
public record PackageDto(int Id, string Code, string Name, bool IsActive);
public record PackageCreateDto(string Code, string Name, bool IsActive);
public record PackageUpdateDto(string Code, string Name, bool IsActive);

public enum PackageItemTypeDto { Test = 1, Profile = 2 } // UI-facing enum (optional)

public record PackageItemDto(int Id, int PackageId, PackageItemTypeDto ItemType, int ItemId, int DisplayOrder);
public record PackageItemCreateDto(int PackageId, PackageItemTypeDto ItemType, int ItemId, int DisplayOrder);
public record PackageItemUpdateDto(PackageItemTypeDto ItemType, int ItemId, int DisplayOrder);

//Client Rate
public record ClientRateDto(int Id, int ClientId, int ItemId, RegItemType ItemType, decimal Rate, decimal VatRate, string? Currency, bool IsActive);
public record ClientRateCreateDto(int ClientId, int ItemId, RegItemType ItemType, decimal Rate, decimal VatRate, string? Currency, bool IsActive);
public record ClientRateUpdateDto(int ItemId, RegItemType ItemType, decimal Rate, decimal VatRate, string? Currency, bool IsActive);

public record ClientRateBulkItemDto(RegItemType ItemType, int ItemId, decimal? Rate, decimal VatRate, string? Currency, bool IsActive);
public record ClientRateBulkDto(int ClientId, List<ClientRateBulkItemDto> Items);
public record ClientRateBulkResultDto(int Created, int Updated, int Removed);

// Query helpers
// public record ListQuery(int Page = 1, int PageSize = 20, string? Search = null, int? ClientId = null, int? SpeciesId = null);
// public record PagedResult<T> { public int Page {get;set;} public int PageSize {get;set;} public int Total {get;set;} public IEnumerable<T> Items {get;set;} = []; }

// Create a registration with animals + items
public record RegistrationCreateDto(
    string OwnerName,
    int? ClientId,
    int? OwnerId,
    int? DoctorId,
    string? ReferenceNumber,
    string? ClinicalNotes,
    List<RegistrationAnimalDto> Animals,
    List<RegistrationItemCreateDto> Items,
    int? BranchId = null,
    decimal? InitialPaymentAmount = null,
    PaymentMethod? InitialPaymentMethod = null,
    string? InitialPaymentRef = null,
    string? OwnerMobile = null
);

public record RegistrationDto(
    int Id,
    string OwnerName,
    int? ClientId,
    int? OwnerId,
    int? DoctorId,
    string Barcode,
    string? ReferenceNumber,
    string? ClinicalNotes,
    DateTime CreatedAt,
    decimal TotalAmount,
    decimal TotalVat,
    decimal PaidAmount,
    decimal Balance,
    string? OwnerMobile = null
);

public record RegistrationAnimalDto(int SpeciesId, string? AnimalName, int? AnimalId = null, int? AgeInDays = null);

public record RegistrationItemCreateDto(RegItemType ItemType, int ItemId, int? SpeciesId, int? SampleTypeId = null);

public record RegistrationDetailDto(
    int Id,
    string OwnerName,
    int? ClientId,
    int? OwnerId,
    int? DoctorId,
    string Barcode,
    string? ReferenceNumber,
    string? ClinicalNotes,
    DateTime CreatedAt,
    decimal TotalAmount,
    decimal TotalVat,
    decimal PaidAmount,
    decimal Balance,
    IEnumerable<RegistrationAnimalView> Animals,
    IEnumerable<RegistrationItemView> Items,
    IEnumerable<PaymentView> Payments,
    string? OwnerMobile = null
);

public record RegistrationAnimalView(int Id, int SpeciesId, string? AnimalName, int? AnimalId, int? AgeInDays = null);
public record RegistrationItemView(
    int Id,
    RegItemType ItemType,
    int ItemId,
    int? SpeciesId,
    int? SampleTypeId,
    decimal Price,
    decimal VatRate,
    decimal VatAmount,
    string ItemBarcode,
    string Status,
    string SampleStatus,
    DateTime? SampleCollectedAt,
    DateTime? SampleReceivedAt,
    DateTime? SampleProcessedAt,
    string? SampleNotes,
    int Quantity = 1
);
public record PaymentView(int Id, PaymentMethod Method, decimal Amount, DateTime PaidAt, string? Reference, string Currency);

public record PaymentCreateDto(PaymentMethod Method, decimal Amount, string? Reference, string Currency = "AED");

//Result 
public record ResultWorkItemDto(
    int RegistrationItemId,
    int RegistrationId,
    string OwnerName,
    string ItemBarcode,
    string? TestCode,
    string? TestName,
    int? SpeciesId,
    string? SpeciesName,
    string Status,
    int? SampleTypeId,
    string SampleStatus,
    bool TechnicianVerified,
    bool PathologistAuthenticated,
    string? AnimalNames = null,
    int AnimalCount = 0,
    string? OwnerMobile = null
);

public record ResultValueDto(
    int TestParameterId,
    string ParameterName,
    string? UnitSymbol,
    ResultEntryType EntryType,
    decimal? NumericValue,
    string? TextValue,
    string? PositiveNegativeValue,
    string? FormulaExpression,
    string? FormulaResult,
    string? ImageUrl,
    string? PdfAttachmentUrl,
    int? RangeMinDays,
    int? RangeMaxDays,
    decimal? RangeLow,
    decimal? RangeHigh,
    bool? IsLow,
    bool? IsHigh,
    bool? IsOutOfRange
);

public record ResultDetailDto(
    int RegistrationItemId,
    string ItemBarcode,
    string? Notes,
    string SampleStatus,
    string? TechnicianVerifiedBy,
    DateTime? TechnicianVerifiedAt,
    string? PathologistAuthenticatedBy,
    DateTime? PathologistAuthenticatedAt,
    string? DigitalSignature,
    string? VerificationRemarks,
    IEnumerable<ResultValueDto> Values,
    string? OwnerName = null,
    string? AnimalNames = null,
    int AnimalCount = 0
);

// payload to save values
public record ResultEntryDto(
    int RegistrationItemId,
    int? SpeciesId,
    int? AgeInDays,
    string? Notes,
    IEnumerable<ResultEntryValue> Values
);

public record ResultEntryValue(
    int TestParameterId,
    ResultEntryType EntryType,
    decimal? NumericValue,
    string? TextValue,
    string? PositiveNegativeValue,
    string? FormulaExpression,
    string? FormulaResult,
    string? ImageUrl,
    string? PdfAttachmentUrl
);

public record SampleWorkflowUpdateDto(string? Notes = null);
public record ResultVerificationDto(string VerifiedBy, string? Remarks = null);
public record ResultAuthenticationDto(string AuthenticatedBy, string? DigitalSignature = null, string? Remarks = null);
