using System;
using Gtk;

namespace RemoteCW
{
    class Program
    {
        public const int DEFAULT_SPEED = 20;
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.RemoteCW.RemoteCW", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            NetworkDriver nd = new NetworkDriver();
            SerialDriver sd = new SerialDriver();
            KeyDriver kd = new KeyDriver(sd, nd);
            AudioDriver ad = new AudioDriver(kd);
            var win = new MainWindow(ad, sd, kd, nd);
            app.AddWindow(win);

            win.Show();
            Application.Run();
            sd.Stop();
            ad.Stop();
            nd.Stop();
        }
    }
}
