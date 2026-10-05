using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace SafeVoicePlatform.Services
{
    public class QRCodeService
    {
        public byte[] GenerateQRCode(string text, int pixelsPerModule = 20)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var bitmap = qrCode.GetGraphic(pixelsPerModule))
                    {
                        using (var stream = new MemoryStream())
                        {
                            bitmap.Save(stream, ImageFormat.Png);
                            return stream.ToArray();
                        }
                    }
                }
            }
        }

        public string GenerateReportQRUrl(int reportId, string referenceNumber)
        {
            return $"/Reports/Track?referenceNumber={referenceNumber}";
        }

        public string GenerateSurveyQRUrl(int surveyId)
        {
            return $"/Survey/Take/{surveyId}";
        }
    }
}