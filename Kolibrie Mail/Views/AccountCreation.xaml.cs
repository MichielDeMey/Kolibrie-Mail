using System.Net;
using System.Windows;
using Crystalbyte.Equinox.Security;
using Kolibrie_Mail.Model;

namespace Kolibrie_Mail.Views
{
    /// <summary>
    /// Interaction logic for AccountCreation.xaml
    /// </summary>
    public partial class AccountCreation : Window
    {
        public Account Account { get; set; }

        public AccountCreation()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            Account = new Account
                          {
                              Name = txtAccountName.Text,
                              Credential =
                                  new NetworkCredential {UserName = txtUsername.Text, Password = txtPassword.Password},
                              Host = txtHost.Text,
                              Security = GetSecurity()
                          };

            DialogResult = true;
            Close();
        }

        private SecurityPolicies GetSecurity()
        {
            if (cboSecurity.Text.Contains("TLS"))
            {
                return SecurityPolicies.Implicit;
            }

            if (cboSecurity.Text.Contains("SSL"))
            {
                return SecurityPolicies.Explicit;
            }

            return SecurityPolicies.None;
        }

    }
}
