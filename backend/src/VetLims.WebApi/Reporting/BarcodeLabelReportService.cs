using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VetLims.Infrastructure;
using ZXing;
using ZXing.Common;

namespace VetLims.WebApi.Reporting;

public sealed record BarcodeLabelFile(string FileName, string ContentType, byte[] Content);

public sealed class BarcodeLabelReportService
{
    private readonly VetLimsDbContext _db;

    public BarcodeLabelReportService(VetLimsDbContext db)
    {
        _db = db;
    }

    public async Task<BarcodeLabelFile?> GenerateForRegistrationAsync(int registrationId, int count = 1, CancellationToken ct = default)
    {
        var animals = await _db.Registrations.AsNoTracking()
            .Where(r => r.Id == registrationId)
            .SelectMany(r => r.Animals.OrderBy(a => a.Id).Select(a => new BarcodeLabelModel
            {
                OwnerName = r.OwnerName,
                OwnerCode = r.Owner != null ? r.Owner.Code : null,
                AnimalName = a.AnimalName,
                Barcode = a.Barcode ?? string.Empty,
                SpeciesName = a.Species.Name,
                RegisteredAt = r.CreatedAt,
            }))
            .ToListAsync(ct);

        if (animals.Count == 0) return null;

        var repeated = animals.SelectMany(a => Enumerable.Repeat(a, Math.Max(count, 1))).ToList();
        var document = new BarcodeLabelDocument(repeated);
        var bytes = document.GeneratePdf();
        return new BarcodeLabelFile($"barcodes-reg-{registrationId}.pdf", "application/pdf", bytes);
    }

    public async Task<BarcodeLabelFile?> GenerateForAnimalAsync(int registrationAnimalId, int count = 1, CancellationToken ct = default)
    {
        var animal = await _db.RegistrationAnimals.AsNoTracking()
            .Where(a => a.Id == registrationAnimalId)
            .Select(a => new BarcodeLabelModel
            {
                OwnerName = a.Registration.OwnerName,
                OwnerCode = a.Registration.Owner != null ? a.Registration.Owner.Code : null,
                AnimalName = a.AnimalName,
                Barcode = a.Barcode ?? string.Empty,
                SpeciesName = a.Species.Name,
                RegisteredAt = a.Registration.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (animal is null) return null;

        var repeated = Enumerable.Repeat(animal, Math.Max(count, 1)).ToList();
        var document = new BarcodeLabelDocument(repeated);
        var bytes = document.GeneratePdf();
        return new BarcodeLabelFile($"barcode-{animal.Barcode}.pdf", "application/pdf", bytes);
    }
}

internal sealed record BarcodeLabelModel
{
    public string OwnerName { get; init; } = string.Empty;
    public string? OwnerCode { get; init; }
    public string AnimalName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string SpeciesName { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}

internal sealed class BarcodeLabelDocument : IDocument
{
    private readonly IReadOnlyList<BarcodeLabelModel> _labels;

    public BarcodeLabelDocument(IReadOnlyList<BarcodeLabelModel> labels)
    {
        _labels = labels;
    }

    public DocumentMetadata GetMetadata() => new() { Title = "Barcode Labels" };

    public void Compose(IDocumentContainer container)
    {
        foreach (var label in _labels)
        {
            container.Page(page =>
            {
                page.Size(2, 1, Unit.Inch);
                page.Margin(0.07f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                // Two-column layout keeps everything on a single physical label:
                // left = identity text block, right = scannable barcode.
                page.Content().Row(row =>
                {
                    row.ConstantItem(92).Column(text =>
                    {
                        text.Item().Text(t =>
                        {
                            t.Span(label.AnimalName).SemiBold().FontSize(8);
                            if (!string.IsNullOrWhiteSpace(label.SpeciesName))
                            {
                                t.Span(" · ").FontSize(7).FontColor(Colors.Grey.Darken1);
                                t.Span(label.SpeciesName).FontSize(7).FontColor(Colors.Grey.Darken2);
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(label.OwnerCode))
                            text.Item().PaddingTop(1).Text(label.OwnerCode).FontSize(6).FontColor(Colors.Grey.Darken1);

                        text.Item().PaddingTop(2).Text($"Barcode: {label.Barcode}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                        text.Item().Text($"Date: {label.RegisteredAt:dd-MMM-yyyy hh:mm tt}").FontSize(6).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().AlignCenter().AlignMiddle().MaxHeight(30).MaxWidth(84)
                        .Image(GenerateBarcodeImage(label.Barcode));
                });
            });
        }
    }

    private static byte[] GenerateBarcodeImage(string barcodeValue)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            return Array.Empty<byte>();

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = 80,
                Width = 280,
                Margin = 0,
                PureBarcode = true,
            }
        };

        var result = writer.Write(barcodeValue);
        return PngEncoder.Encode(result.Pixels, result.Width, result.Height);
    }
}

internal static class PngEncoder
{
    public static byte[] Encode(byte[] pixels, int width, int height)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // PNG signature
        bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR
        WriteChunk(bw, "IHDR", BuildIhdr(width, height));
        // IDAT
        WriteChunk(bw, "IDAT", BuildIdat(pixels, width, height));
        // IEND
        WriteChunk(bw, "IEND", Array.Empty<byte>());

        return ms.ToArray();
    }

    private static void WriteChunk(BinaryWriter bw, string type, byte[] data)
    {
        var lengthBytes = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        bw.Write(lengthBytes);
        bw.Write(System.Text.Encoding.ASCII.GetBytes(type));
        bw.Write(data);
        var crc = Crc32(type, data);
        var crcBytes = BitConverter.GetBytes(crc);
        if (BitConverter.IsLittleEndian) Array.Reverse(crcBytes);
        bw.Write(crcBytes);
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        WriteBigEndian(bw, width);
        WriteBigEndian(bw, height);
        bw.Write((byte)8);  // bit depth
        bw.Write((byte)2);  // color type: RGB
        bw.Write((byte)0);  // compression
        bw.Write((byte)0);  // filter
        bw.Write((byte)0);  // interlace
        return ms.ToArray();
    }

    private static byte[] BuildIdat(byte[] pixels, int width, int height)
    {
        using var inputMs = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            inputMs.WriteByte(0); // filter: None
            var offset = y * width * 3;
            inputMs.Write(pixels, offset, width * 3);
        }

        var raw = inputMs.ToArray();
        using var outputMs = new MemoryStream();
        using (var deflate = new System.IO.Compression.DeflateStream(outputMs, System.IO.Compression.CompressionLevel.Optimal))
        {
            deflate.Write(raw, 0, raw.Length);
        }
        return outputMs.ToArray();
    }

    private static void WriteBigEndian(BinaryWriter bw, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        bw.Write(bytes);
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (var n = 0; n < 256; n++)
        {
            var c = (uint)n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(string type, byte[] data)
    {
        var crc = 0xFFFFFFFF;
        foreach (var b in System.Text.Encoding.ASCII.GetBytes(type))
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFF;
    }
}
