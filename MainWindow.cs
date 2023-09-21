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
        [UI] private Button buttonNetwork = null;
        //[UI] private Button buttonSpot = null;
        [UI] private ToggleButton toggleSpot = null;
        SerialDriver serialDriver;
        KeyDriver keyDriver;
        NetworkDriver networkDriver;
        AudioDriver audioDriver;
        bool iambic = true;

        public MainWindow(AudioDriver audioDriver, SerialDriver serialDriver, KeyDriver keyDriver, NetworkDriver networkDriver) : this(new Builder("MainWindow.glade"))
        {
            this.audioDriver = audioDriver;
            this.serialDriver = serialDriver;
            this.keyDriver = keyDriver;
            this.networkDriver = networkDriver;
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
            spinWPM.Value = Program.DEFAULT_SPEED;
            spinWPM.ValueChanged += wpmChanged;
            buttonMode.Clicked += modeChanged;
            buttonNetwork.Clicked += networkChanged;
            toggleSpot.Toggled += spotChanged;
            //buttonSpot.Pressed += spotOn;
            //buttonSpot.Released += spotOff;
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

        private void networkChanged(object o, EventArgs args)
        {
            networkDriver.send = !networkDriver.send;
            if (networkDriver.send)
            {
                buttonNetwork.Label = "Network On";
            }
            else
            {
                buttonNetwork.Label = "Network Off";
            }
            keyDriver.SetMode(iambic);
        }

        private void spotChanged(object o, EventArgs args)
        {
            audioDriver.spot = toggleSpot.Active;
        }

        /*
        private void spotOn(object o, EventArgs args)
        {
            audioDriver.spot = true;
        }
        
        private void spotOff(object o, EventArgs args)
        {
            audioDriver.spot = false;
        }
        */

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}
