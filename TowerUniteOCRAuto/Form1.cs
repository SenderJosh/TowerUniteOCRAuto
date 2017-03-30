using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Patagames.Ocr;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TowerUniteOCRAuto
{
    public partial class Form1 : Form
    {

        private int timesBypassed = 0, timesClicked = 0;

        public static bool cont = false, pressImageFound = false;
        private OcrApi api;
        Keys lastKeyPressed;
        bool changed = false;

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private IntPtr towerUniteHandle = IntPtr.Zero;

        KeyboardHook hook = new KeyboardHook();

        public Form1()
        {
            InitializeComponent();
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            MaximizeBox = false;
            Console.Title = "TowerUniteOCRAuto";
            Console.WriteLine("Looking for Tower Unite handle...");
            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Equals("Tower-Win64-Shipping"))
                {
                    towerUniteHandle = p.MainWindowHandle;
                    Console.WriteLine("FOUND HANDLE: " + towerUniteHandle);
                }
            }

            if (towerUniteHandle == IntPtr.Zero)
            {
                MessageBox.Show("Tower Unite is not running.\nPlease run Tower Unite before running this application.\nClosing application.");
                Application.Exit();
                Environment.Exit(0);
            }

            Console.WriteLine("Working with your Primary Screen with dimensions: " + Screen.PrimaryScreen.Bounds.Width + "x" +
                                            Screen.PrimaryScreen.Bounds.Height);
            Console.WriteLine("I recommend you TURN OFF your ChatBox as to not mess with the OCR reading.\n\tGo into Tower Unite ingame, then: Settings->Content->Disable Chat");
            Console.WriteLine("Note that as of now, the default button that will be pressed is SPACE.\n\tIf you are on Video Blacjack, go into your Tower Unite ingame, then: Settings->Controls->Mouse 1 (Click, Putt, Etc) | Rebind to SPACE");
        }

        private void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (StartedStoppedLabel.Text.Equals("Stopped"))
            {
                cont = true;
                bool success = false;
                StartedStoppedLabel.Text = "Started";
                int delTime = 0, ranTime = 0;
                if (Int32.TryParse(delayTimeTextBox.Text, out delTime) && Int32.TryParse(randomTimeTextBox.Text, out ranTime))
                {
                    if (delTime > 0 && ranTime >= 0)
                    {
                        CallRunOCRAsync(3000);
                        CallRunAutoKeyPressAsync(Int32.Parse(delayTimeTextBox.Text), Int32.Parse(randomTimeTextBox.Text));
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Starting auto...");
                        Console.ResetColor();
                        success = true;
                    }
                }
                if (!success)
                {
                    MessageBox.Show("Only integer values are allowed in the time textboxes\nMake sure they are non-negative, and your delay must be a non-zero time.");
                }
            }
            else
            {
                cont = false;
                StartedStoppedLabel.Text = "Stopped";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Stopping auto...");
                Console.ResetColor();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            api = OcrApi.Create();
            string[] config = { "configs.cfg" };
            api.Init(Patagames.Ocr.Enums.Languages.English);
            api.SetVariable("tessedit_char_whitelist", ("ABCDEFGHIJKLMNOPQRSTUVWXYZ") + ("ABCDEFGHIJKLMNOPQRSTUVWXYZ").ToLower());
            /*Image img = Image.FromFile(@"C:\Users\Joshua\Documents\Visual Studio 2015\Projects\TowerUniteOCRAuto\TowerUniteOCRAuto\bin\x86\Debug\Collection of failed pictures\b.png");
            Bitmap bitmap = (Bitmap)img;
            Bitmap bm = new Bitmap(bitmap);
            Console.WriteLine(api.GetTextFromImage(bm));
            bm.Save("testtest1.png");

            bm = GrayScale(bm);
            bm = AdjustContrast(bm, 100);
            Console.WriteLine(api.GetTextFromImage(bm));
            bm.Save("testtest2.png");*/
        }

        ////////Key Press Here////////

        private async void CallRunAutoKeyPressAsync(int msDelay, int msRand)
        {
            await RunAutoKeypressAsync(msDelay, msRand);
        }

        private Task<Boolean> RunAutoKeypressAsync(int msDelay, int msRand)
        {
            return Task.Factory.StartNew(() => RunAutoKeyPress(msDelay, msRand));
        }

        private Boolean RunAutoKeyPress(int msDelay, int msRand)
        {
            Random rand = new Random();
            while (Form1.cont)
            {
                Thread.Sleep(msDelay + rand.Next(msRand) + 1);
                if (Form1.cont && !Form1.pressImageFound) //User may toggle here
                {
                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)Keys.Space, IntPtr.Zero);
                    Console.Title = "TowerUniteOCRAuto - Times Clicked: " + ++timesClicked;
                }
            }
            return Form1.cont;
        }

        ////////Picture OCR Here////////

        private async void CallRunOCRAsync(int ms)
        {
            if (await RunOCRAsync(ms)) //Returned true means something fucked up and was forcibly stopped
            {
                StartedStoppedLabel.Text = "Stopped";
            }
        }

        private Task<Boolean> RunOCRAsync(int ms)
        {
            return Task.Factory.StartNew(() => RunOCR(ms));
        }

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        private Boolean RunOCR(int ms)
        {
            int lastTime = ms, iteratedImage = 0;
            bool finFirstCheck = false;
            while (Form1.cont)
            {
                Thread.Sleep(lastTime);
                if (Form1.cont) //User may toggle here
                {
                    Image img = Utilities.CaptureWindow(towerUniteHandle);
                    //Image img = Image.FromFile("C:/Program Files (x86)/Steam/userdata/101136263/760/remote/394690/screenshots/press2.jpg"); //Test Specific key press image
                    if(finFirstCheck)
                    {
                        //Console.WriteLine("DebugMode: Waited 15 minutes, saving iterated image");
                        img.Save("iteratedImage" + ++iteratedImage + ".png");
                    }
                    try
                    {
                        using (Bitmap bmp = AdjustContrast(GrayScale(new Bitmap(img)), 100))
                        {
                            List<string> words = new List<string>(api.GetTextFromImage(bmp).Split(' '));
                            bool found = false;
                            for (int i = 0; i < words.Count - 1 && !found; i++)
                            {
                                if (words[i].ToLower() == "press")
                                {
                                    Form1.pressImageFound = true;
                                    string keyToPress = words[i + 1];
                                    if (!string.IsNullOrEmpty(keyToPress) && keyToPress.Length == 1) //Didn't grab a null string and is larger not recognized as a word (def a char)
                                    {
                                        if (keyToPress.ToLower() == "l" || keyToPress.ToLower() == "i")
                                        {
                                            //press both of those
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.L), IntPtr.Zero);
                                            Thread.Sleep(100);
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.I), IntPtr.Zero);

                                            Console.WriteLine("Pressed L and I");
                                        }
                                        else
                                        {
                                            //press just the key that it found
                                            Keys key = Keys.None;
                                            if (Regex.IsMatch(keyToPress, @"(?i)^[a-z ]+")) //Only letters
                                            {
                                                Enum.TryParse(keyToPress.ToUpper(), out key);
                                            }

                                            if (key == Keys.None)
                                            {
                                                Console.WriteLine("Could not find the key to press! Stopping automated procedures.");
                                                Form1.cont = false;
                                                Console.WriteLine("Application stopped -- ERROR: Could not resolve key to press: " + keyToPress + " during OCR\nLet the dev know the key that failed\n\tSaved Failed Image as failedimage.png");
                                                img.Save("failedimage.png");
                                                img.Dispose();
                                                api.Clear(); //new
                                                return true;
                                            }
                                            else
                                            {
                                                PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
                                                Console.WriteLine("Pressed " + key.ToString());
                                            }
                                        }
                                        found = true;
                                    }
                                    else if(!string.IsNullOrEmpty(keyToPress) && keyToPress.Length == 3)
                                    {
                                        string keyToPressLower = keyToPress.ToLower();
                                        if(keyToPressLower[1] == 't' && keyToPressLower[2] == 'o')
                                        {
                                            if (keyToPressLower[0] == 'l' || keyToPressLower[0] == 'i')
                                            {
                                                //press both of those
                                                PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.L), IntPtr.Zero);
                                                Thread.Sleep(100);
                                                PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.I), IntPtr.Zero);

                                                Console.WriteLine("Pressed L and I");
                                            }
                                            else
                                            {
                                                //press just the key that it found
                                                Keys key = Keys.None;
                                                if (Regex.IsMatch(keyToPress, @"(?i)^[a-z ]+")) //Only letters
                                                {
                                                    Enum.TryParse(keyToPress.ToUpper(), out key);
                                                }

                                                if (key == Keys.None)
                                                {
                                                    Console.WriteLine("Could not find the key to press! Stopping automated procedures.");
                                                    Form1.cont = false;
                                                    Console.WriteLine("Application stopped -- ERROR: Could not resolve key to press: " + keyToPress + " during OCR\nLet the dev know the key that failed\n\tSaved Failed Image as failedimage.png");
                                                    img.Save("failedimage.png");
                                                    img.Dispose();
                                                    api.Clear(); //new
                                                    return true;
                                                }
                                                else
                                                {
                                                    PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
                                                    Console.WriteLine("Pressed " + key.ToString());
                                                }
                                            }
                                            found = true;
                                        }
                                    }
                                }
                            }
                            if (found)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                                Console.ResetColor();
                                Console.WriteLine("Bypassed an AFK check x" + ++timesBypassed);
                                img.Save("lastSuccessfulBypassImageTaken.png");
                                api.Clear(); //new
                                lastTime = (60 * 15 * 1000) - (30 * 1000); //Wait 14.5 minutes because the next check won't happen until 15min. +-time for things. 
                                //Should get the time before I run OCR and after OCR and run the difference to get exact 15 min.
                                finFirstCheck = true;
                            }
                            else
                            {
                                lastTime = ms;
                            }
                        }
                        img.Save("lastImageTaken.png");
                        img.Dispose();
                        api.Clear(); //new
                        Form1.pressImageFound = false;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("EXCEPTION -- Saved as output.txt (not implemented yet) -- Stopping automated procedures");
                        Form1.cont = false;
                        Console.WriteLine("Error Message:\n" + ex.Message + "\n" + ex.ToString());
                        Console.ResetColor();
                        img.Dispose();
                        api.Clear(); //new
                        return true;
                    }
                }
            }
            return Form1.cont;
        }

        private void keybindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            hook.UnregisterHotKeys();
            lastKeyPressed = e.KeyCode;
            hook.RegisterHotKey(lastKeyPressed);
            changed = true;
            label4.Focus();
        }

        private void keybindTextBox_TextChanged(object sender, EventArgs e)
        {
            if (changed)
            {
                keybindTextBox.Text = lastKeyPressed.ToString();
            }
            else
            {
                changed = false;
            }
        }

        /// <summary>
        /// Credits to casperOne and raRaRa on Stackoverflow
        /// 
        /// GrayScale a bitmap. Helps with Tesseract's OCR, despite Tesseract internally grascaling the image, doing it externally to the image seems
        /// to have improved the accuracy.
        /// </summary>
        /// <param name="Bmp"></param>
        /// <returns>GrayScale Bitmap</returns>
        private Bitmap GrayScale(Bitmap Bmp)
        {
            int rgb;
            Color c;

            for (int y = 0; y < Bmp.Height; y++)
                for (int x = 0; x < Bmp.Width; x++)
                {
                    c = Bmp.GetPixel(x, y);
                    rgb = (int)((c.R + c.G + c.B) / 3);
                    Bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            return Bmp;
        }

        private Bitmap AdjustContrast(Bitmap Image, float Value)
        {
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(
                new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                ImageLockMode.ReadWrite,
                NewBitmap.PixelFormat);
            int Height = NewBitmap.Height;
            int Width = NewBitmap.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float Red = R / 255.0f;
                        float Green = G / 255.0f;
                        float Blue = B / 255.0f;
                        Red = (((Red - 0.5f) * Value) + 0.5f) * 255.0f;
                        Green = (((Green - 0.5f) * Value) + 0.5f) * 255.0f;
                        Blue = (((Blue - 0.5f) * Value) + 0.5f) * 255.0f;

                        int iR = (int)Red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)Green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)Blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }

            NewBitmap.UnlockBits(data);

            return NewBitmap;
        }

    }
}
