using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Composition;

namespace EncodingTester
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.outputTextBox.Text = String.Empty;
            this.Encodings = new System.Collections.Generic.List<EncodingAttribute>();
            this.Instance = this;


            for (int i = 0; i < Encoding.BuiltinEncodings.Count; i++)
            {
                EncodingAttribute attr = Encoding.BuiltinEncodings[i];
                this.Encodings.Add(attr);
            }

            try
            {
                string[] directories = System.IO.Directory.GetDirectories(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "plugins"));
                for (int i = 0; i < directories.Length; i++)
                {
                    System.ComponentModel.Composition.Hosting.DirectoryCatalog catalog = new System.ComponentModel.Composition.Hosting.DirectoryCatalog(System.IO.Path.Combine("plugins", System.IO.Path.GetFileName(directories[i])));
                    using (System.ComponentModel.Composition.Hosting.CompositionContainer container = new System.ComponentModel.Composition.Hosting.CompositionContainer(catalog))
                    {
                        container.ComposeParts(this);

                        object[] attributeObjects;
                        foreach (Encoding plugin in this.PluginEncodings)
                        {
                            attributeObjects = plugin.GetType().GetCustomAttributes(false);
                            for (int j = 0; j < attributeObjects.Length; j++)
                            {
                                if (attributeObjects[j] is EncodingAttribute)
                                {
                                    EncodingAttribute attr = attributeObjects[j] as EncodingAttribute;
                                    if (this.Encodings.Contains(attr))
                                    {
                                        this.outputTextBox.Text += "Error while loading plugin encoding with name '" + attr.Name + "' and type '" + attr.EncoderType.FullName + "': Encoding with name already exists." + System.Environment.NewLine;
                                        continue;
                                    }
                                    this.Encodings.Add(attr);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.outputTextBox.Text += ex.ToString();
                return;
            }


            try
            {
            }
            catch (Exception ex)
            {
                this.outputTextBox.Text += ex.ToString();
                return;
            }

            if (this.Encodings.Count < 1)
                this.outputTextBox.Text += "Error: no encodings found...";

            this.encodingType.DataSource = this.Encodings;
            this.encodingType.DisplayMember = "Name";
        }

        private readonly System.Collections.Generic.List<EncodingAttribute> Encodings;
        
        [Export("MainFormParameter")]
        private MainForm Instance { get; set; }

        [System.ComponentModel.Composition.ImportMany(typeof(Encoding))] // This is a signal to the MEF framework to load all matching exported assemblies.
        private System.Collections.Generic.IEnumerable<Encoding> PluginEncodings { get; set; }

        public void UpdateText(string output)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new UpdateTextDelegate(this.UpdateText), output);
                }
                catch (System.Reflection.TargetInvocationException ex)
                {
                    this.outputTextBox.Text = ex.InnerException.ToString();
                }
                catch (Exception ex)
                {
                    this.outputTextBox.Text = ex.ToString();
                }
                return;
            }

            this.outputTextBox.Text = String.Empty;
            this.outputTextBox.Text = output;
        }

        private delegate void UpdateTextDelegate(string output);

        private void encodeButton_CheckedChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(this.outputTextBox.Text))
            {
                this.RunProcess(this.outputTextBox.Text);
            }

        }
        private void MainForm_DragDrop(object sender, DragEventArgs args)
        {
            this.importData(args.Data);
        }

        private void pasteButton_Click(object sender, EventArgs e)
        {
            this.importData(Clipboard.GetDataObject());
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            this.UpdateText(String.Empty);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void importData(System.Windows.Forms.IDataObject data)
        {
            object inputData = null;
            bool success = false;

            if (data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] fileNames = data.GetData(DataFormats.FileDrop, true) as string[];
                if (fileNames?.Length is 1)
                {
                    if (System.IO.File.Exists(fileNames[0]))
                    {
                        inputData = new System.IO.FileInfo(fileNames[0]);
                        success = true;
                    }
                }
            }
            else if (data.GetDataPresent(DataFormats.Text, true))
            {
                inputData = data.GetData(DataFormats.Text, true) as string;
                success = true;
            }

            if (success)
                RunProcess(inputData);

        }

        private void RunProcess(object inputData)
        {

            EncodingAttribute encodingAttr = this.encodingType.SelectedValue as EncodingAttribute;
            if (encodingAttr == null)
                return;

            this.UpdateText(String.Empty);
            Encoding encoding;
            try
            {
                encoding = encodingAttr.GetEncoding(this);
            }
            catch (Exception ex)
            {
                this.UpdateText(ex.ToString());
                return;
            }

            bool isEncode = this.encodeButton.Checked;

            Task.Run(async () =>
            {
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    try
                    {
                        if (inputData is System.IO.FileInfo)
                        {
                            System.IO.FileInfo inputFile = inputData as System.IO.FileInfo;
                            using (System.IO.FileStream fileStream = inputFile.OpenRead())
                            {
                                await fileStream.CopyToAsync(stream);
                            }
                        }
                        else if (inputData is string && !String.IsNullOrWhiteSpace((string)inputData))
                        {
                            string inputText = inputData as string;
                            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8, inputText.Length, true))
                            {
                                await writer.WriteAsync(inputText);
                            }
                        }

                        await Task.Delay(500);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);

                        if (isEncode)
                            await encoding.Encode(stream);
                        else
                            await encoding.Decode(stream);
                    }
                    catch (Exception ex)
                    {
                        this.UpdateText(ex.ToString());
                    }
                }
            });
        }

        private void MainForm_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

    }
}
