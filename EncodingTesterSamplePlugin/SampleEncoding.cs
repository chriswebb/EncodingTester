using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncodingTesterSamplePlugin
{
    [System.ComponentModel.Composition.Export(typeof(EncodingTester.Encoding))]
    [EncodingTester.Encoding("sample", typeof(SampleEncoding))]
    public class SampleEncoding : EncodingTester.Encoding
    {
        [System.ComponentModel.Composition.ImportingConstructor]
        public SampleEncoding([System.ComponentModel.Composition.Import("MainFormParameter")]EncodingTester.MainForm instance) : base(instance)
        {

        }

        protected override async Task<Tuple<MemoryStream, Encoding>> BeginDecode(MemoryStream stream)
        {
            System.IO.MemoryStream outputStream = new MemoryStream();
            try
            {
                using (stream)
                {
                    byte[] bytes = this.FinalEncoding.GetBytes("Example of decoded text.");
                    await outputStream.WriteAsync(bytes, 0, bytes.Length);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return new Tuple<MemoryStream, Encoding>(outputStream, null);
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
                    return new Tuple<MemoryStream, Encoding>(outputStream, null);
                }
            }
        }

        protected override async Task<Tuple<MemoryStream, Encoding>> BeginEncode(MemoryStream stream)
        {
            System.IO.MemoryStream outputStream = new MemoryStream();
            try
            {
                using (stream)
                {
                    byte[] bytes = this.FinalEncoding.GetBytes("Example of encoded text.");
                    await outputStream.WriteAsync(bytes, 0, bytes.Length);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    return new Tuple<MemoryStream, Encoding>(outputStream, null);
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
                    return new Tuple<MemoryStream, Encoding>(outputStream, null);
                }
            }
        }
    }
}
