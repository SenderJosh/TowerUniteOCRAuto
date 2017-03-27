﻿using System;
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
            api.Init(Patagames.Ocr.Enums.Languages.English);
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
                        Console.WriteLine("DebugMode: Waited 15 minutes, saving iterated image");
                        img.Save("iteratedImage" + ++iteratedImage);
                    }
                    try
                    {
                        using (Bitmap bmp = new Bitmap(img))
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
                                        if (keyToPress.ToLower() == "0" || keyToPress.ToLower() == "o") //wait at least 100ms between presses
                                        {
                                            //press both 0 and o
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.O), IntPtr.Zero);
                                            Thread.Sleep(100);
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.NumPad0), IntPtr.Zero);

                                            Console.WriteLine("Pressed 0 and o");
                                        }
                                        else if (keyToPress.ToLower() == "1" || keyToPress.ToLower() == "l" || keyToPress.ToLower() == "i")
                                        {
                                            //press all of those
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.L), IntPtr.Zero);
                                            Thread.Sleep(100);
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.I), IntPtr.Zero);

                                            Console.WriteLine("Pressed L and I");
                                        }
                                        else if (keyToPress.ToLower() == "2") //chances are, meant "Z" according to screenshots
                                        {
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.Z), IntPtr.Zero);

                                            Console.WriteLine("Pressed 2");
                                        }
                                        else if (keyToPress.ToLower() == "5") //chances are, meant "S" according to screenshots
                                        {
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);

                                            Console.WriteLine("Pressed S");
                                        }
                                        else if (keyToPress.ToLower() == "9") //chances are, meant "G" according to screenshots
                                        {
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.G), IntPtr.Zero);

                                            Console.WriteLine("Pressed G");
                                        }
                                        else if (keyToPress.ToLower() == "7")
                                        {
                                            PostMessage(towerUniteHandle, WM_KEYDOWN, (IntPtr)(Keys.T), IntPtr.Zero);

                                            Console.WriteLine("Pressed T");
                                            //NOTE: m doesn't get read for some reason (like, the whole thing doesn't read)
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
                            if (found)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.Write(DateTime.Now.ToShortTimeString() + ": ");
                                Console.ResetColor();
                                Console.WriteLine("Bypassed an AFK check x" + ++timesBypassed);
                                img.Save("lastSuccessfulBypassImageTaken.png");
                                api.Clear(); //new
                                lastTime = (60 * 14 * 1000) - 500; //Wait 14 minutes because the next check won't happen until 15min. +-time for things. 
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
    }
}
