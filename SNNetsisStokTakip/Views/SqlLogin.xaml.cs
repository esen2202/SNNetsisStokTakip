using SNNetsisStokTakip.Classes;
using SNNetsisStokTakip.Models;
using SNNetsisStokTakip.Properties;
using System.Threading.Tasks;
using System.Windows;

namespace SNNetsisStokTakip.Views
{
    /// <summary>
    /// Interaction logic for SqlLogin.xaml
    /// </summary>
    public partial class SqlLogin : Window
    {
        public bool DbSelectionActive { get; private set; }

        public SqlLogin()
        {
            InitializeComponent();

            ConnectionManagement.SqlOperations = new SQLOperations();

            ChangePanel(false);
            GetServerSettings();
        }

        private void GetServerSettings()
        {
            txtServer.Text = Settings.Default.Server;
            txtUser.Text = Settings.Default.User;
            txtPass.Password = Settings.Default.Pass;
            cbDbList.Text = Settings.Default.Db;
        }

        private void GetDbSettings()
        {
            int index = cbDbList.Items.IndexOf(Settings.Default.Db);
            cbDbList.SelectedItem = cbDbList.Items[index];
        }

        private void SetServerSettings()
        {
            Settings.Default.Server = txtServer.Text;
            Settings.Default.User = txtUser.Text;
            Settings.Default.Pass = txtPass.Password;
            Settings.Default.Save();
        }

        private void SetDbSettings()
        {
            Settings.Default.Db = cbDbList.Text;
            Settings.Default.Save();
        }

        private async Task FillDbNamesToCombobox()
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    ConnectionManagement.ConnStr = new ModelConnStr
                    {
                        Server = txtServer.Text,
                        User = txtUser.Text,
                        Pass = txtPass.Password
                    };

                    var ex = ExceptionHelper.CatchException(() =>
                    {
                        cbDbList.ItemsSource = ConnectionManagement.SqlOperations.GetDBNames(ConnectionManagement.ConnStr);
                    });

                    if (ex == null)
                    {
                        DbSelectionActive = cbDbList.Items.Count > 0;

                        ChangePanel(DbSelectionActive, true);

                        if (cbDbList.Items.Contains(Settings.Default.Db))
                            GetDbSettings();
                    }
                    else
                    {
                        _ = MessageBox.Show(ex.Message, "Server Hatası");
                    }

                });
            });
        }

        private void ChangePanel(bool dbNameSelectionActive, bool saveActive = false)
        {
            if (dbNameSelectionActive)
            {
                spServerPanel.Visibility = Visibility.Collapsed;
                spDbNameSelectionPanel.Visibility = Visibility.Visible;
            }
            else
            {
                spServerPanel.Visibility = Visibility.Visible;
                spDbNameSelectionPanel.Visibility = Visibility.Collapsed;
            }
            if (saveActive)
            {
                SetServerSettings();
            }
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            await FillDbNamesToCombobox();
        }

        private void btnDbSelect_Click(object sender, RoutedEventArgs e)
        {
            var ex = ExceptionHelper.CatchException(() =>
            {
                if (ConnectionManagement.ConnStr != null)
                    ConnectionManagement.ConnStr.DbName = cbDbList.SelectedItem.ToString();
                ConnectionManagement.StocksTable = ConnectionManagement.SqlOperations.GetAllStocks();
            });

            if (ex == null)
            {
                MainWindow mainWindow = new MainWindow();
                this.Visibility = Visibility.Hidden;
                mainWindow.Show();
            }
            else
            {
                _ = MessageBox.Show(ex.Message, "Server Hatası");
            }

        }

        private void btnBackServer_Click(object sender, RoutedEventArgs e)
        {
            ChangePanel(false);
        }

        private void cbDbList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ConnectionManagement.ConnStr != null)
                ConnectionManagement.ConnStr.DbName = cbDbList.SelectedItem.ToString();
            else
                ConnectionManagement.ConnStr = new ModelConnStr
                {
                    Server = txtServer.Text,
                    User = txtUser.Text,
                    Pass = txtPass.Password,
                    DbName = cbDbList.Text
                };
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
