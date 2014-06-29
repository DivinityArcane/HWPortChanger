using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace HWPortChanger
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// This is the version number of the actual program.
        /// </summary>
        private string version = "1.0";
        /// <summary>
        /// This is the original Hammerwatch port, which we're searching for to replace.
        /// While it should only be an unsigned 2 byte integer (ushort), it's a signed 4 byte integer (int).
        /// </summary>
        private int original_port = 9995; 
        /// <summary>
        /// This is a string containing the filename that the end-user selected.
        /// </summary>
        private string filename;

        public Form1 ()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method logs a message to the output console (textbox) in the main form
        /// </summary>
        /// <param name="msg">message to be output</param>
        private void Log (string msg)
        {
            if (Output.InvokeRequired)
            {
                Output.Invoke(new Action(() => Log(msg)));
            }
            else
            {
                Output.AppendText(msg + Environment.NewLine);
            }
        }

        /// <summary>
        /// Clicking the "Select File" button triggers the OpenFileDialog
        /// </summary>
        private void SelectFileButton_Click (object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        /// <summary>
        /// Upon selecting a valid file, we store the filename and wait for a port entry.
        /// </summary>
        private void openFileDialog1_FileOk (object sender, CancelEventArgs e)
        {
            SelectFileButton.Enabled = false;
            PortNumber.Enabled = true;
            PatchFileButton.Enabled = true;
            this.filename = openFileDialog1.FileName;
            Log("Using binary: " + this.filename);
            Log("Input the port number between 1 and 65535 you would like to use, then click 'Patch It!'");
        }

        /// <summary>
        /// Assuming the end-user selected a new port, we look for positions to overwrite.
        /// </summary>
        private void PatchFileButton_Click (object sender, EventArgs e)
        {
            if (!File.Exists(this.filename))
            {
                Log("Whoa! The file you selected has been moved or deleted! Try again!");
                PatchFileButton.Enabled = false;
                PortNumber.Enabled = false;
                SelectFileButton.Enabled = true;
                return;
            }

            ushort port = 0;
            ushort.TryParse(PortNumber.Text, out port);

            if (port == 0)
            {
                Log("Please enter a valid port number between 1 and 65535!");
                return;
            }

            Log("Okay, using port: " + port.ToString());
            string newfile = this.filename + ".bak";

            // Ensure we don't overwrite backups
            for (byte b = 0; b < 255; b++)
            {
                if (!File.Exists(newfile + b.ToString()))
                {
                    newfile = newfile + b.ToString();
                    break;
                }
            }

            Log("Backing up the binary to: " + newfile);

            try
            {
                File.Copy(this.filename, newfile);
                Log("Opening binary for reading/writing...");

                // Woo, nested 'using' blocks!
                using (Stream filestream = new FileStream(filename, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(filestream))
                    {
                        using (BinaryWriter writer = new BinaryWriter(filestream))
                        {
                            Log("Searching for positions to overwrite...");
                            int p = 0;

                            // Loop through the file checking for the port in question
                            for (long pos = 0; pos < filestream.Length; pos++)
                            {
                                // Make sure we won't go out of bounds while reading a 4 byte integer
                                if (pos + 4 < filestream.Length)
                                {
                                    filestream.Seek(pos, SeekOrigin.Begin);
                                    p = reader.ReadInt32();

                                    // Check if the int at the current index matches our target port
                                    if (p == this.original_port)
                                    {
                                        Log("Overwriting port at index " + pos.ToString() + " to: " + port.ToString());
                                        filestream.Seek(-4, SeekOrigin.Current);
                                        writer.Write(port);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception E)
            {
                Log("Unfortunately, something went wrong: " + E.Message);
                return;
            }

            Log("We're done!");

            PatchFileButton.Enabled = false;
            PortNumber.Enabled = false;
        }

        /// <summary>
        /// Some general information to be displayed on startup.
        /// </summary>
        private void Form1_Load (object sender, EventArgs e)
        {
            Log("Hammerwatch Port Changer v" + this.version + " by DivinityArcane <eittreim.justin@live.com>");
            Log("Source: http://github.com/DivinityArcane/HWPortChanger");
            Log("Note that this changes the port used for hosting and joining games.");
            Log("The client will still show 'Port: 9995' but the actual port will be changed!");
            Log("Please select your Hammerwatch.exe file!");
        }
    }
}
