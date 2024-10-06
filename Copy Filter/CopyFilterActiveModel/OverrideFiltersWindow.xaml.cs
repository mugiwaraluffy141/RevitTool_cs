using System.Windows;

namespace MinhTranTools.CopyFilterActiveModel
{
    public partial class OverrideConfirmationWindow : Window
    {
        public bool UserResponse { get; private set; }
        public bool Confirm { get; set; }


        public OverrideConfirmationWindow()
        {
            InitializeComponent();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            UserResponse = true;
            this.DialogResult = true;
            this.Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            UserResponse = false;
            this.DialogResult = false;
            this.Close();
        }

        // Handle the closing event to prevent action when the window is closed using the "X" button
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Confirm = true;
            // Check if DialogResult is null (this happens when the window is closed via "X" button)
            if (this.DialogResult == null)
            {
                // Do nothing, just close the window without setting UserResponse or DialogResult
                Confirm = false;  // Ensures the window closes
                this.Close ();
            }
        }
    }
}
