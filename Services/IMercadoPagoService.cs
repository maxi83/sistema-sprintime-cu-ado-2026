using lucho_sprintime.Models;

namespace lucho_sprintime.Services;

public record CheckoutResult(bool IsConfigured, string? CheckoutUrl, string? PreferenceId, string? Error);
public record VerifiedPayment(string ExternalId, string Status, decimal Amount, string? ExternalReference, string RawJson);

public interface IMercadoPagoService
{
    Task<CheckoutResult> CreateCheckoutAsync(Registration registration, string successUrl, string pendingUrl, string failureUrl, string webhookUrl, CancellationToken ct = default);
    Task<VerifiedPayment?> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}
