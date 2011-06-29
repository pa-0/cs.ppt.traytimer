using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ZTrayClock
{
    public enum TimeFormat { TwelveHour, TwentyFourHour };
    public class ZTrayClock : ApplicationContext
    {
        private NotifyIcon iconHour;
        private NotifyIcon iconMinute;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem mitem12HourFormat, mitem24HourFormat, mitemDisplayAMPM;
        private Timer timer;

        private static int iconSize = 16;
        private Font font = new Font(Properties.Settings.Default.Font.FontFamily, Properties.Settings.Default.Font.Size, Properties.Settings.Default.FontStyle, GraphicsUnit.Pixel);

        private RegistryKey hkcu = Registry.CurrentUser;

        public ZTrayClock() {
            iconHour = new NotifyIcon();
            iconMinute = new NotifyIcon();
            timer = new Timer();

            iconHour.Icon   = DrawHour();
            iconMinute.Icon = DrawMinute();
            iconHour.Text = iconMinute.Text = DateTime.Now.ToLongDateString();
            iconHour.Visible   = true;
            iconMinute.Visible = true;

            iconHour.DoubleClick   += new EventHandler(iconHour_DoubleClick);
            iconMinute.DoubleClick += new EventHandler(iconHour_DoubleClick);

            // set up the context menu items
            ToolStripMenuItem mitemZTrayClock = new ToolStripMenuItem("ZTrayClock (Built from Git commit " + Properties.Resources.revision.TrimEnd() + ")");
            mitemZTrayClock.Enabled = false;

            ToolStripMenuItem mitemAdjTimeDate = new ToolStripMenuItem("&Adjust time/date");
            mitemAdjTimeDate.Font = new Font(mitemAdjTimeDate.Font, FontStyle.Bold);
            mitemAdjTimeDate.Click += new EventHandler(mitemAdjTimeDate_Click);

            ToolStripMenuItem mitemChangeFont = new ToolStripMenuItem("Change &font...");
            mitemChangeFont.Click += new EventHandler(mitemChangeFont_Click);

            ToolStripMenuItem mitemLeadingZeroesHours = new ToolStripMenuItem("Display leading zeroes on &hours");
            mitemLeadingZeroesHours.Checked = Properties.Settings.Default.LeadingZeroesHours;
            mitemLeadingZeroesHours.Click += new EventHandler(mitemLeadingZeroesHours_Click);

            ToolStripMenuItem mitemLeadingZeroesMinutes = new ToolStripMenuItem("Display leading zeroes on &minutes");
            mitemLeadingZeroesMinutes.Checked = Properties.Settings.Default.LeadingZeroesMinutes;
            mitemLeadingZeroesMinutes.Click += new EventHandler(mitemLeadingZeroesMinutes_Click);

            mitem12HourFormat = new ToolStripMenuItem("12-hour format");
            mitem24HourFormat = new ToolStripMenuItem("24-hour format");
            switch ((TimeFormat)Properties.Settings.Default.TimeFormat) {
                case TimeFormat.TwelveHour: mitem12HourFormat.Checked = true; break;
                case TimeFormat.TwentyFourHour: mitem24HourFormat.Checked = true; break;
            }
            mitem12HourFormat.Click += new EventHandler(mitem12HourFormat_Click);
            mitem24HourFormat.Click += new EventHandler(mitem24HourFormat_Click);

            mitemDisplayAMPM = new ToolStripMenuItem("Display AM/PM");
            mitemDisplayAMPM.Checked = Properties.Settings.Default.DisplayAMPM;
            mitemDisplayAMPM.Click += new EventHandler(mitemDisplayAMPM_Click);

            ToolStripMenuItem mitemStartAtLogon = new ToolStripMenuItem("Start at logon");
            try {
                RegistryKey run = hkcu.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                string value = (string)(run.GetValue("ZTrayClock"));
                mitemStartAtLogon.Checked = value.Equals(String.Format("\"{0}\"", Application.ExecutablePath));
            } catch (Exception) {
                mitemStartAtLogon.Checked = false;
            }
            mitemStartAtLogon.Click += new EventHandler(mitemStartAtLogon_Click);

            ToolStripMenuItem mitemExit = new ToolStripMenuItem("E&xit");
            mitemExit.Click += new EventHandler(mitemExit_Click);
            
            // set up the context menu
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { 
                mitemZTrayClock, new ToolStripSeparator(),
                mitemAdjTimeDate, mitemChangeFont, new ToolStripSeparator(),
                mitemLeadingZeroesHours, mitemLeadingZeroesMinutes, mitem12HourFormat, mitem24HourFormat, mitemDisplayAMPM, new ToolStripSeparator(),
                mitemStartAtLogon, new ToolStripSeparator(),
                mitemExit
            });
            iconHour.ContextMenuStrip = iconMinute.ContextMenuStrip = contextMenuStrip;

            timer.Interval = 10;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Enabled = true;

            SystemEvents.TimeChanged += new EventHandler(SystemEvents_TimeChanged);
            this.ThreadExit += new EventHandler(ZTrayClock_ThreadExit);
        }

        void mitemStartAtLogon_Click(object sender, EventArgs e) {
            var mitem = sender as ToolStripMenuItem;

            RegistryKey run;
            try {
                run = hkcu.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            } catch (Exception ex) {
                MessageBox.Show("Failed to change start at logon state.  Couldn't open registry key " + @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run" + "\n" +
                    "Error:" + ex.Message, "ZTrayClock", MessageBoxButtons.OK,MessageBoxIcon.Error);
                mitem.Checked = false;
                return;
            }

            string value = (string)(run.GetValue("ZTrayClock",""));
            if (value.Equals(String.Format("\"{0}\"", Application.ExecutablePath))) {
                try {
                    run.DeleteValue("ZTrayClock");
                    mitem.Checked = false;
                    return;
                } catch (Exception ex) {
                    MessageBox.Show("Failed to disable start at login.  Couldn't delete registry value " + @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ZTrayClock." + "\n" +
                        "Error: " + ex.Message, "ZTrayClock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    mitem.Checked = true;
                    return;
                }
            } else {
                try {
                    run.SetValue("ZTrayClock", String.Format("\"{0}\"", Application.ExecutablePath), RegistryValueKind.String);
                    mitem.Checked = true;
                    return;
                } catch (Exception ex) {
                    MessageBox.Show("Failed to enable start at login.  Couldn't create registry value " + @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ZTrayClock." + "\n" +
                        "Error: " + ex.Message, "ZTrayClock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    mitem.Checked = false;
                    return;
                }
            }
        }

        void SystemEvents_TimeChanged(object sender, EventArgs e) {
            timer_Tick(sender, e);
        }

        void mitem24HourFormat_Click(object sender, EventArgs e) {
            if (mitem12HourFormat.Checked && !mitem24HourFormat.Checked) {
                mitem12HourFormat.Checked = false; mitem24HourFormat.Checked = true;
                Properties.Settings.Default.TimeFormat = (int)(TimeFormat.TwentyFourHour);
            }
            Properties.Settings.Default.DisplayAMPM = mitemDisplayAMPM.Checked = false;
            Properties.Settings.Default.Save();
            timer_Tick(sender, e);
        }

        void mitem12HourFormat_Click(object sender, EventArgs e) {
            if (mitem24HourFormat.Checked && !mitem12HourFormat.Checked) {
                mitem24HourFormat.Checked = false; mitem12HourFormat.Checked = true;
                Properties.Settings.Default.TimeFormat = (int)(TimeFormat.TwelveHour);
            }
            Properties.Settings.Default.DisplayAMPM = mitemDisplayAMPM.Checked = true;
            Properties.Settings.Default.Save();
            timer_Tick(sender, e);
        }

        void mitemLeadingZeroesHours_Click(object sender, EventArgs e) {
            var mitem = sender as ToolStripMenuItem;
            mitem.Checked = !mitem.Checked;
            Properties.Settings.Default.LeadingZeroesHours = mitem.Checked;
            Properties.Settings.Default.Save();
            timer_Tick(sender, e);
        }

        void mitemLeadingZeroesMinutes_Click(object sender, EventArgs e) {
            var mitem = sender as ToolStripMenuItem;
            mitem.Checked = !mitem.Checked;
            Properties.Settings.Default.LeadingZeroesMinutes = mitem.Checked;
            Properties.Settings.Default.Save();
            timer_Tick(sender, e);
        }

        void mitemDisplayAMPM_Click(object sender, EventArgs e) {
            var mitem = sender as ToolStripMenuItem;
            mitem.Checked = !mitem.Checked;
            Properties.Settings.Default.DisplayAMPM = mitem.Checked;
            Properties.Settings.Default.Save();
            timer_Tick(sender, e);
        }

        void mitemChangeFont_Click(object sender, EventArgs e) {
            FontDialog fd = new FontDialog();
            fd.FontMustExist = true;
            fd.ShowEffects = false;
            fd.Font = font; // default to the currently chosen font

            DialogResult result = fd.ShowDialog();
            if (result == DialogResult.OK) {
                font = new Font(fd.Font.FontFamily, fd.Font.Size, fd.Font.Style);
                timer_Tick(sender, e);
            }
        }

        void timer_Tick(object sender, EventArgs e) {
            int seconds = DateTime.Now.Second;
            timer.Interval = (60 - seconds) * 1000;
            iconHour.Icon = DrawHour();
            iconMinute.Icon = DrawMinute();
            iconHour.Text = iconMinute.Text = DateTime.Now.ToLongDateString();
        }

        void mitemExit_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        void mitemAdjTimeDate_Click(object sender, EventArgs e) {
            startTimeDateCpl();
        }

        void ZTrayClock_ThreadExit(object sender, EventArgs e) {
            // save settings
            Properties.Settings.Default.Save();

            // disable timer just for cleanliness' sake
            timer.Enabled = false;

            // prevents stale icons from hanging around
            iconHour.Visible = false;
            iconMinute.Visible = false;
        }

        void iconHour_DoubleClick(object sender, EventArgs e) {
            startTimeDateCpl();
        }

        void startTimeDateCpl() {
            // TODO: is it different on different versions of windows?
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo(@"C:\Windows\System32\timedate.cpl");
            p.StartInfo = psi;
            p.Start();
        }

        public Icon DrawHour() {
            string hour;
            switch ((TimeFormat)Properties.Settings.Default.TimeFormat) {
                case TimeFormat.TwelveHour:
                    hour = DateTime.Now.ToString((Properties.Settings.Default.LeadingZeroesHours ? "hh" : "%h"));
                    break;
                case TimeFormat.TwentyFourHour:
                    hour = DateTime.Now.ToString((Properties.Settings.Default.LeadingZeroesHours ? "HH" : "%H"));
                    break;
                default: // so that the compiler won't complain about uninitialized variable usage (default to 12-hour)
                    hour = DateTime.Now.ToString((Properties.Settings.Default.LeadingZeroesHours ? "hh" : "%h"));
                    break;
            }

            Bitmap b = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
            Graphics gb = Graphics.FromImage(b);
            Icon i;

            gb.Clear(Color.Transparent);
            gb.SmoothingMode = SmoothingMode.AntiAlias;

            SizeF hourregsize  = gb.MeasureString(hour, font);
            gb.DrawString(hour, font, new SolidBrush(Color.Black), (iconSize-hourregsize.Width)+2, -2);
            gb.DrawString(hour, font, new SolidBrush(Color.White), (iconSize-hourregsize.Width)+1, -3);
            i = Icon.FromHandle(b.GetHicon());
            gb.Dispose();
            b.Dispose();

            return i;
        }

        public Icon DrawMinute() {
            string minute = DateTime.Now.ToString((Properties.Settings.Default.LeadingZeroesMinutes ? "mm" : "%m"));

            Bitmap b = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
            Graphics gb = Graphics.FromImage(b);
            Icon i;

            gb.Clear(Color.Transparent);
            gb.SmoothingMode = SmoothingMode.HighQuality;

            gb.DrawString(minute, font, new SolidBrush(Color.Black), -1, -2);
            gb.DrawString(minute, font, new SolidBrush(Color.White), -2, -3);

            if (Properties.Settings.Default.DisplayAMPM) {
                string ampm = String.Format("{0:tt}", DateTime.Now);
                Font ampmFont = new Font(font.FontFamily, 6, font.Style, GraphicsUnit.Pixel);
                SizeF ampmsize = gb.MeasureString(ampm, ampmFont);
                gb.DrawString(String.Format("{0:tt}", DateTime.Now), ampmFont, new SolidBrush(Color.White), (iconSize - ampmsize.Width), (iconSize - ampmsize.Height));
            }

            i = Icon.FromHandle(b.GetHicon());
            gb.Dispose();
            b.Dispose();

            return i;
        }
    }
}
