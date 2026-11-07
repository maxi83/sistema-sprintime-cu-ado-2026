using System.Globalization;
using System.Text;
using lucho_sprintime.Models;

namespace lucho_sprintime.Services;

public class ExportService : IExportService
{
    public byte[] BuildCsv(Event race, IReadOnlyCollection<Registration> registrations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Dorsal;Apellido;Nombre;DNI;Email;Telefono;Distancia;Categoria;Pago;Importe;Estado;QR");
        foreach (var r in registrations.OrderBy(x => BibSort(x.BibNumber)).ThenBy(x => x.Cyclist.LastName))
        {
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(r.BibNumber ?? ""), Csv(r.Cyclist.LastName), Csv(r.Cyclist.FirstName), Csv(r.Cyclist.Dni), Csv(r.Cyclist.Email), Csv(r.Cyclist.Phone),
                Csv(r.EventDistance.Name), Csv(r.Category.Name), Csv(r.PaymentStatus.ToString()), r.AmountPaid.ToString("0.00", CultureInfo.InvariantCulture), Csv(r.CheckInStatus.ToString()), r.QrToken.ToString()
            }));
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] BuildTxt(Event race, IReadOnlyCollection<Registration> registrations)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SPRINTIME|{race.Name}|{race.StartDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine("DORSAL|DNI|APELLIDO|NOMBRE|DISTANCIA|CATEGORIA|PAGO|QR");
        foreach (var r in registrations.OrderBy(x => BibSort(x.BibNumber)).ThenBy(x => x.Cyclist.LastName))
            sb.AppendLine($"{Clean(r.BibNumber)}|{Clean(r.Cyclist.Dni)}|{Clean(r.Cyclist.LastName)}|{Clean(r.Cyclist.FirstName)}|{Clean(r.EventDistance.Name)}|{Clean(r.Category.Name)}|{r.PaymentStatus}|{r.QrToken}");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] BuildPdf(Event race, IReadOnlyCollection<Registration> registrations)
    {
        var lines = new List<string>
        {
            "SPRINTIME - NOMINA DE INSCRIPTOS",
            race.Name,
            $"Fecha: {race.StartDate:dd/MM/yyyy HH:mm}   Lugar: {race.City}, {race.Province}",
            $"Cierre: {race.RegistrationCloseAt:dd/MM/yyyy HH:mm}   Confirmados: {registrations.Count}",
            "",
            "Dorsal   Apellido y nombre                 DNI          Distancia      Categoria"
        };
        foreach (var r in registrations.OrderBy(x => BibSort(x.BibNumber)).ThenBy(x => x.Cyclist.LastName))
        {
            var name = $"{r.Cyclist.LastName}, {r.Cyclist.FirstName}";
            lines.Add($"{Fit(r.BibNumber ?? "-", 8)} {Fit(name, 34)} {Fit(r.Cyclist.Dni, 12)} {Fit(r.EventDistance.Name, 14)} {Fit(r.Category.Name, 18)}");
        }
        lines.Add("");
        lines.Add("Generado por SprinTime · Solo pagos confirmados salvo configuracion expresa del organizador.");
        return SimplePdf(lines);
    }

    public byte[] BuildDiploma(Result result)
    {
        var r = result.Registration;
        var lines = new List<string>
        {
            "SPRINTIME",
            "DIPLOMA FINISHER",
            "",
            r.Event.Name.ToUpperInvariant(),
            r.Event.StartDate.ToString("dd/MM/yyyy") + " - " + r.Event.City + ", " + r.Event.Province,
            "",
            (r.Cyclist.FirstName + " " + r.Cyclist.LastName).ToUpperInvariant(),
            "Dorsal " + (r.BibNumber ?? "-") + " - " + r.EventDistance.Name + " - " + r.Category.Name,
            "",
            "TIEMPO OFICIAL: " + (result.TotalTime?.ToString(@"hh\:mm\:ss") ?? "-"),
            "POSICION GENERAL: #" + (result.GeneralPosition?.ToString() ?? "-"),
            "POSICION CATEGORIA: #" + (result.CategoryPosition?.ToString() ?? "-"),
            "",
            "Resultado verificado por SprinTime",
            "ID: ST-R-" + result.Id.ToString("D6")
        };
        return SimplePdf(lines);
    }

    private static byte[] SimplePdf(IReadOnlyList<string> input)
    {
        var pages = input.Chunk(48).ToArray();
        var objects = new List<string> { "" };
        int catalogId = Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add(""); // pages placeholder at #2
        int fontId = Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var pageIds = new List<int>();

        foreach (var pageLines in pages)
        {
            var stream = new StringBuilder("BT\n/F1 10 Tf\n50 790 Td\n");
            foreach (var line in pageLines)
            {
                stream.Append('(').Append(PdfEscape(line)).Append(") Tj\n0 -15 Td\n");
            }
            stream.Append("ET");
            var content = stream.ToString();
            var contentId = Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            var pageId = Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
            pageIds.Add(pageId);
        }
        objects[2] = $"<< /Type /Pages /Count {pageIds.Count} /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] >>";

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, true) { NewLine = "\n" };
        writer.Write("%PDF-1.4\n"); writer.Flush();
        var offsets = new List<long> { 0 };
        for (var i = 1; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            writer.Write($"{i} 0 obj\n{objects[i]}\nendobj\n"); writer.Flush();
        }
        var xref = ms.Position;
        writer.Write($"xref\n0 {objects.Count}\n0000000000 65535 f \n");
        for (var i = 1; i < objects.Count; i++) writer.Write($"{offsets[i]:0000000000} 00000 n \n");
        writer.Write($"trailer\n<< /Size {objects.Count} /Root {catalogId} 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return ms.ToArray();

        int Add(string value) { objects.Add(value); return objects.Count - 1; }
    }

    private static string PdfEscape(string s) => RemoveDiacritics(s).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c <= 127).ToArray());
    }
    private static string Fit(string value, int width) => value.Length > width ? value[..Math.Max(0, width - 1)] + "~" : value.PadRight(width);
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static string Clean(string? value) => (value ?? "").Replace("|", " ").Replace("\r", " ").Replace("\n", " ");
    private static int BibSort(string? bib) => int.TryParse(bib, out var n) ? n : int.MaxValue;
}
