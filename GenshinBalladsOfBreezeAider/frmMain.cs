using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GenshinBalladsOfBreezeAider
{
    public partial class frmMain : Form
    {
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd, out Rectangle lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out Rectangle lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr ptr);
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        const int HORZRES = 8;
        const int VERTRES = 10;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;

        private IntPtr hwndGenshin = IntPtr.Zero;

        private int genshinWindowX = 0;
        private int genshinWindowY = 0;
        private int genshinWindowWdith = 0;
        private int genshinWindowHeight = 0;
        private bool working = false;

        public static float DpiScaleX
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                float ScaleX = (float)GetDeviceCaps(hdc, DESKTOPHORZRES) / (float)GetDeviceCaps(hdc, HORZRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleX;
            }
        }

        public static float DpiScaleY
        {
            get
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                float ScaleY = (float)(float)GetDeviceCaps(hdc, DESKTOPVERTRES) / (float)GetDeviceCaps(hdc, VERTRES);
                ReleaseDC(IntPtr.Zero, hdc);
                return ScaleY;
            }
        }

        public frmMain() => InitializeComponent();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
#if DEBUG
            btnDebugScreenshot.Visible = true;
            debugTextBox.Visible = true;
#endif
            FindGenshinProcess();
        }

        private void FindGenshinProcess()
        {
            lblStatus.Text = $"未找到原神进程";
            lblStatus.ForeColor = Color.Red;
            Task.Run(() =>
            {
                while (!working)
                {
                    while (hwndGenshin == IntPtr.Zero)
                    {
                        hwndGenshin = FindWindow("UnityWndClass", "原神");
                        Task.Delay(100).Wait();
                    }

                    GetWindowRect(hwndGenshin, out Rectangle windowRect);
                    GetClientRect(hwndGenshin, out Rectangle clientRect);

                    Invoke(new Action(() =>
                    {
                        try
                        {
                            if (!working)
                            {
                                lblStatus.Text = $"已找到原神进程，未开始自动演奏";
                                lblStatus.ForeColor = Color.Black;
                            }

                            float dpiX = DpiScaleX;
                            float dpiY = DpiScaleY;

                            if (windowRect.X < -16000)  //全屏
                            {
                                genshinWindowX = 0;
                                genshinWindowY = 0;
                                genshinWindowWdith = (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * dpiX);
                                genshinWindowHeight = (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * DpiScaleY);

                                lblWindowType.Text = "全屏";
                                lblWindowLocation.Text = $"(0,0)";

                            }
                            else  //窗口化
                            {
                                Rectangle tempRect = new Rectangle(windowRect.X, windowRect.Y, windowRect.Width - windowRect.X, windowRect.Height - windowRect.Y);
                                Rectangle rect = new Rectangle(tempRect.X + (tempRect.Width - clientRect.Width) - 3, tempRect.Y + (tempRect.Height - clientRect.Height) - 3, clientRect.Width, clientRect.Height);

                                if (dpiX > 1 || DpiScaleY > 1)
                                {
                                    rect = new Rectangle((int)Math.Round(rect.X * dpiX), (int)Math.Round(rect.Y * dpiY), (int)Math.Round(rect.Width * dpiX), (int)Math.Round(rect.Height * dpiY));
                                }

                                genshinWindowX = rect.X;
                                genshinWindowY = rect.Y;
                                genshinWindowWdith = rect.Width;
                                genshinWindowHeight = rect.Height;

                                lblWindowType.Text = "窗口化";
                                lblWindowLocation.Text = $"({genshinWindowX},{genshinWindowY})";
                            }

                            lblDpi.Text = $"{DpiScaleX * 100}%";

                            if (genshinWindowWdith / 16 * 9 == genshinWindowHeight)
                            {
                                lblRatio.Text = "16:9";
                                lblRatio.ForeColor = Color.Black;
                                lblWarring.Text = "";

                                if (DpiScaleX != 1)
                                {
                                    lblWarring.Text = "您显示设置-更改文本、应用等项目的大小不是100%，可能在坐标换算中产生偏移。";
                                }
                            }
                            else
                            {
                                if (genshinWindowWdith / 16 * 10 == genshinWindowHeight)
                                {
                                    lblRatio.Text = "16:10";
                                }
                                else if (genshinWindowWdith / 4 * 3 == genshinWindowHeight)
                                {
                                    lblRatio.Text = "4:3";
                                }
                                else if (genshinWindowWdith / 5 * 4 == genshinWindowHeight)
                                {
                                    lblRatio.Text = "5:4";
                                }
                                else
                                {
                                    lblRatio.Text = "未知";
                                }
                                lblRatio.ForeColor = Color.Red;
                                lblWarring.Text = "尚未适配当前分辨率，请切换到长宽比为16:9的分辨率";
                            }

                            lblWindowSize.Text = $"{genshinWindowWdith}×{genshinWindowHeight}";

                            btnStart.Enabled = true;
                        }
                        catch
                        {
                        }
                    }));
                }
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (working)
            {
                working = false;
                btnStart.Text = "开始自动演奏";
                lblStatus.Text = $"已找到原神进程，未开始自动演奏";
                lblStatus.ForeColor = Color.Black;
                FindGenshinProcess();
                return;
            }
            else
            {
                working = true;
                lblStatus.Text = $"已开始自动演奏";
                lblStatus.ForeColor = Color.Green;
                btnStart.Text = "停止自动演奏";
            }
            Task.Run(() =>
            {
                float dpiX = DpiScaleX;
                float dpiY = DpiScaleY;

                Dictionary<Keys, DateTime> dicKeysNextPressTime = new Dictionary<Keys, DateTime>();
                Dictionary<Keys, DateTime> dicKeysNextPressTimeLong = new Dictionary<Keys, DateTime>();

                int width = (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * dpiX);
                int height = (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * dpiY);
                Size s = new Size(width, height);

                while (working)
                {
                    Bitmap memoryImage = new Bitmap(width, height);
                    Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                    memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);


                    double xs = 0.0381249d;
                    double ys = 0.0677083d;
                    //以下是短按的
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.253125, 0.37685, Keys.W);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.253125, 0.823148, Keys.S);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.127083, 0.6, Keys.A);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.378125, 0.6, Keys.D);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.74739583, 0.37685, Keys.I);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.74739583, 0.823148, Keys.K);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.62135416, 0.6, Keys.J);
                    StartAutoPressKeyAndRecordCD(dicKeysNextPressTime, memoryImage, 0.872917, 0.6, Keys.L);


                    //以下是长按的
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.253125, 0.37685, Keys.W);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.253125, 0.823148 - ys * 2, Keys.S);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.127083 + xs, 0.6 - ys, Keys.A);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.378125 - xs, 0.6 - ys, Keys.D);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.74739583, 0.37685, Keys.I);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.74739583, 0.823148 - ys * 2, Keys.K);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.62135416 + xs, 0.6 - ys, Keys.J);
                    StartAutoPressKeyAndRecordLong(dicKeysNextPressTimeLong, memoryImage, 0.872917 - xs, 0.6 - ys, Keys.L);

                    memoryGraphics.Dispose();
                    memoryImage.Dispose();
                }
            });
        }

        bool wReady, sReady, aReady, dReady, iReady, kReady, jReady, lReady;
        bool wisPressed, sisPressed, aisPressed, disPressed, iisPressed, kisPressed, jisPressed, lisPressed;
        bool wReadyLong, sReadyLong, aReadyLong, dReadyLong, iReadyLong, kReadyLong, jReadyLong, lReadyLong;

        private void btnDebugScreenshot_Click(object sender, EventArgs e) => SaveDebugImage();

        private void StartAutoPressKeyAndRecordCD(Dictionary<Keys, DateTime> dicKeysNextPressTime, Bitmap bmp, double scaleX, double scaleY, Keys key)
        {
            if (dicKeysNextPressTime.ContainsKey(key))
            {
                if (dicKeysNextPressTime[key] < DateTime.Now)
                {
                    dicKeysNextPressTime[key] = StartAutoPressKey(bmp, scaleX, scaleY, key);
                }
            }
            else
            {
                dicKeysNextPressTime.Add(key, StartAutoPressKey(bmp, scaleX, scaleY, key));
            }
        }

        private void StartAutoPressKeyAndRecordLong(Dictionary<Keys, DateTime> dicKeysNextPressTime, Bitmap bmp, double scaleX, double scaleY, Keys key)
        {
            if (dicKeysNextPressTime.ContainsKey(key))
            {
                if (dicKeysNextPressTime[key] < DateTime.Now)
                {
                    dicKeysNextPressTime[key] = StartAutoPressKeyLong(bmp, scaleX, scaleY, key);
                }
            }
            else
            {
                dicKeysNextPressTime.Add(key, StartAutoPressKeyLong(bmp, scaleX, scaleY, key));
            }
        }


        private void SaveDebugImage()
        {
            float dpiX = DpiScaleX;
            float dpiY = DpiScaleY;

            int width = (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * dpiX);
            int height = (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * dpiY);
            Size s = new Size(width, height);
            Bitmap memoryImage = new Bitmap(width, height);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);

            DrawDebugLine(0.253125, 0.37685, memoryImage);
            DrawDebugLine(0.253125, 0.823148, memoryImage);
            DrawDebugLine(0.127083, 0.6, memoryImage);
            DrawDebugLine(0.378125, 0.6, memoryImage);
            DrawDebugLine(0.74739583, 0.37685, memoryImage);
            DrawDebugLine(0.74739583, 0.823148, memoryImage);
            DrawDebugLine(0.62135416, 0.6, memoryImage);
            DrawDebugLine(0.872917, 0.6, memoryImage);

            memoryImage.Save(Application.StartupPath + @"\text.jpg");
        }

        private void SaveDebugImagebk()
        {
            float dpiX = DpiScaleX;
            float dpiY = DpiScaleY;

            int width = (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * dpiX);
            int height = (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * dpiY);
            Size s = new Size(width, height);
            Bitmap memoryImage = new Bitmap(width, height);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);

            DrawDebugLine(0.253125, 0.37685, memoryImage);
            DrawDebugLine(0.253125, 0.823148, memoryImage);
            DrawDebugLine(0.127083, 0.6, memoryImage);
            DrawDebugLine(0.378125, 0.6, memoryImage);
            DrawDebugLine(0.74739583, 0.37685, memoryImage);
            DrawDebugLine(0.74739583, 0.823148, memoryImage);
            DrawDebugLine(0.62135416, 0.6, memoryImage);
            DrawDebugLine(0.872917, 0.6, memoryImage);

            memoryImage.Save(Application.StartupPath + @"\textbk.jpg");
        }

        private void DrawDebugLine(double scaleX, double scaleY, Bitmap highDpiScreenshot)
        {
            int xw = (int)Math.Round(genshinWindowWdith * scaleX) + genshinWindowX;
            int yw = (int)Math.Round(genshinWindowHeight * scaleY) + genshinWindowY;
            for (int i = 0; i < highDpiScreenshot.Width; i++)
            {
                highDpiScreenshot.SetPixel(i, yw, Color.Red);
            }
            for (int i = 0; i < highDpiScreenshot.Height; i++)
            {
                highDpiScreenshot.SetPixel(xw, i, Color.Red);
            }
        }

        private DateTime StartAutoPressKey(Bitmap bmp, double scaleX, double scaleY, Keys key)
        {
            DateTime nextEnablePressTime;
            bool getReady = key switch
            {
                Keys.W => wReady,
                Keys.S => sReady,
                Keys.A => aReady,
                Keys.D => dReady,
                Keys.I => iReady,
                Keys.K => kReady,
                Keys.J => jReady,
                Keys.L => lReady,
                _ => throw new Exception()
            };
            //是否按下
            bool isPressed = key switch
            {
                Keys.W => wisPressed,
                Keys.S => sisPressed,
                Keys.A => aisPressed,
                Keys.D => disPressed,
                Keys.I => iisPressed,
                Keys.K => kisPressed,
                Keys.J => jisPressed,
                Keys.L => lisPressed,
                _ => throw new Exception()
            };

            int x = (int)Math.Round(genshinWindowWdith * scaleX) + genshinWindowX;
            int y = (int)Math.Round(genshinWindowHeight * scaleY) + genshinWindowY;
            nextEnablePressTime = PressKey(ref getReady,ref isPressed, bmp, x, y, key);

            switch (key)
            {
                case Keys.W:
                    wReady = getReady;
                    break;
                case Keys.S:
                    sReady = getReady;
                    break;
                case Keys.A:
                    aReady = getReady;
                    break;
                case Keys.D:
                    dReady = getReady;
                    break;
                case Keys.I:
                    iReady = getReady;
                    break;
                case Keys.K:
                    kReady = getReady;
                    break;
                case Keys.J:
                    jReady = getReady;
                    break;
                case Keys.L:
                    lReady = getReady;
                    break;
            }
            return nextEnablePressTime;
        }

        private DateTime StartAutoPressKeyLong(Bitmap bmp, double scaleX, double scaleY, Keys key)
        {
            DateTime nextEnablePressTime;
            bool getReadyLong = key switch
            {
                Keys.W => wReadyLong,
                Keys.S => sReadyLong,
                Keys.A => aReadyLong,
                Keys.D => dReadyLong,
                Keys.I => iReadyLong,
                Keys.K => kReadyLong,
                Keys.J => jReadyLong,
                Keys.L => lReadyLong,
                _ => throw new Exception()
            };
            //是否按下
            bool isPressed = key switch
            {
                Keys.W => wisPressed,
                Keys.S => sisPressed,
                Keys.A => aisPressed,
                Keys.D => disPressed,
                Keys.I => iisPressed,
                Keys.K => kisPressed,
                Keys.J => jisPressed,
                Keys.L => lisPressed,
                _ => throw new Exception()
            };

            int x = (int)Math.Round(genshinWindowWdith * scaleX) + genshinWindowX;
            int y = (int)Math.Round(genshinWindowHeight * scaleY) + genshinWindowY;
            nextEnablePressTime = PressKeyLong(ref getReadyLong, ref isPressed, bmp, x, y, key);
            switch (key)
            {
                case Keys.W:
                    wReadyLong = getReadyLong;
                    break;
                case Keys.S:
                    sReadyLong = getReadyLong;
                    break;
                case Keys.A:
                    aReadyLong = getReadyLong;
                    break;
                case Keys.D:
                    dReadyLong = getReadyLong;
                    break;
                case Keys.I:
                    iReadyLong = getReadyLong;
                    break;
                case Keys.K:
                    kReadyLong = getReadyLong;
                    break;
                case Keys.J:
                    jReadyLong = getReadyLong;
                    break;
                case Keys.L:
                    lReadyLong = getReadyLong;
                    break;
            }

            switch (key)
            {
                case Keys.W:
                    wisPressed = isPressed;
                    break;
                case Keys.S:
                    sisPressed = isPressed;
                    break;
                case Keys.A:
                    aisPressed = isPressed;
                    break;
                case Keys.D:
                    disPressed = isPressed;
                    break;
                case Keys.I:
                    iisPressed = isPressed;
                    break;
                case Keys.K:
                    kisPressed = isPressed;
                    break;
                case Keys.J:
                    jisPressed = isPressed;
                    break;
                case Keys.L:
                    lisPressed = isPressed;
                    break;
            }
            return nextEnablePressTime;
        }


        private byte GetScancode(Keys key) => key switch
        {
            Keys.W => Convert.ToByte(0x11),
            Keys.S => Convert.ToByte(0x1F),
            Keys.A => Convert.ToByte(0x1E),
            Keys.D => Convert.ToByte(0x20),
            Keys.I => Convert.ToByte(0x17),
            Keys.K => Convert.ToByte(0x25),
            Keys.J => Convert.ToByte(0x24),
            Keys.L => Convert.ToByte(0x26),
            _ => throw new Exception()
        };

        int i = 1;



        private DateTime PressKeyLong(ref bool getReadyLong, ref bool isPressed, Bitmap bmp, int x, int y, Keys key)
        {
            Color color = bmp.GetPixel(x, y);

            if (!getReadyLong)
            {
                if (color.R >40 && color.R < 100 && color.G >40  && color.G < 100 && color.B >40 && color.B < 100)
                {
                    getReadyLong = true;
                    Invoke(new Action(() => debugTextBox.AppendText($"getReadyLong，坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));
                    return DateTime.Now;
                }
            }
            if (getReadyLong)
            {
                if (isPressed)
                {
                    //已经按下
                    if (color.R < 80 && color.G < 80 && color.B < 80)
                    {
                        isPressed = false;
                        getReadyLong = false;
                        byte byteKey = (byte)key;
                        //getReady = false; 
                        byte code = GetScancode(key);
                        keybd_event(byteKey, code, 2, 0);
                        if (debugTextBox.Visible)
                        {
                            Invoke(new Action(() => debugTextBox.AppendText($"第{i++}次按下按键，长按放开,坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));
                        }
                        return DateTime.Now.AddMilliseconds(100);
                    }
                    return DateTime.Now;
                }
                //118,139,231
                //没按下的如果颜色符合，这个时候按下
                else if (isPressed == false && color.R > 145 && color.R < 160 && color.G > 145 && color.G < 160 && color.B > 145 && color.B < 160)
                //else if (isPressed == false && color.R > 100 && color.R < 140 && color.G > 110 && color.G < 160 && color.B > 220 && color.B < 255)
                {
                    isPressed = true;
                    byte byteKey = (byte)key;
                    //getReady = false; 
                    byte code = GetScancode(key);
                    keybd_event(byteKey, code, 0, 0);
                    if (debugTextBox.Visible)
                    {
                        Invoke(new Action(() => debugTextBox.AppendText($"第{i++}次按下按键，长按按下,坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));
                    }
                    return DateTime.Now.AddMilliseconds(100);
                }
            }
            return DateTime.Now;
        }
        //124,138,255 //长按标记开始的颜色

        private DateTime PressKey(ref bool getReady,ref bool isPressed, Bitmap bmp, int x, int y, Keys key)
        {
            if(isPressed)
            {
                //已经长按按下了这个键，不能再短按
                return DateTime.Now;
            }
            Color color = bmp.GetPixel(x, y);

            if (!getReady)
            {
                if ( color.R < 100 &&  color.G < 100 && color.B < 100)
                {
                    getReady = true;
                    Invoke(new Action(() => debugTextBox.AppendText($"getReady，坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));

                }
            }
            if (getReady)
            {
                if (color.R > 240 && color.G > 210 && color.G < 240 && ((color.B > 50 &&  color.B < 100))) //color.B < 80) || (color.B > 120 &&
                {
                    byte byteKey = (byte)key;
                    //getReady = false;  //连击不会显示黑色圈导致判定有问题, 现只用作判断是否开始音游
                    byte code = GetScancode(key);
                    keybd_event(byteKey, code, 0, 0);
                    if (debugTextBox.Visible)
                    {
                        Invoke(new Action(() => debugTextBox.AppendText($"第{i++}次按下按键,短按按下,坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));
                    }
                    Task.Delay(50).ContinueWith(_ =>
                    {
                        keybd_event(byteKey, code, 2, 0);
                        Invoke(new Action(() => debugTextBox.AppendText($"第{i++}次按下按键，短按松开,坐标为{x},{y},颜色值{color.R},{color.G},{color.B}, 键为:{((Keys)key).ToString()} ----{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\r\n")));
                    });
                    return DateTime.Now.AddMilliseconds(100);
                }
            }
            return DateTime.Now;
        }
    }
}
