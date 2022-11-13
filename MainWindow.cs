using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace RemoteCW
{
    class MainWindow : Window
    {
        [UI] private Label labelStatus = null;
        [UI] private SpinButton spinWPM = null;
        [UI] private Button buttonMode = null;
        SerialDriver serialDriver;
        KeyDriver keyDriver;
        bool iambic = true;

        public MainWindow(SerialDriver serialDriver, KeyDriver keyDriver) : this(new Builder("MainWindow.glade"))
        {
            this.serialDriver = serialDriver;
            this.keyDriver = keyDriver;
            serialDriver.SetUICallback(UpdateWindow);
        }

        private void UpdateWindow(string sendText)
        {
            Application.Invoke(delegate
            {
                labelStatus.Text = sendText;
            }
            );
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);
            DeleteEvent += Window_DeleteEvent;
            spinWPM.ValueChanged += wpmChanged;
            buttonMode.Clicked += modeChanged;
            labelStatus.Text = "Waiting for serial input";
        }

        private void wpmChanged(object o, EventArgs args)
        {
            keyDriver.SetWPM((int)spinWPM.Value);
        }

        private void modeChanged(object o, EventArgs args)
        {
            iambic = !iambic;
            if (iambic)
            {
                buttonMode.Label = "Iambic";
            }
            else
            {
                buttonMode.Label = "Straight";
            }
            keyDriver.SetMode(iambic);
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}
