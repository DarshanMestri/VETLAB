
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

public record LaboratoryDto(int Id, int OrganizationId, string? OrganizationName, string Code, string Name, string? Logo, string? Address, string? Phone, string? Email, string? Gst, string? RegistrationNumber, string? WhatsAppNumber, string? ReportHeader, string? ReportFooter, string? DigitalSignature, bool IsActive);
public record LaboratoryCreateDto(string Code, string Name, string? Logo = null, string? Address = null, string? Phone = null, string? Email = null, string? Gst = null, string? RegistrationNumber = null, string? WhatsAppNumber = null, string? ReportHeader = null, string? ReportFooter = null, string? DigitalSignature = null, bool IsActive = true, int? OrganizationId = null);
public record LaboratoryUpdateDto(string Code, string Name, string? Logo = null, string? Address = null, string? Phone = null, string? Email = null, string? Gst = null, string? RegistrationNumber = null, string? WhatsAppNumber = null, string? ReportHeader = null, string? ReportFooter = null, string? DigitalSignature = null, bool IsActive = true);

public record ClinicDto(int Id, int OrganizationId, string? OrganizationName, string Code, string Name, string? ShortName, string? Phone, string? Email, string? Address, string? Logo, string? LicenseNumber, bool IsActive);
public record ClinicCreateDto(string Code, string Name, string? ShortName = null, string? Phone = null, string? Email = null, string? Address = null, string? Logo = null, string? LicenseNumber = null, bool IsActive = true, int? OrganizationId = null);
public record ClinicUpdateDto(string Code, string Name, string? ShortName = null, string? Phone = null, string? Email = null, string? Address = null, string? Logo = null, string? LicenseNumber = null, bool IsActive = true);

public record BranchDto(int Id, int OrganizationId, string? OrganizationName, int ClinicId, int LaboratoryId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive, string? ClinicName = null, string? LaboratoryName = null);
public record BranchCreateDto(int ClinicId, int LaboratoryId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive = true, int? OrganizationId = null);
public record BranchUpdateDto(int ClinicId, int LaboratoryId, string Code, string Name, string? Address, string? Phone, string? Email, bool IsActive = true, int? OrganizationId = null);
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
public record TestDto(int Id, string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, int? SampleTypeId, TestValueType ValueType, bool IsActive);
public record TestCreateDto(string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, int? SampleTypeId, TestValueType ValueType, bool IsActive);
public record TestUpdateDto(string Name, string? DisplayName, string? Code, int DepartmentId, int? UnitId, int? SampleTypeId, TestValueType ValueType, bool IsActive);

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
    string? OwnerNameAr = null,
    int? ClientId = null,
    int? OwnerId = null,
    int? DoctorId = null,
    string? ReferenceNumber = null,
    string? ClinicalNotes = null,
    List<RegistrationAnimalDto> Animals = null!,
    List<RegistrationItemCreateDto> Items = null!,
    int? BranchId = null,
    decimal? InitialPaymentAmount = null,
    PaymentMethod? InitialPaymentMethod = null,
    string? InitialPaymentRef = null,
    string? OwnerMobile = null,
    DiscountType? DiscountType = null,
    decimal DiscountValue = 0,
    int? AgeValue = null,
    AgeUnit? AgeUnit = null
);

public record RegistrationDto(
    int Id,
    string OwnerName,
    string? OwnerNameAr,
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
    string? OwnerMobile = null,
    decimal DiscountAmount = 0,
    decimal DiscountValue = 0,
    int? DiscountType = null,
    int? AgeValue = null,
    int? AgeUnit = null,
    string PaymentStatus = "Unpaid",
    DateTime? CancelledAt = null,
    string? CancellationReason = null
);

public record RegistrationAnimalDto(int SpeciesId, string? AnimalName, int? AnimalId = null, int? AgeInDays = null);

public record RegistrationItemCreateDto(RegItemType ItemType, int ItemId, int? SpeciesId, int? SampleTypeId = null, int? RegistrationItemId = null, bool IsNew = false);

public record RegistrationDetailDto(
    int Id,
    string OwnerName,
    string? OwnerNameAr,
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
    string? OwnerMobile = null,
    decimal DiscountAmount = 0,
    decimal DiscountValue = 0,
    int? DiscountType = null,
    int? AgeValue = null,
    int? AgeUnit = null,
    string PaymentStatus = "Unpaid",
    string? OwnerCode = null
);

public record RegistrationAnimalView(int Id, int SpeciesId, string? AnimalName, int? AnimalId, string? Barcode = null, int? AgeInDays = null, string? AnimalCode = null);
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
    string? OwnerNameAr,
    string? OwnerMobile,
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
    string? RerunReason = null
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

public record AnimalResultDto(
    int RegistrationAnimalId,
    int AnimalId,
    string AnimalName,
    string? SpeciesName,
    int? AgeInDays,
    IEnumerable<ResultValueDto> Values,
    bool IsVerified,
    bool IsAuthenticated,
    string? TechnicianVerifiedBy,
    DateTime? TechnicianVerifiedAt,
    string? PathologistAuthenticatedBy,
    DateTime? PathologistAuthenticatedAt,
    string? DigitalSignature,
    string? VerificationRemarks,
    string? Notes
);

public record ResultDetailDto(
    int RegistrationItemId,
    string ItemBarcode,
    string? Notes,
    string SampleStatus,
    string Status,
    IEnumerable<AnimalResultDto> AnimalResults,
    string? OwnerName = null,
    string? OwnerNameAr = null,
    string? OwnerMobile = null,
    string? OwnerEmail = null,
    string? OwnerCode = null,
    int? OwnerId = null,
    int? ClientId = null,
    string? RerunReason = null,
    string? RerunInitiatedBy = null,
    DateTime? RerunAt = null,
    IEnumerable<ResultOrderedItemDto>? OrderedTests = null
);

public record ResultOrderedItemDto(
    int RegistrationItemId,
    int ItemType,
    int? TestId,
    string ItemName,
    string? ItemCode,
    string Status,
    bool IsCurrent
);

public record TestEntryGroupDto(
    int RegistrationItemId,
    int? TestId,
    string TestName,
    string? ItemCode,
    string ItemBarcode,
    string Status,
    bool IsVerified,
    bool IsAuthenticated,
    string? TechnicianVerifiedBy,
    DateTime? TechnicianVerifiedAt,
    string? PathologistAuthenticatedBy,
    DateTime? PathologistAuthenticatedAt,
    string? DigitalSignature,
    string? VerificationRemarks,
    string? Notes,
    IEnumerable<ResultValueDto> Values
);

public record AnimalEntryGroupDto(
    int RegistrationAnimalId,
    int AnimalId,
    string AnimalName,
    string? SpeciesName,
    int? AgeInDays,
    IEnumerable<TestEntryGroupDto> Tests
);

public record ResultEntryGroupDto(
    int RegistrationId,
    string RegistrationBarcode,
    string? OwnerName,
    string? OwnerNameAr,
    string? OwnerMobile,
    string? OwnerEmail,
    string? OwnerCode,
    IEnumerable<AnimalEntryGroupDto> Animals,
    IEnumerable<ResultOrderedItemDto> OrderedTests
);

// payload to save values
public record ResultEntryDto(
    int RegistrationItemId,
    int? RegistrationAnimalId,
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
public record ResultVerificationDto(int? RegistrationAnimalId, string? Remarks = null);
public record ResultAuthenticationDto(int? RegistrationAnimalId, string? DigitalSignature = null, string? Remarks = null);
public record ResultRerunDto(string? Reason = null, int RegistrationAnimalId = 0);

public record ResultReportQueryDto(int? AnimalId = null, bool Combined = false);
