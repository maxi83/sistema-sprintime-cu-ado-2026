using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

[ApiController]
[Route("MercadoPago")]
public class MercadoPagoController(ApplicationDbContext db, IMercadoPagoService mercadoPago, ILogger<MercadoPagoController> logger, IConfiguration configuration) : ControllerBase
{
    [HttpPost("Webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        string? paymentId = Request.Query["data.id"].FirstOrDefault();
        try
        {
            if (string.IsNullOrWhiteSpace(paymentId) && !string.IsNullOrWhiteSpace(raw))
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id)) paymentId = id.ToString();
            }
        }
        catch (JsonException ex) { logger.LogWarning(ex, "Webhook MP con JSON inválido"); }

        if (string.IsNullOrWhiteSpace(paymentId)) return Ok();

        var webhookSecret = configuration["MercadoPago:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            var signature = Request.Headers["x-signature"].FirstOrDefault();
            var requestId = Request.Headers["x-request-id"].FirstOrDefault();
            if (!IsValidSignature(signature, requestId, paymentId, webhookSecret))
            {
                logger.LogWarning("Webhook Mercado Pago rechazado por firma inválida. PaymentId: {PaymentId}", paymentId);
                return Unauthorized();
            }
        }

        var verified = await mercadoPago.GetPaymentAsync(paymentId, ct);
        if (verified is null || string.IsNullOrWhiteSpace(verified.ExternalReference)) return Ok();
        if (!verified.ExternalReference.StartsWith("SPRINTIME-REG-") || !int.TryParse(verified.ExternalReference[15..], out var registrationId)) return Ok();

        var registration = await db.Registrations.Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == registrationId, ct);
        if (registration is null) return Ok();
        var status = verified.Status.ToLowerInvariant() switch
        {
            "approved" => PaymentStatus.Approved,
            "rejected" => PaymentStatus.Rejected,
            "refunded" => PaymentStatus.Refunded,
            "cancelled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
        registration.PaymentStatus = status;
        registration.PaymentExternalId = verified.ExternalId;
        if (status == PaymentStatus.Approved)
        {
            registration.Status = RegistrationStatus.Confirmed;
            registration.AmountPaid = verified.Amount;
            registration.PaidAt ??= DateTime.UtcNow;
        }
        else if (status == PaymentStatus.Refunded) registration.Status = RegistrationStatus.Refunded;

        registration.Payment ??= new Payment { RegistrationId = registration.Id, Amount = registration.AmountDue };
        registration.Payment.ExternalId = verified.ExternalId;
        registration.Payment.Status = status;
        registration.Payment.Amount = verified.Amount;
        registration.Payment.RawNotification = verified.RawJson;
        registration.Payment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok();
    }
    private static bool IsValidSignature(string? signature, string? requestId, string dataId, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        string? ts = null, v1 = null;
        foreach (var part in signature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2) continue;
            if (pair[0].Equals("ts", StringComparison.OrdinalIgnoreCase)) ts = pair[1];
            if (pair[0].Equals("v1", StringComparison.OrdinalIgnoreCase)) v1 = pair[1];
        }
        if (string.IsNullOrWhiteSpace(ts) || string.IsNullOrWhiteSpace(v1)) return false;
        var manifest = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(dataId)) manifest.Append($"id:{dataId};");
        if (!string.IsNullOrWhiteSpace(requestId)) manifest.Append($"request-id:{requestId};");
        manifest.Append($"ts:{ts};");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var calculated = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest.ToString()))).ToLowerInvariant();
        try
        {
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(calculated), Encoding.ASCII.GetBytes(v1.ToLowerInvariant()));
        }
        catch { return false; }
    }

}
