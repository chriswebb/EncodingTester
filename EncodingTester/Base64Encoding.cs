using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EncodingTester
{
    [EncodingTester.Encoding("base64", typeof(Base64Encoding))]
    public class Base64Encoding : EncodingTester.Encoding
    {
        public Base64Encoding(EncodingTester.MainForm mainForm) : base(mainForm)
        {

        }
        protected override async Task<Tuple<MemoryStream, System.Text.Encoding>> BeginDecode(MemoryStream stream)
        {
            System.IO.MemoryStream outputStream = new MemoryStream();
            try
            {
                using (stream)
                {
                    using (System.IO.StreamReader reader = new StreamReader(stream))
                    {
                        string results = await reader.ReadToEndAsync();
                        byte[] bytes = Convert.FromBase64String(results);
                        await outputStream.WriteAsync(bytes, 0, bytes.Length);
                        await outputStream.FlushAsync();
                        outputStream.Seek(0, SeekOrigin.Begin);
                        return new Tuple<MemoryStream, System.Text.Encoding>(outputStream, null);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    return await this.AddExceptionToStream(ex, outputStream);
                }
                catch (Exception)
                {
                    return new Tuple<MemoryStream, System.Text.Encoding>(outputStream, null);
                }
            }
        }

        protected override async Task<Tuple<MemoryStream, System.Text.Encoding>> BeginEncode(MemoryStream stream)
        {
            System.IO.MemoryStream outputStream = new MemoryStream();
            try
            {
                using (stream)
                {
                    byte[] bytes = stream.ToArray();
                    string encodedString = Convert.ToBase64String(bytes);
                    bytes = this.FinalEncoding.GetBytes(encodedString);
                    await outputStream.WriteAsync(bytes, 0, bytes.Length);
                    await outputStream.FlushAsync();
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return new Tuple<MemoryStream, System.Text.Encoding>(outputStream, null);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    return await this.AddExceptionToStream(ex, outputStream);
                }
                catch (Exception)
                {
                    return new Tuple<MemoryStream, System.Text.Encoding>(outputStream, null);
                }
            }
        }
    }
}
