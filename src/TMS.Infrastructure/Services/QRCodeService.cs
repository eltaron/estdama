using QRCoder;

namespace TMS.Infrastructure.Services;

public interface IQRCodeService
{
    byte[] GenerateQrCode(string data);
    string GenerateAsBase64(string data);
}

public class QRCodeService : IQRCodeService
{
    public byte[] GenerateQrCode(string data)
    {
        using var generator = new QRCodeGenerator();
        var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }

    public string GenerateAsBase64(string data)
    {
        var bytes = GenerateQrCode(data);
        return Convert.ToBase64String(bytes);
    }
}
