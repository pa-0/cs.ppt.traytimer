using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace ZTrayClock
{
    public class ZTrayClock : ApplicationContext
    {
        private NotifyIcon iconHour;
        private NotifyIcon iconMinute;
        private ContextMenuStrip contextMenuStrip;
        private Timer timer;

        static int iconSize = 16;
        static float fontSize = 14F;
        static FontFamily ff = new FontFamily("Calibri");
        Font fbold = new Font(ff, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        Font freg  = new Font(ff, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

        public ZTrayClock() {
            iconHour = new NotifyIcon();
            iconMinute = new NotifyIcon();
            timer = new Timer();

            iconHour.Icon = DrawHour();
            iconMinute.Icon = DrawMinute();
            iconHour.Visible = true;
            iconMinute.Visible = true;

            iconHour.DoubleClick   += new EventHandler(iconHour_DoubleClick);
            iconMinute.DoubleClick += new EventHandler(iconHour_DoubleClick);

            // set up the context menu items
            ToolStripMenuItem mitemAdjTimeDate = new ToolStripMenuItem("&Adjust time/date");
            mitemAdjTimeDate.Font = new Font(mitemAdjTimeDate.Font, FontStyle.Bold);
            mitemAdjTimeDate.Click += new EventHandler(mitemAdjTimeDate_Click);
            ToolStripSeparator mitemSeparator = new ToolStripSeparator();
            ToolStripMenuItem mitemExit = new ToolStripMenuItem("E&xit");
            mitemExit.Click += new EventHandler(mitemExit_Click);
            // set up the context menu
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { mitemAdjTimeDate, mitemSeparator, mitemExit });
            iconHour.ContextMenuStrip = iconMinute.ContextMenuStrip = contextMenuStrip;

            this.ThreadExit += new EventHandler(ZTrayClock_ThreadExit);
        }

        void mitemExit_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        void mitemAdjTimeDate_Click(object sender, EventArgs e) {
            startTimeDateCpl();
        }

        void ZTrayClock_ThreadExit(object sender, EventArgs e) {
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
            string hour = String.Format("{0:hh}", DateTime.Now);

            Bitmap b = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
            Graphics gb = Graphics.FromImage(b);

            gb.Clear(Color.Transparent);
            gb.SmoothingMode = SmoothingMode.AntiAlias;

            SizeF hourboldsize = gb.MeasureString(hour, fbold);
            SizeF hourregsize  = gb.MeasureString(hour, freg);
            gb.DrawString(hour, freg, new SolidBrush(Color.Black), (iconSize-hourregsize.Width)+2, -3);
            gb.DrawString(hour, fbold, new SolidBrush(Color.White), (iconSize-hourboldsize.Width)+1, -4);

            return Icon.FromHandle(b.GetHicon());
        }

        public Icon DrawMinute() {
            string minute = String.Format("{0:mm}", DateTime.Now);
            string ampm = String.Format("{0:tt}", DateTime.Now);
            Font ampmFont = new Font(ff, 6, FontStyle.Bold, GraphicsUnit.Pixel);

            Bitmap b = new Bitmap(iconSize, iconSize, PixelFormat.Format32bppArgb);
            Graphics gb = Graphics.FromImage(b);

            gb.Clear(Color.Transparent);
            gb.SmoothingMode = SmoothingMode.AntiAlias;

            SizeF minuteboldsize = gb.MeasureString(minute, fbold);
            SizeF minuteregsize  = gb.MeasureString(minute, freg);
            gb.DrawString(minute, freg, new SolidBrush(Color.Black), -1, -3);
            gb.DrawString(minute, fbold, new SolidBrush(Color.White), -2, -4);

            SizeF ampmsize = gb.MeasureString(ampm,ampmFont);
            gb.DrawString(String.Format("{0:tt}", DateTime.Now), ampmFont, new SolidBrush(Color.White), (iconSize - ampmsize.Width), (iconSize - ampmsize.Height));
            
            return Icon.FromHandle(b.GetHicon());
        }
    }
}
