using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kolibrie_Mail
{
    /// <summary>
    /// Interaction logic for OAuthVerificationForm.xaml
    /// </summary>
    public partial class OAuthVerificationForm : Window, IDisposable
    {
        public OAuthVerificationForm()
        {
            InitializeComponent();
        }

        public string VerificationCode { get; private set; }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            VerificationCode = textBox1.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void Dispose()
        {
            this.Close();
        }
    }
}
