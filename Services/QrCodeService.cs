using QRCoder;

namespace lucho_sprintime.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        return qr.GetGraphic(12);
    }
}
