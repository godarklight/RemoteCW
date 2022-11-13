using System;
using Gtk;

namespace RemoteCW
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.RemoteCW.RemoteCW", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            SerialDriver sd = new SerialDriver();
            KeyDriver kd = new KeyDriver(sd);
            AudioDriver ad = new AudioDriver(kd);
            var win = new MainWindow(sd, kd);
            app.AddWindow(win);

            win.Show();
            Application.Run();
            kd.Stop();
        }
    }
}
