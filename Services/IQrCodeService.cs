namespace lucho_sprintime.Services;

public interface IQrCodeService
{
    byte[] GeneratePng(string content);
}
