using System;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EncodingTester
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EncodingAttribute : Attribute
    {
        public EncodingAttribute(string name, System.Type encoderType)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (!encoderType.IsSubclassOf(typeof(Encoding)))
                throw new ArgumentException("Invalid encoder type; type must inherit the " + typeof(Encoding).FullName + " class.", nameof(encoderType));
            if (encoderType.IsAbstract)
                throw new ArgumentException("Encoder type cannot be abstract.", nameof(encoderType));
            this.Name = name;
            this.EncoderType = encoderType;
        }

        public string Name { get; private set; }
        public System.Type EncoderType { get; private set; }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            EncodingAttribute attr = obj as EncodingAttribute;
            return attr != null && this.Name.Equals(attr.Name);
        }

        public Encoding GetEncoding(MainForm form)
        {
            return System.Activator.CreateInstance(this.EncoderType, form) as Encoding;
        }

    }
    public abstract class Encoding
    {
        static Encoding()
        {
            BuiltinEncodings = new System.Collections.Generic.List<EncodingAttribute>();
            object[] attributeObjects;
            Type[] types;
            types = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                attributeObjects = types[i].GetCustomAttributes(true);
                for (int j = 0; j < attributeObjects.Length; j++)
                {
                    if (attributeObjects[j] is EncodingAttribute)
                    {
                        EncodingAttribute attr = attributeObjects[j] as EncodingAttribute;
                        if (BuiltinEncodings.Contains(attr))
                            throw new ApplicationException("Error while loading built-in encoding with name '" + attr.Name + "' and type '" + attr.EncoderType.FullName + "': Encoding with name already exists.");
                        BuiltinEncodings.Add(attr);
                    }
                }
            }
        }

        public static System.Collections.Generic.List<EncodingAttribute> BuiltinEncodings { get; private set; }
        protected System.Text.Encoding FinalEncoding { get; private set; }
        protected MainForm MainForm { get; private set; }
        public Encoding(MainForm mainForm, System.Text.Encoding finalEncoding)
        {
            if (mainForm == null)
                throw new System.ArgumentNullException(nameof(mainForm));
            if (finalEncoding == null)
                throw new System.ArgumentNullException(nameof(finalEncoding));
            this.MainForm = mainForm;
            this.FinalEncoding = finalEncoding;
        }

        /// <summary>
        /// Creates Encoding using UTF-8 as final encoding
        /// </summary>
        /// <param name="mainForm"></param>
        public Encoding(MainForm mainForm) : this(mainForm, System.Text.Encoding.UTF8)
        {
        }

        public static string FoldString(string input)
        {
            return FoldString(input, defaultFoldLineLength);
        }

        public static string FoldString(string input, int length)
        {
            if (input.Length <= length)
                return input;

            string output = String.Empty;
            bool firstRun = true;
            while (input.Length > length)
            {
                if (!firstRun)
                    output += foldEndLine;
                output += input.Substring(0, length);
                input = input.Substring(length);
                if (firstRun)
                {
                    firstRun = false;
                    length--;   // To account for the space in the foldEndLine sequence
                }
            }

            return output += foldEndLine + input;
        }
        private static readonly int defaultFoldLineLength = 75;
        private static readonly string foldEndLine = "\r\n ";

        public static string UnfoldString(string input)
        {
            return input.Replace("\r\n ", String.Empty);
        }
        public async Task Encode(System.IO.MemoryStream stream)
        {
            string output = null;
            System.IO.MemoryStream memoryStream;
            System.Text.Encoding encoding;
            try
            {
                (memoryStream, encoding) = await this.BeginEncode(stream);
                if (memoryStream != null)
                {
                    using (memoryStream)
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(memoryStream, encoding == null ? this.FinalEncoding : encoding))
                        {
                            output = await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                output = ex.ToString();
            }

            if (!String.IsNullOrWhiteSpace(output))
            {
                try
                {
                    this.MainForm.UpdateText(output);
                }
                catch
                {

                }
            }
        }
        public async Task Decode(System.IO.MemoryStream stream)
        {
            string output = null;
            System.IO.MemoryStream memoryStream;
            System.Text.Encoding encoding;
            try
            {
                (memoryStream, encoding) = await this.BeginDecode(stream);
                if (memoryStream != null)
                {
                    using (memoryStream)
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(memoryStream, encoding == null ? this.FinalEncoding : encoding))
                        {
                            output = await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                output = ex.ToString();
            }

            if (!String.IsNullOrWhiteSpace(output))
            {
                try
                {
                    this.MainForm.UpdateText(output);
                }
                catch
                {

                }
            }
        }

        protected async Task<Tuple<System.IO.MemoryStream, System.Text.Encoding>> AddExceptionToStream(Exception ex, System.IO.MemoryStream stream)
        {
            byte[] exceptionBytes = System.Text.Encoding.UTF8.GetBytes(ex.ToString());
            await stream.WriteAsync(exceptionBytes, 0, exceptionBytes.Length);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return new Tuple<System.IO.MemoryStream, System.Text.Encoding>(stream, System.Text.Encoding.UTF8);
        }
        protected abstract Task<Tuple<System.IO.MemoryStream, System.Text.Encoding>> BeginEncode(System.IO.MemoryStream stream);
        protected abstract Task<Tuple<System.IO.MemoryStream, System.Text.Encoding>> BeginDecode(System.IO.MemoryStream stream);
    }
}
