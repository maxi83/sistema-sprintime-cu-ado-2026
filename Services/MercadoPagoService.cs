using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using lucho_sprintime.Models;

namespace lucho_sprintime.Services;

public class MercadoPagoService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IMercadoPagoService
{
    private string Token => configuration["MercadoPago:AccessToken"] ?? "";
    private string BaseUrl => (configuration["MercadoPago:BaseUrl"] ?? "https://api.mercadopago.com").TrimEnd('/');

    public async Task<CheckoutResult> CreateCheckoutAsync(Registration registration, string successUrl, string pendingUrl, string failureUrl, string webhookUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Token))
            return new(false, null, null, "Mercado Pago todavía no tiene AccessToken configurado.");

        var client = CreateClient();
        var payload = new
        {
            items = new[]
            {
                new
                {
                    title = $"{registration.Event.Name} · {registration.EventDistance.Name}",
                    quantity = 1,
                    currency_id = "ARS",
                    unit_price = registration.AmountDue
                }
            },
            payer = new { name = registration.Cyclist.FirstName, surname = registration.Cyclist.LastName, email = registration.Cyclist.Email },
            external_reference = $"SPRINTIME-REG-{registration.Id}",
            notification_url = webhookUrl,
            back_urls = new { success = successUrl, pending = pendingUrl, failure = failureUrl },
            auto_return = "approved",
            metadata = new { registration_id = registration.Id, event_id = registration.EventId }
        };

        using var response = await client.PostAsJsonAsync($"{BaseUrl}/checkout/preferences", payload, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return new(true, null, null, $"Mercado Pago respondió {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var checkoutUrl = root.TryGetProperty("init_point", out var initEl) ? initEl.GetString() : null;
        return new(true, checkoutUrl, id, null);
    }

    public async Task<VerifiedPayment?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Token)) return null;
        var client = CreateClient();
        using var response = await client.GetAsync($"{BaseUrl}/v1/payments/{Uri.EscapeDataString(paymentId)}", ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.ToString() : paymentId;
        var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() ?? "pending" : "pending";
        var amount = root.TryGetProperty("transaction_amount", out var amountEl) && amountEl.TryGetDecimal(out var value) ? value : 0;
        var reference = root.TryGetProperty("external_reference", out var refEl) ? refEl.GetString() : null;
        return new(id, status, amount, reference, json);
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }
}
