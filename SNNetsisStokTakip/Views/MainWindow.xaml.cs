using ExcelDataReader;
using Microsoft.Win32;
using SNNetsisStokTakip.Classes;
using SNNetsisStokTakip.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;


namespace SNNetsisStokTakip.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtServer.Text = Settings.Default.Server;
            txtUser.Text = Settings.Default.User;
            txtPass.Text = Settings.Default.Pass;
            txtServer.Text = Settings.Default.Server;
        }

        private void Cb_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            cb.IsDropDownOpen = true;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            connectionString = new ModelConnStr
            {
                Server = txtServer.Text,
                User = txtUser.Text,
                Pass = txtPass.Text
            };

            cbDbList.ItemsSource = GetDBNames();
        }

        ModelConnStr connectionString;

        public string GenerateConnStr(ModelConnStr modelConnStr)
        {
            string connStr = "";
            if (modelConnStr != null)
            {
                connStr = string.Format("Data Source={0}", modelConnStr.Server);

                if (!string.IsNullOrEmpty(modelConnStr.DbName))
                    connStr = connStr + string.Format(";Initial Catalog={0}", modelConnStr.DbName);

                if (!string.IsNullOrEmpty(modelConnStr.User))
                    connStr = connStr + string.Format(";User ID={0};Password={1}", modelConnStr.User, modelConnStr.Pass);
            }
            return connStr;
        }

        public List<string> GetDBNames()
        {
            var result = new List<string>();
            using (var conn = new SqlConnection(GenerateConnStr(connectionString)))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", conn))
                {
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result.Add((string)dr[0]);
                        }
                    }
                    conn.Close();
                }
            }
            return result;
        }


        public void GetAllStocks()
        {
            var dt = GetRecords(@"Select Stok_kodu,ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD = 'G'  Group By STOK_KODU),0) -ISNULL((SELECT Sum(STHAR_GCMIK)
                                FROM  dbo.TBLSTHAR
                                WHERE[STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD = 'C' Group By STOK_KODU),0) as adet from dbo.TBLSTHAR as a
                                Group By STOK_KODU");

            if (dt != null)
                dgDb.ItemsSource = dt.DefaultView;
        }

        public DataTable GetRecords(string query)
        {
            try
            {
                using (var conn = new SqlConnection(GenerateConnStr(connectionString)))
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable("dbo.TBLSTHAR");
                        sda.Fill(dt);

                        return dt;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public bool IfExistStockCode(string stockCode)
        {
            try
            {
                using (var conn = new SqlConnection(GenerateConnStr(connectionString)))
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) from dbo.TBLSTHAR where STOK_KODU = @stockCode", conn))
                    {
                        conn.Open();
                        cmd.Parameters.AddWithValue("@stockCode", stockCode);
                        int recordCount = (int)cmd.ExecuteScalar();
                        if (recordCount > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public int InsertNewStockAmount(string stockCode, int amount)
        {
            string query =
                    @"--Buradan Yeni Miktar Eklemesi Yapılıyor  
                    -- (-1) stok yok , (0) kayıt eklenmedi, (1) kayıt eklendi
                    DECLARE @EskiMiktar int;
                    DECLARE @YeniMiktar int;
                    DECLARE @Result int;
                    SELECT @Result = -1;
                    IF EXISTS (SELECT 1 FROM dbo.TBLSTHAR WHERE [STOK_KODU] =  @stockCode)
                    BEGIN
	                    SELECT @YeniMiktar = @newAmount;
	                    SELECT @EskiMiktar = (Select ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='G'  Group By STOK_KODU),0) - ISNULL((SELECT Sum(STHAR_GCMIK)
			                    FROM  dbo.TBLSTHAR
			                    WHERE [STOK_KODU] = a.Stok_kodu AND STHAR_GCKOD ='C' Group By STOK_KODU),0) as Adet from  dbo.TBLSTHAR as a 
			                    Where STOK_KODU= @stockCode 
			                    Group By STOK_KODU);

	                    IF @EskiMiktar <> @YeniMiktar 
	                    BEGIN
		                    INSERT INTO TBLSTHAR (STOK_KODU, STHAR_GCMIK, STHAR_GCKOD, STHAR_TARIH, STHAR_HTUR, STHAR_DOVTIP, STHAR_DOVFIAT, SUBE_KODU)
		                    VALUES ( @stockCode, ABS(@YeniMiktar - @EskiMiktar),
		                    CASE 
			                    WHEN @EskiMiktar < @YeniMiktar THEN 'G'
			                    WHEN @EskiMiktar  > @YeniMiktar THEN 'C' 
		                    END
		                    ,@DateTime,'A' ,0 ,0.000000000000000,0);
	                    END
	                    SELECT @Result =  @@ROWCOUNT;
                    END

                    SELECT @Result";
            try
            {
                using (var conn = new SqlConnection(GenerateConnStr(connectionString)))
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        cmd.Parameters.AddWithValue("@newAmount", amount);
                        cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@stockCode", stockCode);
                        int recordCount = (int)cmd.ExecuteScalar();

                        return recordCount;

                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return -1;
        }


        #region Excel

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

                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        var tablenames = GetTablenames(result.Tables);

                        var columns = result.Tables[0].Columns;

                        dgExcel.ItemsSource = result.Tables[0].DefaultView;
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

        #endregion

        private void btnGetStHarTable_Click(object sender, RoutedEventArgs e)
        {
            GetAllStocks();
        }

        private void cbDbList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (connectionString != null) connectionString.DbName = cbDbList.SelectedItem.ToString();
        }

        private void btnProcessStart_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dgExcel.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)dgExcel.ItemContainerGenerator.ContainerFromIndex(i);
                if (row != null)
                {
                    row.Background = new SolidColorBrush(Colors.Azure);
                    TextBlock cellStockCode = dgExcel.Columns[0].GetCellContent(row) as TextBlock;
                    TextBlock cellAmount = dgExcel.Columns[1].GetCellContent(row) as TextBlock;

                    int x = 0;

                    Int32.TryParse(cellAmount.Text, out x);

                    lblProgresRecord.Content = cellStockCode.Text;
                    var result = InsertNewStockAmount(cellStockCode.Text, x);
                    if (result == 1)
                    {
                        row.Background = new SolidColorBrush(Colors.LightSeaGreen);
                    }
                    else if (result == 0)
                    {
                        row.Background = new SolidColorBrush(Colors.Lavender);
                    }
                    else
                    {
                        row.Background = new SolidColorBrush(Colors.DarkRed);
                        row.Foreground = new SolidColorBrush(Colors.White);
                    }
                }
            }
        }

        private void btnToCsv_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcelAndCsv();
        }

        private void ExportToExcelAndCsv()
        {
            DataTable dt = new DataTable();
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

        Microsoft.Office.Interop.Excel.Application excel;
        Microsoft.Office.Interop.Excel.Workbook workBook;
        Microsoft.Office.Interop.Excel.Worksheet workSheet;
        Microsoft.Office.Interop.Excel.Range cellRange;

        private void GenerateExcel(DataTable DtIN)
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
                dgExcel.ItemsSource = tempDt.DefaultView;
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

        //public static DataTable ToDataTable<T>(this IList<T> data)
        //{
        //    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
        //    DataTable dt = new DataTable();
        //    foreach (PropertyDescriptor prop in properties)
        //    {
        //        dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        //    }
        //    foreach (T item in data)
        //    {
        //        DataRow row = dt.NewRow();
        //        foreach (PropertyDescriptor pdt in properties)
        //        {
        //            row[pdt.Name] = pdt.GetValue(item) ?? DBNull.Value;
        //        }
        //        dt.Rows.Add(row);
        //    }
        //    return dt;
        //}
    }



    public class ModelConnStr
    {
        public string Server { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string DbName { get; set; }


    }

}
