using ExcelDataReader;
using MaterialDesignThemes.Wpf;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using SNNetsisStokTakip.Classes;
using SNNetsisStokTakip.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SNNetsisStokTakip.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public static Snackbar Snackbar;

        public DataView Dv
        {
            get { return (DataView)GetValue(DvProperty); }
            set { SetValue(DvProperty, value); }
        }

        public static readonly DependencyProperty DvProperty =
            DependencyProperty.Register("Dv", typeof(DataView), typeof(MainWindow));

        public MainWindow()
        {
            InitializeComponent();

            Snackbar = this.MainSnackbar;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDgDataSource();

        }

        private async Task LoadDgDataSource()
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    dgDb.ItemsSource = ConnectionManagement.StocksTable;
                    dgDb.Columns[0].IsReadOnly = true;
                    Snackbar.MessageQueue.Enqueue("Stoklar Yüklendi");
                    pbProcess.Visibility = Visibility.Hidden;
                });
            });
        }

        private void DgStockFilter(string stockKod)
        {
            ConnectionManagement.StocksTable.RowFilter = "(STOK_KODU like '" + stockKod + "*')"; // Stok_kodu - Adet
        }

        private async void btnProcessStart_Click(object sender, RoutedEventArgs e)
        {
            pbProcess.Visibility = Visibility.Visible;
            successStocks.Clear();
            faultStocks.Clear();
            notrStocks.Clear();
            await ProcessThread();
        }

        int totalSuccess, totalFailed;

        List<ModelStock> successStocks = new List<ModelStock>();
        List<ModelStock> faultStocks = new List<ModelStock>();
        List<ModelStock> notrStocks = new List<ModelStock>();
        private async Task ProcessThread()
        {
            await Task.Run(() =>
            {
                totalSuccess = totalFailed = 0;
                if (dtStock != null)
                {
                    for (int i = 0; i < dtStock.DefaultView.Count; i++)
                    {

                        var stockCode = dtStock.Rows[i][0].ToString();
                        var amount = dtStock.Rows[i][1].ToString();

                        int x = 0;

                        Int32.TryParse(amount, out x);

                        lblProgresRecord.Dispatcher.Invoke(() => { lblProgresRecord.Content = stockCode; });
                        var result = ConnectionManagement.SqlOperations.InsertNewStockAmount(stockCode, x);
                        if (result == 1)
                        {
                            totalSuccess++;
                            successStocks.Add(new ModelStock { StockCode = stockCode, Amount = x });
                        }
                        else if (result == 0)
                        {
                            notrStocks.Add(new ModelStock { StockCode = stockCode, Amount = x });
                        }
                        else
                        {
                            faultStocks.Add(new ModelStock { StockCode = stockCode, Amount = x });
                            totalFailed++;
                        }

                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        dgExcel.ItemsSource = dtStock.DefaultView;
                    });
                }

                this.Dispatcher.Invoke(() =>
                {
                    lblProgresRecord.Content = string.Format("{0} Başarılı - {1} Hatalı", totalSuccess, totalFailed);
                    pbProcess.Visibility = Visibility.Hidden;
                    btnRefreshStocks_Click(this, null);
                });
            });
        }

        #region Excel

        Microsoft.Office.Interop.Excel.Application excel;
        Microsoft.Office.Interop.Excel.Workbook workBook;
        Microsoft.Office.Interop.Excel.Worksheet workSheet;
        Microsoft.Office.Interop.Excel.Range cellRange;
        System.Data.DataTable dtStock;

        private void btnOpenExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var openResult = (bool)openFileDialog.ShowDialog();

            if (openResult)
            {
                using (var stream = File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {

                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {

                        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        var tablenames = GetTablenames(dataSet.Tables);

                        dtStock = dataSet.Tables[0];
                        var col = dtStock.Columns.Add("Sonuc", typeof(string));

                        dgExcel.ItemsSource = dtStock.DefaultView;

                        dgExcel.Columns[0].IsReadOnly = true;
                        dgExcel.Columns[1].IsReadOnly = false;
                    }
                }
            }
        }

        private static IList<string> GetTablenames(DataTableCollection tables)
        {
            var tableList = new List<string>();
            foreach (var table in tables)
            {
                tableList.Add(table.ToString());
            }

            return tableList;
        }

        private void btnToCsv_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcelAndCsv();
        }

        private void ExportToExcelAndCsv()
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            dt = ((DataView)dgDb.ItemsSource).ToTable();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "excel (*.xlsx)|*.xlsx";
            if (saveFileDialog.ShowDialog() == true)
            {
                GenerateExcel(dt);
                workBook.SaveAs(saveFileDialog.FileName);
                workBook.Close(); excel.Quit();

            }
        }

        private void GenerateExcel(System.Data.DataTable DtIN)
        {
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.DisplayAlerts = false;
                excel.Visible = false;
                workBook = excel.Workbooks.Add(Type.Missing);
                workSheet = (Microsoft.Office.Interop.Excel.Worksheet)workBook.ActiveSheet;
                workSheet.Name = "NetsisStokListesi";
                System.Data.DataTable tempDt = DtIN;
                //dgExcel.ItemsSource = tempDt.DefaultView;
                workSheet.Cells.Font.Size = 11;
                int rowcount = 1;
                for (int i = 1; i <= tempDt.Columns.Count; i++) //taking care of Headers.  
                {
                    workSheet.Cells[1, i] = tempDt.Columns[i - 1].ColumnName;
                }
                foreach (System.Data.DataRow row in tempDt.Rows) //taking care of each Row  
                {
                    rowcount += 1;
                    for (int i = 0; i < tempDt.Columns.Count; i++) //taking care of each column  
                    {
                        workSheet.Cells[rowcount, i + 1] = row[i].ToString();
                    }
                }
                cellRange = workSheet.Range[workSheet.Cells[1, 1], workSheet.Cells[rowcount, tempDt.Columns.Count]];
                cellRange.EntireColumn.AutoFit();
            }
            catch (Exception)
            {
                throw;
            }
        }


        #endregion

        bool back = false;
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            back = true;
            this.Close();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (!back)
                ((SqlLogin)System.Windows.Application.Current.MainWindow).Close();
            else
                ((SqlLogin)System.Windows.Application.Current.MainWindow).Visibility = Visibility.Visible;
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            DgStockFilter(txtFilter.Text);
        }

        private void btnRefreshStocks_Click(object sender, RoutedEventArgs e)
        {
            var ex = ExceptionHelper.CatchException(() =>
            {
                ConnectionManagement.StocksTable = ConnectionManagement.SqlOperations.GetAllStocks();
                dgDb.ItemsSource = ConnectionManagement.StocksTable;
                dgDb.Columns[0].IsReadOnly = true;
                DgStockFilter(txtFilter.Text);
                Snackbar.MessageQueue.Enqueue("Stoklar Yeniden Yüklendi");
            });

            if (ex != null)
            {
                _ = MessageBox.Show(ex.Message, "Server Hatası");
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            int parsedValue;
            if (!int.TryParse(txtAmount.Text, out parsedValue))
            {
                Snackbar.MessageQueue.Enqueue("Adet Değeri Sayı Olmalı");
                return;
            }
            else
            {
                var result = ConnectionManagement.SqlOperations.InsertNewStockAmount(tbStockCode.Text, parsedValue);
                if (result == 1)
                {
                    Snackbar.MessageQueue.Enqueue("Stok Güncellendi");
                    tbAmount.Text = ConnectionManagement.SqlOperations.GetStock(tbStockCode.Text)[0]["Adet"].ToString().Split('.')[0].ToString();
                    DataRowView rowView = (DataRowView)dgDb.SelectedItem;
                    rowView.Row["Adet"] = tbAmount.Text;
                }

                else if (result == 0)
                {
                    Snackbar.MessageQueue.Enqueue("Stok Değeri Zaten Tanımlı");
                }
                else
                {
                    Snackbar.MessageQueue.Enqueue("Stok Bulunamadı");
                }
            }


        }

        private void btnSuccess_Click(object sender, RoutedEventArgs e)
        {
            if (successStocks != null)
                dgExcel.ItemsSource = successStocks;

        }

        private void btnFault_Click(object sender, RoutedEventArgs e)
        {
            if (faultStocks != null)
                dgExcel.ItemsSource = faultStocks;
        }

        private void btnOrginal_Click(object sender, RoutedEventArgs e)
        {
            if (dtStock != null)
                dgExcel.ItemsSource = dtStock.DefaultView;
        }

        private void dgDb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object item = dgDb.SelectedItem;
            if (item != null)
            {
                tbStockCode.Text = (dgDb.SelectedCells[0].Column.GetCellContent(item) as TextBlock).Text;
                txtAmount.Text = tbAmount.Text = (dgDb.SelectedCells[1].Column.GetCellContent(item) as TextBlock).Text.Split('.')[0].ToString();
            }
        }
    }


}
