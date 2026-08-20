using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VetLims.Infrastructure;
using VetLims.Infrastructure.Entities;

namespace VetLims.WebApi.Reporting;

public enum ResultReportFormat
{
    Pdf,
    Png,
    Jpg
}

public sealed record ResultReportFile(string FileName, string ContentType, byte[] Content);

public sealed class ResultReportService
{
    private readonly VetLimsDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ResultReportService(VetLimsDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<ResultReportFile?> GenerateAsync(int registrationItemId, string? format, CancellationToken cancellationToken = default)
    {
        var reportFormat = ParseFormat(format);
        var model = await BuildModelAsync(registrationItemId, cancellationToken);
        if (model is null)
            return null;

        var document = new ResultReportDocument(model);
        var fileNameBase = $"result-{SanitizeFileName(model.ItemBarcode)}";

        return reportFormat switch
        {
            ResultReportFormat.Pdf => new ResultReportFile($"{fileNameBase}.pdf", "application/pdf", document.GeneratePdf()),
            ResultReportFormat.Png => new ResultReportFile($"{fileNameBase}.png", "image/png", GenerateImage(document, QuestPDF.Infrastructure.ImageFormat.Png)),
            ResultReportFormat.Jpg => new ResultReportFile($"{fileNameBase}.jpg", "image/jpeg", GenerateImage(document, QuestPDF.Infrastructure.ImageFormat.Jpeg)),
            _ => throw new InvalidOperationException("Unsupported report format.")
        };
    }

    private static ResultReportFormat ParseFormat(string? format)
    {
        return format?.Trim().ToLowerInvariant() switch
        {
            null or "" or "pdf" => ResultReportFormat.Pdf,
            "png" => ResultReportFormat.Png,
            "jpg" or "jpeg" => ResultReportFormat.Jpg,
            _ => throw new ArgumentException("Supported report formats are pdf, png, and jpg.")
        };
    }

    private static byte[] GenerateImage(IDocument document, QuestPDF.Infrastructure.ImageFormat imageFormat)
    {
        var images = document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = imageFormat,
            RasterDpi = 144
        }).ToList();

        if (images.Count == 0)
            throw new InvalidOperationException("QuestPDF did not generate any images for the report.");

        return images[0];
    }

    private async Task<ResultReportModel?> BuildModelAsync(int registrationItemId, CancellationToken cancellationToken)
    {
        var item = await _db.RegistrationItems
            .AsNoTracking()
            .Include(x => x.Laboratory)
            .Include(x => x.SampleType)
            .Include(x => x.Species)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Owner)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Doctor)
                    .ThenInclude(x => x!.Department)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Animals)
                    .ThenInclude(x => x.Animal)
                        .ThenInclude(x => x!.Species)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Animals)
                    .ThenInclude(x => x.Animal)
                        .ThenInclude(x => x!.Breed)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Animals)
                    .ThenInclude(x => x.Animal)
                        .ThenInclude(x => x!.Gender)
            .Include(x => x.Registration)
                .ThenInclude(x => x.Animals)
                    .ThenInclude(x => x.Animal)
                        .ThenInclude(x => x!.Color)
            .FirstOrDefaultAsync(x => x.Id == registrationItemId, cancellationToken);

        if (item is null || item.ItemType != RegItemType.Test)
            return null;

        var result = await _db.Results
            .AsNoTracking()
            .Include(x => x.Values)
                .ThenInclude(x => x.TestParameter)
            .FirstOrDefaultAsync(x => x.RegistrationItemId == registrationItemId, cancellationToken);

        if (result is null)
            return null;

        var test = await _db.Tests
            .AsNoTracking()
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == item.ItemId, cancellationToken);

        if (test is null)
            return null;

        var laboratory = item.Laboratory;
        if (laboratory is null && result.LaboratoryId.HasValue)
            laboratory = await _db.Laboratories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == result.LaboratoryId.Value, cancellationToken);
        laboratory ??= await _db.Laboratories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

        var qrCodeBytes = BuildQrCodeBytes(item, result);
        var logoAsset = ResolveAsset(laboratory?.Logo);
        var signatureAsset = ResolveAsset(result.DigitalSignature ?? laboratory?.DigitalSignature);

        var animals = item.Registration.Animals
            .OrderBy(x => x.Id)
            .Select(x =>
            {
                var animal = x.Animal;
                var dob = animal?.DateOfBirth?.ToString("yyyy-MM-dd");
                var ageText = x.AgeInDays.HasValue ? $"{x.AgeInDays.Value} days" : null;

                return new ResultReportAnimal(
                    animal?.Code,
                    x.AnimalName,
                    animal?.Species?.Name ?? item.Species?.Name,
                    animal?.Breed?.Name,
                    animal?.Gender?.Name,
                    dob,
                    ageText,
                    animal?.WeightKg,
                    animal?.Color?.Name,
                    animal?.MicrochipNumber,
                    animal?.VaccinationStatus);
            })
            .ToList();

        var rows = result.Values
            .OrderBy(x => x.TestParameter.SortOrder)
            .ThenBy(x => x.TestParameter.Name)
            .Select(x => new ResultReportRow(
                x.TestParameter.Name,
                FormatValue(x),
                x.UnitSymbol,
                FormatReferenceRange(x),
                FormatFlag(x),
                x.EntryType == ResultEntryType.Image ? ResolveAsset(x.ImageUrl) : null,
                x.EntryType == ResultEntryType.Pdf ? x.PdfAttachmentUrl : null))
            .ToList();

        return new ResultReportModel(
            ClinicName: laboratory?.Name ?? "Clinic",
            ClinicAddress: laboratory?.Address,
            ClinicPhone: laboratory?.Phone,
            ClinicEmail: laboratory?.Email,
            ClinicHeader: laboratory?.ReportHeader,
            ClinicFooter: laboratory?.ReportFooter,
            ClinicLogo: logoAsset,
            Signature: signatureAsset,
            SignatureLabel: result.PathologistAuthenticatedBy ?? result.TechnicianVerifiedBy,
            OwnerCode: item.Registration.Owner?.Code,
            OwnerName: item.Registration.Owner?.Name ?? item.Registration.OwnerName,
            OwnerMobile: item.Registration.Owner?.Mobile,
            OwnerWhatsApp: item.Registration.Owner?.WhatsApp,
            OwnerEmail: item.Registration.Owner?.Email,
            OwnerAddress: item.Registration.Owner?.Address,
            OwnerCity: item.Registration.Owner?.City,
            Animals: animals,
            DoctorName: item.Registration.Doctor?.Name,
            DoctorCode: item.Registration.Doctor?.Code,
            DoctorDepartment: item.Registration.Doctor?.Department?.Name,
            TestName: test.Name,
            TestCode: test.Code,
            DepartmentName: test.Department?.Name,
            RegistrationBarcode: item.Registration.Barcode,
            ItemBarcode: item.ItemBarcode,
            SampleStatus: item.SampleStatus.ToString(),
            CollectedAt: item.SampleCollectedAt,
            ReceivedAt: item.SampleReceivedAt,
            ProcessedAt: item.SampleProcessedAt,
            ReportedAt: result.CompletedAt ?? result.ModifiedDate ?? result.CreatedDate,
            Interpretation: result.Notes,
            VerificationRemarks: result.VerificationRemarks,
            TechnicianVerifiedBy: result.TechnicianVerifiedBy,
            TechnicianVerifiedAt: result.TechnicianVerifiedAt,
            PathologistAuthenticatedBy: result.PathologistAuthenticatedBy,
            PathologistAuthenticatedAt: result.PathologistAuthenticatedAt,
            QrCodeBytes: qrCodeBytes,
            Rows: rows);
    }

    private static string FormatValue(ResultValue value)
    {
        return value.EntryType switch
        {
            ResultEntryType.Numeric => value.NumericValue?.ToString("0.##") ?? string.Empty,
            ResultEntryType.Text => value.TextValue ?? string.Empty,
            ResultEntryType.PositiveNegative => value.PositiveNegativeValue ?? string.Empty,
            ResultEntryType.Formula => !string.IsNullOrWhiteSpace(value.FormulaResult)
                ? value.FormulaResult
                : value.NumericValue?.ToString("0.##") ?? string.Empty,
            ResultEntryType.Image => "Image attachment",
            ResultEntryType.Pdf => "PDF attachment",
            _ => value.TextValue ?? value.NumericValue?.ToString("0.##") ?? string.Empty
        };
    }

    private static string FormatReferenceRange(ResultValue value)
    {
        if (value.RangeLow is null && value.RangeHigh is null)
            return "—";

        var range = $"{value.RangeLow?.ToString("0.##") ?? string.Empty} - {value.RangeHigh?.ToString("0.##") ?? string.Empty}".Trim();
        if (value.RangeMinDays is null && value.RangeMaxDays is null)
            return range;

        return $"{range} ({value.RangeMinDays?.ToString() ?? "-"}–{value.RangeMaxDays?.ToString() ?? "-"} d)";
    }

    private static string FormatFlag(ResultValue value)
    {
        if (value.IsLow == true) return "LOW";
        if (value.IsHigh == true) return "HIGH";
        if (value.IsOutOfRange == true) return "OUT";
        return string.Empty;
    }

    private static byte[] BuildQrCodeBytes(RegistrationItem item, Result result)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(
            $"RESULT|REG:{item.RegistrationId}|ITEM:{item.Id}|BARCODE:{item.ItemBarcode}|STATUS:{item.Status}|REPORTED:{result.CompletedAt?.ToString("O") ?? string.Empty}",
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(10);
    }

    private ReportAsset? ResolveAsset(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var trimmed = source.Trim();

        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex > 0)
            {
                var base64 = trimmed[(commaIndex + 1)..];
                return new ReportAsset(trimmed, Convert.FromBase64String(base64));
            }
        }

        var path = ResolvePath(trimmed);
        if (path is not null && File.Exists(path))
            return new ReportAsset(trimmed, File.ReadAllBytes(path));

        return new ReportAsset(trimmed, null);
    }

    private string? ResolvePath(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.IsFile)
                return absoluteUri.LocalPath;

            return null;
        }

        if (Path.IsPathRooted(source))
            return source;

        var normalized = source.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.ContentRootPath, normalized);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
    }
}

internal sealed record ReportAsset(string Source, byte[]? Content);

internal sealed record ResultReportModel(
    string ClinicName,
    string? ClinicAddress,
    string? ClinicPhone,
    string? ClinicEmail,
    string? ClinicHeader,
    string? ClinicFooter,
    ReportAsset? ClinicLogo,
    ReportAsset? Signature,
    string? SignatureLabel,
    string? OwnerCode,
    string OwnerName,
    string? OwnerMobile,
    string? OwnerWhatsApp,
    string? OwnerEmail,
    string? OwnerAddress,
    string? OwnerCity,
    IReadOnlyList<ResultReportAnimal> Animals,
    string? DoctorName,
    string? DoctorCode,
    string? DoctorDepartment,
    string TestName,
    string? TestCode,
    string? DepartmentName,
    string RegistrationBarcode,
    string ItemBarcode,
    string SampleStatus,
    DateTime? CollectedAt,
    DateTime? ReceivedAt,
    DateTime? ProcessedAt,
    DateTime? ReportedAt,
    string? Interpretation,
    string? VerificationRemarks,
    string? TechnicianVerifiedBy,
    DateTime? TechnicianVerifiedAt,
    string? PathologistAuthenticatedBy,
    DateTime? PathologistAuthenticatedAt,
    byte[] QrCodeBytes,
    IReadOnlyList<ResultReportRow> Rows);

internal sealed record ResultReportAnimal(
    string? Code,
    string Name,
    string? Species,
    string? Breed,
    string? Gender,
    string? DateOfBirth,
    string? Age,
    decimal? WeightKg,
    string? Color,
    string? Microchip,
    string? VaccinationStatus);

internal sealed record ResultReportRow(
    string ParameterName,
    string Value,
    string? Unit,
    string ReferenceRange,
    string Flag,
    ReportAsset? Image,
    string? PdfAttachmentUrl);

internal sealed class ResultReportDocument : IDocument
{
    private readonly ResultReportModel _model;

    public ResultReportDocument(ResultReportModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(ComposeVisitSummary);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(ComposeOwnerDetails);
                    row.ConstantItem(220).Element(ComposeDoctorDetails);
                });
                column.Item().Element(ComposeAnimalDetails);
                column.Item().Element(ComposeResultTable);
                column.Item().Element(ComposeInterpretation);
                if (_model.Rows.Any(x => x.Image is not null || !string.IsNullOrWhiteSpace(x.PdfAttachmentUrl)))
                    column.Item().Element(ComposeAttachments);
            });
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            if (_model.ClinicLogo?.Content is not null)
            {
                row.ConstantItem(70).Height(70).Image(_model.ClinicLogo.Content);
                row.Spacing(12);
            }

            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_model.ClinicName).SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                if (!string.IsNullOrWhiteSpace(_model.ClinicHeader))
                    column.Item().Text(_model.ClinicHeader);
                if (!string.IsNullOrWhiteSpace(_model.ClinicAddress))
                    column.Item().Text(_model.ClinicAddress);
                var contact = string.Join(" | ", new[] { _model.ClinicPhone, _model.ClinicEmail }.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.IsNullOrWhiteSpace(contact))
                    column.Item().Text(contact);
            });

            row.ConstantItem(90).Column(column =>
            {
                column.Item().AlignRight().Text("Lab Report").SemiBold().FontSize(16);
                column.Item().AlignRight().PaddingTop(6).Height(70).Image(_model.QrCodeBytes);
            });
        });
    }

    private void ComposeVisitSummary(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"{_model.TestName}{(!string.IsNullOrWhiteSpace(_model.TestCode) ? $" ({_model.TestCode})" : string.Empty)}").SemiBold().FontSize(12);
            column.Item().Text($"Registration Barcode: {_model.RegistrationBarcode}   |   Sample Barcode: {_model.ItemBarcode}");
            column.Item().Text($"Department: {_model.DepartmentName ?? "—"}   |   Sample Status: {_model.SampleStatus}");

            var timeline = new[]
            {
                FormatDate("Collected", _model.CollectedAt),
                FormatDate("Received", _model.ReceivedAt),
                FormatDate("Processed", _model.ProcessedAt),
                FormatDate("Reported", _model.ReportedAt)
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            column.Item().Text(string.Join("   |   ", timeline));
        });
    }

    private void ComposeOwnerDetails(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Owner Details").SemiBold();
            column.Item().Text($"Name: {_model.OwnerName}");
            if (!string.IsNullOrWhiteSpace(_model.OwnerCode))
                column.Item().Text($"Code: {_model.OwnerCode}");
            if (!string.IsNullOrWhiteSpace(_model.OwnerMobile))
                column.Item().Text($"Mobile: {_model.OwnerMobile}");
            if (!string.IsNullOrWhiteSpace(_model.OwnerWhatsApp))
                column.Item().Text($"WhatsApp: {_model.OwnerWhatsApp}");
            if (!string.IsNullOrWhiteSpace(_model.OwnerEmail))
                column.Item().Text($"Email: {_model.OwnerEmail}");
            if (!string.IsNullOrWhiteSpace(_model.OwnerAddress) || !string.IsNullOrWhiteSpace(_model.OwnerCity))
                column.Item().Text($"Address: {string.Join(", ", new[] { _model.OwnerAddress, _model.OwnerCity }.Where(x => !string.IsNullOrWhiteSpace(x)))}");
        });
    }

    private void ComposeDoctorDetails(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Doctor Details").SemiBold();
            column.Item().Text($"Doctor: {_model.DoctorName ?? "—"}");
            if (!string.IsNullOrWhiteSpace(_model.DoctorCode))
                column.Item().Text($"Code: {_model.DoctorCode}");
            if (!string.IsNullOrWhiteSpace(_model.DoctorDepartment))
                column.Item().Text($"Department: {_model.DoctorDepartment}");
            if (!string.IsNullOrWhiteSpace(_model.TechnicianVerifiedBy))
                column.Item().Text($"Technician Verified: {_model.TechnicianVerifiedBy} {FormatInlineDate(_model.TechnicianVerifiedAt)}");
            if (!string.IsNullOrWhiteSpace(_model.PathologistAuthenticatedBy))
                column.Item().Text($"Pathologist Authenticated: {_model.PathologistAuthenticatedBy} {FormatInlineDate(_model.PathologistAuthenticatedAt)}");
        });
    }

    private void ComposeAnimalDetails(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Animal Details").SemiBold();

            foreach (var animal in _model.Animals)
            {
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(8).Column(inner =>
                {
                    inner.Spacing(2);
                    inner.Item().Text($"{animal.Name}{(!string.IsNullOrWhiteSpace(animal.Code) ? $" ({animal.Code})" : string.Empty)}").SemiBold();
                    inner.Item().Text(string.Join(" | ", new[]
                    {
                        animal.Species,
                        animal.Breed,
                        animal.Gender,
                        animal.Color
                    }.Where(x => !string.IsNullOrWhiteSpace(x))));

                    var traits = new List<string>();
                    if (!string.IsNullOrWhiteSpace(animal.DateOfBirth)) traits.Add($"DOB: {animal.DateOfBirth}");
                    if (!string.IsNullOrWhiteSpace(animal.Age)) traits.Add($"Age: {animal.Age}");
                    if (animal.WeightKg.HasValue) traits.Add($"Weight: {animal.WeightKg.Value:0.##} kg");
                    if (!string.IsNullOrWhiteSpace(animal.Microchip)) traits.Add($"Microchip: {animal.Microchip}");
                    if (!string.IsNullOrWhiteSpace(animal.VaccinationStatus)) traits.Add($"Vaccination: {animal.VaccinationStatus}");
                    if (traits.Count > 0)
                        inner.Item().Text(string.Join(" | ", traits));
                });
            }
        });
    }

    private void ComposeResultTable(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Results").SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Parameter");
                    HeaderCell(header, "Result");
                    HeaderCell(header, "Unit");
                    HeaderCell(header, "Reference Range");
                    HeaderCell(header, "Flag");
                });

                foreach (var row in _model.Rows)
                {
                    BodyCell(table, row.ParameterName);
                    BodyCell(table, row.Value);
                    BodyCell(table, row.Unit ?? "—");
                    BodyCell(table, row.ReferenceRange);
                    BodyCell(table, row.Flag);
                }
            });
        });
    }

    private void ComposeInterpretation(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Interpretation").SemiBold();
            column.Item().Text(string.IsNullOrWhiteSpace(_model.Interpretation) ? "Not provided." : _model.Interpretation);
            if (!string.IsNullOrWhiteSpace(_model.VerificationRemarks))
            {
                column.Item().PaddingTop(4).Text("Verification Remarks").SemiBold();
                column.Item().Text(_model.VerificationRemarks);
            }
        });
    }

    private void ComposeAttachments(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Images & Attachments").SemiBold();

            foreach (var row in _model.Rows.Where(x => x.Image is not null || !string.IsNullOrWhiteSpace(x.PdfAttachmentUrl)))
            {
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(8).Column(inner =>
                {
                    inner.Spacing(4);
                    inner.Item().Text(row.ParameterName).SemiBold();

                    if (row.Image?.Content is not null)
                    {
                        inner.Item().MaxHeight(180).Image(row.Image.Content);
                    }
                    else if (!string.IsNullOrWhiteSpace(row.Image?.Source))
                    {
                        inner.Item().Text($"Image source: {row.Image.Source}");
                    }

                    if (!string.IsNullOrWhiteSpace(row.PdfAttachmentUrl))
                        inner.Item().Text($"PDF attachment: {row.PdfAttachmentUrl}");
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(_model.ClinicFooter))
                    column.Item().Text(_model.ClinicFooter).FontSize(9);
                column.Item().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(180).AlignRight().Column(column =>
            {
                column.Item().Text("Digital Signature").SemiBold().AlignRight();
                if (_model.Signature?.Content is not null)
                    column.Item().Height(50).AlignRight().Image(_model.Signature.Content);
                else if (!string.IsNullOrWhiteSpace(_model.Signature?.Source))
                    column.Item().Text(_model.Signature.Source).FontSize(8).AlignRight();

                if (!string.IsNullOrWhiteSpace(_model.SignatureLabel))
                    column.Item().Text(_model.SignatureLabel).FontSize(9).AlignRight();
            });
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4).PaddingHorizontal(6).Text(text).SemiBold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(6).Text(text);
    }

    private static string FormatDate(string label, DateTime? value)
    {
        return value.HasValue ? $"{label}: {value.Value:yyyy-MM-dd HH:mm}" : string.Empty;
    }

    private static string FormatInlineDate(DateTime? value)
    {
        return value.HasValue ? $"({value.Value:yyyy-MM-dd HH:mm})" : string.Empty;
    }
}
